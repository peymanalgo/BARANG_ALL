# گزارش پذیرش STEP ۱۲ — اعلان‌های business با Transactional Outbox

## وضعیت فعلی

این گزارش بر مبنای source فعلی branch `codex/step2-inventory-unit-contracts` تهیه شده است. زیرساخت canonical قبلی شامل `outbox_messages`، dispatcher/worker Hangfire، retry با سقف ۵ تلاش، dead-letter و Notification Center بود؛ اما تا این مرحله producer business واقعی در مسیر سفارش وجود نداشت و صفحه‌ی Notification Center placeholder بود.

## تغییرات این مرحله

- producer `order.submitted` به همان transaction ثبت سفارش اضافه شد؛ اگر عملیات rollback شود، event قابل dispatch باقی نمی‌ماند.
- recipientها فقط از UserCompany فعال، حساب کاربری فعال و permission صریح `workflow.task.claim` در همان company resolved می‌شوند؛ Admin fallback یا broadcast بین شرکت‌ها وجود ندارد.
- stable correlation key بر پایه‌ی event type، aggregate identity و recipient است. قبل از ایجاد Outbox، رکورد همان company/event/key بررسی می‌شود؛ worker هم با `FOR UPDATE SKIP LOCKED` و وضعیت پردازش، retry/concurrency را کنترل می‌کند.
- Notification Center به `/api/v1/notifications/mine`، mark-read و read-all متصل شد و loading/error/empty و fa/en/ar captions دارد. خواندن اعلان هیچ business mutation انجام نمی‌دهد.
- کانال اجباری این مرحله InApp است؛ SMS/email/push فقط adapterهای local/provider-neutral قبلی هستند و provider خارجی اضافه نشده است.

## ماتریس پوشش فعلی

| Producer | Event/template | Recipient policy | وضعیت |
|---|---|---|---|
| Order submit | `order.submitted` | active same-company users with `workflow.task.claim` | Implemented |
| Task assignment/claim/reassignment | existing workflow transitions | pending explicit integration | Not yet connected |
| Inventory shortage/receipt recheck | shortage/readiness events | pending warehouse responsibility mapping | Not yet connected |
| PR/PO and Finance decisions | approval/issuance events | pending purchasing/finance assignment mapping | Not yet connected |
| Quality rejection/reproduction | quality events | pending quality actor mapping | Not yet connected |
| Shipment dispatch/delivery | logistics events | pending shipping/delivery assignment mapping | Not yet connected |
| SLA escalation | durable escalation exists | notification producer not yet connected | Not yet connected |

## تست و شواهد

- Release build فعلی: PASS، ۰ warning و ۰ error.
- Unit: `231/231` PASS.
- Backoffice: `184/184` PASS.
- Unit/Backoffice و Release build PASS شدند، اما producer اتصال‌داده‌شده‌ی اولیه‌ی `order.submitted` در اجرای isolated مسیر submit باعث HTTP 500 شد؛ این regression با قطع producer از همان مسیر ناپدید شد. بنابراین producer فعلاً به‌طور عمدی deploy نشده و پذیرش نهایی STEP ۱۲ **CONDITIONAL / NOT PASS** است.
- اجرای full Integration روی Compose قدیمی task-owned `barang-step9b-acceptance` نیز قابل قبول به‌عنوان clean baseline نبود: چند سناریوی Pricing/UserAccess/Artwork/Inventory/Purchasing با HTTP 404 در bootstrap/setup شکست خوردند، چون database/content-root آن environment مربوط به fixture قدیمی بود. این failures به‌عنوان PASS یا defect نهایی تفسیر نشدند؛ اجرای کامل باید با fresh PostgreSQL و migration/role initialization رسمی تکرار شود.
- authenticated Notification browser acceptance اجرا نشد؛ به همین دلیل deployment محلی انجام نشد.
- migration جدیدی لازم نشد؛ schema Outbox/Notification موجود استفاده شد و داده‌ی توسعه تغییر داده نشد.

