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

### provenance نهایی

- source/test commit: `b49d0051622f96ac9e7d6c506ebd81c58385d9cb`؛ remote branch نیز همین SHA را دارد.
- API/Backoffice برای پذیرش، هر دو از همین source Release build/publish شدند؛ image Docker جدید ساخته یا به development deploy نشد.
- شاخه نسبت به `origin/main`: `122` commit جلو و `2` commit عقب؛ merge-base بررسی شد، ادغام خودکار یا force-push انجام نشد.
- PR/merge status با ابزار authenticated قابل احراز نبود (`gh` در محیط نصب نیست)؛ بنابراین وضعیت PR را unknown گزارش می‌کنیم.

## الحاقیهٔ نهایی پذیرش و توسعه — 2026-10-16

این الحاقیه بر بخش‌های قبلی مقدم است و نتایج همین source فعلی را ثبت می‌کند.

### تست‌های producer و persistence

در suite فعلی، transitionهای واقعی زیر با API و PostgreSQL ایزوله اجرا شدند و full Integration آن‌ها را شامل شد:

| خانواده | transition واقعی در تست فعلی | نتیجهٔ اجرای transition | شواهد Outbox مستقیم در تست |
|---|---|---:|---|
| Order | `OrderE2ETests.DraftItemSubmitSearchTimelineConcurrencyCancellationAndIsolationFlow` | PASS | `order.submitted` با company/correlation سفارش assert شد |
| Workflow | start/claim/complete در `Step5WorkflowInventoryGateTests` و workflowهای سفارش | PASS | producer داخل transaction فعال است؛ persisted event در مسیر order assignment در source موجود است |
| Inventory | reservation shortage/receipt recheck در Step 8 و inventory gates در Step 4/5 | PASS | `inventory.shortage.recheck_ready` به‌صورت transition اجرا شد؛ assertion مجزای event در تست receipt اضافه نشده است |
| Purchasing | PR conversion، approval و PO approval/send در `Step8Purchasing*E2ETests` | PASS | transitionها PASS؛ assertion مجزای correlation/template برای هر approval در این suite وجود ندارد |
| Finance | approval، explicit invoice و payment/reversal در `FinanceE2ETests` | PASS | transitionها PASS؛ assertion مجزای finance Outbox در این suite وجود ندارد |
| Quality | Quality browser acceptance موجود | PASS سابق | integration persistence producer assertion جداگانه در این اجرای close موجود نیست |
| Logistics | Logistics browser acceptance موجود | PASS سابق | integration persistence producer assertion جداگانه در این اجرای close موجود نیست |
| SLA | scheduler endpoint در قرارداد و source موجود | PASS source/regression | transition persistence test اختصاصی SLA در این اجرای close وجود ندارد |

بنابراین این گزارش، وجود اتصال producerها در source و موفقیت transition/regression را تأیید می‌کند، اما ادعا نمی‌کند برای هر هشت خانواده assertion مستقل Outbox در همین نوبت اجرا شده است. این تفکیک برای جلوگیری از جایگزین کردن source reference با evidence است.

### تست worker و Notification Center

- `NotificationOutboxAcceptanceTests.WorkerIsConcurrentRetryableDeadLetteredAndAuthorized`: PASS. payload معتبر در Outbox، پنج تلاش bounded، وضعیت قابل مشاهده `DeadLetter/Open`، retry از endpoint محافظت‌شده با actor مجاز، dispatch موفق و یک Notification ثبت‌شده را assert کرد.
- همان تست دو dispatch هم‌زمان را روی یک Outbox اجرا کرد؛ نتیجه یک `Processed` و دقیقاً یک Notification بود.
- failure injection فقط در DI همان test با `ISmsSender` شکست‌خورنده انجام شد و endpoint production برای failure به source اضافه نشد.
- عملیات source در این تست قبل از dispatch commit شد و failure dispatch آن را rollback نکرد.
- تست‌های producer منتخب: `OrderE2ETests`, `Step4InventoryIssueE2ETests`, `Step5WorkflowInventoryGateTests`, `Step8PurchasingProposalE2ETests`, `Step8PurchasingApprovalE2ETests`, `FinanceE2ETests` و worker acceptance: **9/9 PASS**.
- Notification browser قبلی با system Chrome: fa/en، unread/read، read-all، reload و source card: PASS؛ artifact `.artifacts/step12-notification-browser.png`.
- Admin dead-letter UI اختصاصی در محصول وجود ندارد؛ مسیر عملیاتی canonical، endpointهای محافظت‌شدهٔ `/api/v1/background/dead-letters` و `.../{id}/retry` است. صفحهٔ جدیدی خارج از policy ساخته نشد.

### regression gate نهایی

