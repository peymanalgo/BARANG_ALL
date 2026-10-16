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