## تصمیم‌ها و محدودیت‌های دقیق

- برای producerهای assignment، shortage، purchasing، finance، quality، logistics و SLA، policy دقیق recipient باید به مسئولیت/position همان دامنه متصل شود؛ تا تعیین این mapping، broadcast یا Admin fallback مجاز نیست.
- مسیر authorized source links باید هنگام اتصال هر producer به route واقعی همان domain اضافه شود؛ این مرحله از ایجاد لینک حدسی خودداری کرده است.
- external SMS/email/messaging خارج از scope باقی ماند.

## provenance و rollout

- Application baseline/deployment قبلی: `aa0d65c09a8f51940bb8abe487a78d15047e03be`.
- این تغییر application دارد و پس از عبور regression gate باید با compose pair هماهنگ deploy شود؛ در این نوبت تا قبل از Integration/browser acceptance، deployment توسعه‌ای انجام نشده است.
- backup و migration برای schema جدید لازم نبود؛ credentials، keys، volumes و business records توسعه حفظ شدند.

## blockerهای دقیق پیش از PASS

1. علت HTTP 500 در resolver recipientهای producer `order.submitted` باید با exception/SQL نخست روی fresh database تعیین و رفع شود؛ احتمال access/RLS یا contract resolution، نه با خاموش‌کردن isolation.
2. باید producerهای task assignment، shortage/recheck، purchasing، finance، quality، logistics و SLA با recipient responsibility واقعی هر دامنه متصل شوند.
3. پس از رفع موارد بالا، full Integration روی fresh isolated DB، worker retry/dedup/dead-letter و browser Notification Center در fa/en/ar باید اجرا شود؛ سپس فقط build تأییدشده deploy شود.

## الحاقیهٔ اجرای فعلی — عیب‌یابی، producerها و پذیرش ایزوله

### علت Submit 500 و اصلاح

خطای واقعی روی PostgreSQL ایزوله در همان transaction سفارش ثبت شد. resolver گیرندگان در `NotificationAdministration.PublishBusinessEventInsideTransactionAsync` عضو unmapped به نام `ApplicationUser.IsActive` را داخل LINQ query استفاده می‌کرد؛ EF Core نمی‌توانست آن را translate کند و قبل از commit، `InvalidOperationException` می‌داد. SQL متناظر join/exists روی `user_companies`، `application_users`، `user_permissions`، `roles` و `permissions` بود و company/RLS context همان company سفارش بود. اصلاح narrow به استفاده از `ApplicationUser.AccountState == Active`، بازهٔ اعتبار membership/role و permission مستقیم یا inherited همان شرکت انجام شد؛ RLS، bootstrap authorization و Admin fallback دست‌نخورده ماندند. producer به همان transaction submit برگشت.

عیب دوم در dispatch بود: payload Outbox با نام camelCase ذخیره می‌شود، اما `DispatchPayload` با deserializer پیش‌فرض حساس به حروف خوانده می‌شد و `Payload`/شناسه‌ها تهی می‌ماندند؛ `Notification.Create` در این مسیر `NullReferenceException` می‌داد. dispatcher اکنون از cached `JsonSerializerDefaults.Web` استفاده می‌کند. لاگ `OutboxDispatchFailed` نیز event type، company و message id را بدون payload حساس ثبت می‌کند.

### وضعیت producerها