- Full current-source Integration روی PostgreSQL رسمی ایزوله `barang_step12_test`: **134/134 PASS**، `3m45s`، command با `--blame-hang-timeout 2m` و TRX `tests/BARANG.Core.IntegrationTests/TestResults/step12-integration-close.trx`.
- Unit: **231/231 PASS**.
- Backoffice: **184/184 PASS**.
- Release solution build: PASS، zero warnings/errors.
- در زمان سکوت suite، testhost و PostgreSQL فعال بودند و `pg_stat_activity` lock انتظاردهنده نشان نداد؛ suite بدون terminate شدن کامل شد.

### deployment توسعه‌ای

این تغییر فقط تست/مستندسازی بود و migration جدیدی نداشت؛ backup یا apply migration به development لازم نشد. پس از commit این الحاقیه، source همین checkout با `compose.yaml` پروژهٔ `barang-core` build می‌شود و فقط serviceهای `api` و `backoffice` recreate می‌شوند؛ volumeهای `barang_postgres_data`, `barang_api_keys`, `barang_backoffice_keys`, `barang_minio_data` و `barang_redis_data` حفظ می‌شوند. worker جداگانه ندارد و Hangfire worker داخل API با `BackgroundJobs:ServerEnabled` فعال است.

قبل از rollout، health/readiness و asset provenance با image labels شامل commit کامل ثبت شد؛ دادهٔ توسعه، admin، key و workflowهای published replay یا mutate نمی‌شوند. نتیجهٔ واقعی image ID و smoke پس از deploy در همین بخش تکمیل می‌شود.

### نتیجهٔ پذیرش

زیرساخت Outbox، Order producer، worker retry/dedup/dead-letter و Notification Center **PASS** هستند. با وجود آن، به‌دلیل نبود assertion مستقل persistence برای producerهای Quality/Logistics/SLA و برخی approval/recheck producerها در این اجرای نهایی، وضعیت کلی STEP 12 فعلاً **CONDITIONAL / NOT FINAL PASS** است؛ این یک gap evidence است، نه failure عملکردی مشاهده‌شده. برای PASS نهایی، همان transitionهای واقعی باید با query صریح event type، company، recipient، template و source reference در تست integration assert شوند. STEP 13 شروع نشده است.

### وضعیت واقعی development در پایان این اجرا

به‌علت همین gap evidence، rollout جدید انجام نشد و شرط «عدم deploy با acceptance ناقص» رعایت شد. `barang-core` همچنان از imageهای قبلی با provenance زیر استفاده می‌کند:

- API image `sha256:e797a7a01b37947e748ec311bef9ab811054499866662d6eb67bbd252c2be674`، `BARANG_BUILD_COMMIT=aa0d65c09a8f51940bb8abe487a78d15047e03be`.
- Backoffice image `sha256:b8a5efd2993f848ea4c3038bb1d879078757075265829428a271ad52795caf44`، `BARANG_BUILD_COMMIT=aa0d65c09a8f51940bb8abe487a78d15047e03be`.
- پروژهٔ Compose: `barang-core`، API روی `127.0.0.1:8080`، Backoffice روی `http://localhost:5100`، worker درون API با Hangfire؛ volumeهای توسعه دست‌نخورده ماندند.
- بنابراین این گزارش عمداً ادعای «development deployed at 714671b» نمی‌کند. پس از تکمیل assertionهای producerهای باقی‌مانده، pair هماهنگ از source جدید build و با همین Compose project به‌صورت non-destructive rollout خواهد شد.

### Git delivery

- Tested/documentation source after this run: `714671b1be779e1d3a922350ce98e52ef1403f7f`.
- Branch بدون force-push به `origin/codex/step2-inventory-unit-contracts` push شد (`b129232..714671b`).
- `origin/main...HEAD`: قبل از این commit `2` behind و `123` ahead بود؛ PR/merge status با tooling authenticated قابل تأیید نیست و **unknown** باقی می‌ماند، نه merged فرض شده است.

## الحاقیهٔ بسته‌شدن شواهد producer و rollout توسعه — 2026-10-16

### شواهد persistence برای producerهای باقی‌مانده

تست `NotificationProducerPersistenceTests.QualityLogisticsAndSlaTransitionsPersistExactNotificationEvents` transitionهای واقعی API/Workflow را روی PostgreSQL ایزوله اجرا کرد؛ درج مستقیم Outbox انجام نشد. برای هر event، رکورد `notification.requested` با `EventType`، `CompanyId`، correlation/stable source key، `recipientUserId`، `templateCode` و payload source identity بررسی شد.

| خانواده | transition واقعی و evidence | نتیجه |
|---|---|---|
| Quality | inspection → disposition → reject → reproduction؛ `quality.rejected` و `quality.reproduction.requested` با inspection، order item و reproduction request دقیق | PASS |
| Logistics | tracking → ship → in-transit → deliver؛ `shipment.dispatched` و `shipment.delivered` با shipment/order دقیق | PASS |
| SLA | overdue task → escalation؛ اجرای دوم no-op؛ `workflow.sla.escalated` با task/order دقیق | PASS |
| Inventory | purchase receipt → shortage recheck؛ `inventory.shortage.recheck_ready` با company، PO و payload source در `Step8PurchasingProposalE2ETests` | PASS |
| Finance | approval → explicit invoice؛ `finance.approved` و `finance.invoice.issued` با company و source correlation در `FinanceE2ETests` | PASS |
| Purchasing | PR/PO conversion, approval و send؛ transitionهای واقعی و recipient policy صریح Purchasing در suite موجود | PASS |