| رویداد | transition canonical | گیرنده | وضعیت source |
|---|---|---|---|
| `order.submitted` | `OrderAdministration.SubmitAsync` | `workflow.task.claim` | متصل، atomic |
| `workflow.task.assigned` | start/complete و ساخت HumanTask بعدی | permission همان task | متصل، atomic |
| `workflow.task.claimed/forwarded` | workflow runtime/admin | `workflow.task.claim` | متصل، dedupe |
| `inventory.shortage.created` | reservation shortage | `inventory.manage` | متصل |
| `inventory.shortage.recheck_ready` | receipt و recheck | `inventory.manage` | متصل |
| `purchasing.*approval_requested/approved/rejected/sent` | PR/PO transitions | permission صریح Purchasing | متصل |
| `finance.*` و `finance.invoice.issued` | decision/explicit issuance | Finance/Invoice permission | متصل |
| `quality.rejected/reproduction.requested` | QC transitions | QC/Reproduction permission | متصل |
| `shipment.dispatched/delivered` | Logistics transitions | Delivery permission | متصل |
| `workflow.sla.escalated` | escalation creation | configured escalation permission یا workflow.manage | متصل |

recipient resolution broadcast نیست: membership فعال همان شرکت، حساب Active و permission مستقیم یا role-derived لازم است. برای shortage/recheck سیاست فعلی permission `inventory.manage` است و warehouse-responsibility-specific notification هنوز در قرارداد کسب‌وکار تثبیت نشده؛ این مورد تصمیم باز است، نه Admin fallback.

### تست‌های فعلی source و محیط

- Release solution build: PASS، ۰ error؛ در یک اجرای هم‌زمان build با process قدیمی فقط lock-retry موقت ثبت شد و build نهایی موفق بود.
- Unit: `231/231` PASS.
- Backoffice: `184/184` PASS.
- Full current Integration روی PostgreSQL رسمی ایزوله `barang_step12_test`، با migration/role setup رسمی، `133/133` PASS؛ runner با `--blame-hang-timeout 2m` و TRX `tests/BARANG.Core.IntegrationTests/TestResults/step12-integration-final.trx`، زمان `3m43s`.
- `OrderE2ETests` current source: `1/1` PASS؛ submit با producer فعال و assert وجود `notification.requested` برای همان order.
- Outbox dispatch isolated: event `order.submitted` به `Processed`، `attempts=1` و Notification به `Sent` رسید؛ retry dispatch دوباره رکورد جدید نساخت. خطای قبلی نیز با `OutboxDispatchFailed` ثبت و علت آن payload deserialization مشخص شد.
- Authenticated browser runner با system Chrome و API/Backoffice current source: PASS؛ URL `/workspace/notifications`، یک notification card، شمارنده unread، mark-all-read، reload و persisted `read_at_utc` تأیید شد. screenshot: `.artifacts/step12-notification-browser.png`. fixture پس از reset با `OrderE2ETests` دوباره ساخته شد؛ company id آزمون در زمان اجرا تولیدی است و در source hard-code نشده.
- محیط API ایزوله: `127.0.0.1:5872` با role `barang_test_app` و DB `barang_step12_test`؛ Backoffice publish با content-root `.artifacts/step12-backoffice-publish` روی `127.0.0.1:5812`. توسعه `barang-core` و volume آن untouched ماند.

### پذیرش و blockerهای باقی‌مانده

پذیرش order producer، transaction، authorization، notification list/read و dedup در محیط ایزوله **PASS** است. اما STEP 12 کلی هنوز **CONDITIONAL GO / NOT FINAL PASS** است، چون matrix مرورگر/پایداری برای همهٔ producerهای دامنه‌ای (shortage/recheck، PR/PO، Finance، Quality، Logistics و SLA) و سناریوهای worker restart، dead-letter exhaustion و authorized retry به‌صورت representative end-to-end اجرا نشده‌اند؛ کد اتصال آن‌ها اکنون در source است ولی نبود evidence را نمی‌توان با full Integration جایگزین کرد. این‌ها موارد باقی‌ماندهٔ پذیرش هستند.

هیچ migration جدیدی برای این اصلاحات لازم نشد؛ backup/migration chain موجود دست‌نخورده است. deployment توسعه‌ای انجام نشد، چون الزام کار «پیش از عبور gateها عدم deployment» بود. image Docker جدیدی ساخته نشده و provenance با source SHA و publish paths مشخص است.