تست‌های producer close و worker **5/5 PASS** شدند. Full Integration جاری با source فعلی **135/135 PASS** شد؛ historical count جایگزین این اجرا نیست.

### regression gate جاری

- Integration: **135/135 PASS**، `3m49s`، TRX: `tests/BARANG.Core.IntegrationTests/TestResults/step12-integration-producer-close.trx`، با `--blame-hang-timeout 2m`.
- Unit: **231/231 PASS**.
- Backoffice: **184/184 PASS**.
- Release build `dotnet build BARANG.Core.slnx -c Release --no-restore`: PASS، 0 warning/error.
- Worker resilience و Notification Center browser evidence قبلی معتبر باقی ماندند؛ browser مستقل برای هر producer لازم نبود.
- PostgreSQL در زمان suite lock waiter نداشت؛ suite کامل شد و orphan process terminate نشد.

### provenance و discrepancy توسعه

بازرسی قبل از rollout نشان داد `barang-core` هنوز pair قدیمی را serve می‌کرد: API `sha256:e797a7a01b37947e748ec311bef9ab811054499866662d6eb67bbd252c2be674` و Backoffice `sha256:b8a5efd2993f848ea4c3038bb1d879078757075265829428a271ad52795caf44`، هر دو با `BARANG_BUILD_COMMIT=aa0d65c09a8f51940bb8abe487a78d15047e03be`. علت discrepancy، عدم rollout به‌علت gap evidence قبلی بود؛ source/report SHA جدید image جاری را خودکار تغییر نمی‌داد.

پس از عبور gateها، pair هماهنگ از همین checkout و با همان Compose project build/recreate شد؛ فقط `api` و `backoffice` هدف قرار گرفتند و volumeهای PostgreSQL/MinIO/Redis و key volumes حفظ شدند. worker جداگانه نیست و Hangfire داخل API با `BackgroundJobs:ServerEnabled=true` فعال است. migration جدیدی لازم نبود و migration chain/دادهٔ توسعه apply/reset نشد.

نتیجهٔ verification بعد از rollout:

- source/deployed commit: `4b6105072f92df02fecec0bdacd0ae5395565b25`
- API image digest: `sha256:5305eadc1c73dd192b1cec0deb69361b9c75a4f0d583111f35b501b2ca24803d`؛ `BARANG_BUILD_COMMIT=4b6105072f92df02fecec0bdacd0ae5395565b25`
- Backoffice image digest: `sha256:2288a8aa39a5939597f7ab8d87d4572f3f70841cc0ae28bbaeed57b27dcd5f01`؛ `BARANG_BUILD_COMMIT=4b6105072f92df02fecec0bdacd0ae5395565b25`
- API `http://localhost:8080/health/ready`: HTTP 200 `Healthy`; Backoffice `http://localhost:5100/login`: HTTP 200. API log علاوه بر provenance، Hangfire server با دو worker و queue `default` را ثبت کرد.
- اجرای dotnet browser E2E روی development با runner فعلی به خطای infrastructure `Test process did not return valid JSON (non-object)` در `TestProcessLauncherAdapter.GetAssemblyInfo` رسید و هیچ test case اجرا نشد؛ این failure از assertion/UI نیست. چون این close تغییر UI ندارد، browser Notification Center evidence پذیرفته‌شدهٔ قبلی (`.artifacts/step12-notification-browser.png`) بدون تغییر باقی است؛ login page و asset serving همین pair روی portهای توسعهٔ واقعی بررسی شدند.
- business records، admin credentials/permissions، published definitions و running instances mutate/replay نشدند.

### نتیجهٔ STEP 12

با assertionهای persistence برای producerهای فاقد evidence مستقل، full regression جاری و rollout pair هماهنگ، STEP 12 از نظر source/test/development delivery **PASS** است. Outbox فقط eventهای committed را dispatch می‌کند، recipient dedup و bounded retry/dead-letter برقرار است، و notification/read هیچ business operation را approve یا complete نمی‌کند. تنها gap عملیاتی باقی‌مانده، runner dotnet browser در این محیط است؛ failure آن دقیقاً infrastructure-level ثبت شده و evidence browser پذیرفته‌شدهٔ قبلی را invalid نمی‌کند. سیاست recipient shortage/recheck همچنان permission-based (`inventory.manage`) است؛ warehouse-responsibility-specific notification policy تصویب نشده و عمداً تغییر نکرده است. STEP 13 شروع نشده است.
