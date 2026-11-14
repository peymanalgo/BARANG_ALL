# BARANG — گزارش ممیزی زیرساخت Docker

**تاریخ اجرای ممیزی:** 2026-10-24 (ثبت نهایی: 2026-10-25)
**ماشین:** Windows — مسیر محلی مخزن: `D:\00_BARANG`
**مخزن:** https://github.com/peymanalgo/BARANG_ALL
**ممیز:** Z Code — حالت فقط-خواندنی مطابق سند `BARANG_DOCKER_INFRASTRUCTURE_AUDIT.md`

> تنها استثنای غیرممیزی: یک کانتینر PostgreSQL یک‌بارمصرف ایزوله (`barang-audit-test-pg`، پورت 127.0.0.1:5599، بدون volume) برای اجرای تست‌های یکپارچه ساخته و بلافاصله پس از پایان تست حذف شد. هیچ کانتینر، volume یا داده موجود دستکاری نشد. هیچ secret یا مقدار حساسی در این گزارش ثبت نشده است.

---

## 1. زمینه Git (در تاریخ ممیزی)

| مورد | مقدار مشاهده‌شده |
|---|---|
| شاخه فعال worktree اصلی | `main` @ `09afbe3` — همگام با `origin/main` (بدون ahead/behind) |
| آخرین کامیت main | `09afbe3 fix(backoffice): accept account state wire contract` (2026-10-12 15:42 +0330) |
| وضعیت درخت کار main | ۱۷۱ فایل modified (unstage) + ۲۱۸ untracked؛ هیچ فایلی stage نشده |
| Worktreeهای جانبی | `step1-order-lifecycle-contract` @ `dfabad7` (برنچ local-only) و `step2-clean` @ `9bf1be9` (برنش `codex/step2-inventory-unit-contracts`، پوش‌شده) |
| رابطه شاخه‌ها | merge-base برنچ step2 با main برابر `e26bf87` است؛ step2 دارای **۱۵۶ کامیت** پس از آن، main فقط ۲ کامیت (`b9d1ce3`, `09afbe3`) |
| پوشه‌های محافظت‌شده | `04_Document` کاملاً untracked و دست‌نخورده؛ `02_WEB_APP` وجود ندارد؛ `03_MOB_APP` خالی و clean |

## 2. فایل‌های Compose و Dockerfile

- فایل Compose اصلی: `01_CORE/compose.yaml` (نام پروژه: `barang-core`) — نسخه working tree اصلی و نسخه `step2-clean` هر دو موجودند؛ **استک‌های در حال اجرا از نسخه `D:\00_BARANG\step2-clean\01_CORE\compose.yaml` ساخته شده‌اند** (مطابق label کانتینرهای running).
- Dockerfileها: `01_CORE/docker/Dockerfile` (API) و `01_CORE/src/BARANG.Backoffice/Dockerfile` (Backoffice).
- اسکریپت init دیتابیس: `docker/postgres/init-app-role.sh` (bind mount فقط-خواندنی، اجرا در اولین init volume).
- فایل `.env` وجود ندارد (فقط `.env.example`)؛ یعنی مقادیر پیش‌فرض dev داخل compose فعال‌اند.
- پروفایل، reverse proxy یا TLS تعریف نشده (محیط توسعه).

## 3. فهرست خدمات اصلی (project: `barang-core`)

| سرویس | Image | پورت (host) | Volume | Health check | وضعیت مشاهده‌شده |
|---|---|---|---|---|---|
| api | بیلد محلی `barang-core-api` | `127.0.0.1:8080→8080` | `barang_api_keys` | `curl http://localhost:8080/health/ready` | Up 2 days (healthy) |
| backoffice | بیلد محلی `barang-core-backoffice` | `0.0.0.0:5100→5100` (عمداً LAN-reachable طبق کامنت compose) | `barang_backoffice_keys` | `curl http://localhost:5100/login` | Up 2 days (healthy) |
| postgres | `postgres:17.6-bookworm` | `127.0.0.1:5432→5432` | `barang_postgres_data` + init script | `pg_isready` | Up 2 days (healthy) |
| redis | `redis:8.2.1-alpine` | `127.0.0.1:6379→6379` | `barang_redis_data` | `redis-cli ping` | Up 2 days (healthy) |
| minio | `minio/minio:RELEASE.2025-07-23T15-54-02Z` | `127.0.0.1:9000, 9001` | `barang_minio_data` | `mc ready local` | Up 2 days (healthy) |

وابستگی‌ها: `api` منتظر healthy شدن postgres/minio/redis؛ `backoffice` منتظر healthy شدن api. همه با `restart: unless-stopped`.

## 4. کانتینرهای مشاهده‌شده (2026-10-24)

**استک اصلی `barang-core` (زنده و سالم):** پنج کانتینر بالا — همه `Up 2 days (healthy)`.

**استک‌های acceptance (محیط‌های شواهد مرحله‌ای، از کارهای step2):**
- `dashboard-accept-*` — همه healthy (api ‏5818، backoffice ‏5822، pg ‏5579، redis ‏5689، minio ‏5910/5911)
- `barang-stock-adjust-acceptance-*` — همه healthy (api ‏5808، backoffice ‏5510، pg ‏5547، redis ‏6387، minio ‏9050/9051)
- `barang-step9b-acceptance-*` — api/pg/redis/minio سالم؛ **کانتینر backoffice در وضعیت `Created` و متوقف**
- `barang-step45-postgres` — `Exited (255)`؛ volume مربوط به `barang-step6` همچنان موجود است (داده حفظ شده)

**غیرمرتبط با BARANG:** سه کانتینر Oracle (`oracle26ai`, `ords26ai`, `oracle26ai_backup`) متعلق به پروژه دیگر — exited و دست‌نخورده.

**منشأ بیلد (provenance):**
- `barang-core-api-1`: image ساخته 2026-10-17T08:04Z از `step2-clean/01_CORE`؛ label محیطی `BARANG_BUILD_COMMIT=a7202d6` (= `feat: deliver operational management dashboard` — آخرین کامیت عملکردی step2؛ سه کامیت بعد از آن فقط docs/test هستند).
- `dashboard-accept-api-1`: image ساخته 2026-10-17T08:01Z؛ commit ثبت‌شده `unknown`.

**نتیجه:** سیستم زنده‌ی در حال اجرا = خط کد `codex/step2-inventory-unit-contracts`، نه محتوای working tree اصلی `D:\00_BARANG\01_CORE`.

## 5. پایگاه داده و ماندگاری داده

- DB اصلی: `barang_core` روی `barang-core-postgres-1`؛ اسکیماها: `public`, `barang`, `hangfire`.
- جدول تاریخچه migration با نام سفارشی: `barang.__ef_migrations_history` — **۹۶ migration اعمال‌شده**؛ آخرین: `20261016140407_AlignShipmentReturnIdempotency` (همگام با مراحل پایانی step2).
- داده واقعی موجود: ۶۱ سفارش، ۲ مشتری، ۷۸ تأمین‌کننده (نمونه‌گیری فقط-خواندنی).
- Volumeهای نام‌دار برای همه سرویس‌های stateful موجود و متصل‌اند؛ داده‌ها در برابر بازسازی کانتینر ماندگارند.
- پشتیبان‌ها: ۹ فایل `*.backup`/`*.dump` در ریشه مخزن (اکتبر 2026، مربوط به recovery/پیش‌از-deploy) وجود دارد — یعنی بکاپ‌گیری دستی انجام شده، اما **روال خودکار/مستند بکاپ و بازیابی آزمایش‌نشده ثبت نشده است**.

## 6. ماتریس متغیرهای پیکربندی (فقط نام‌ها؛ مقادیر redact)

| متغیر | سرویس | در compose تعریف | پیش‌فرض dev | توضیح |
|---|---|---|---|---|
| `POSTGRES_DB/USER/PASSWORD` | postgres | بله | دارد | اعتبارنامه dev |
| `POSTGRES_APP_USER/APP_PASSWORD` | postgres/api | بله | دارد | کاربر اپلیکیشن (RLS) |
| `ConnectionStrings__CoreDatabase` | api | بله | دارد (از متغیرهای بالا) | اتصال به `postgres` داخل شبکه compose |
| `ConnectionStrings__Redis` | api | بله | ثابت | `redis:6379` |
| `FileStorage__*` (Endpoint/AccessKey/SecretKey/Bucket/Secure/Enabled/MaximumUploadBytes) | api | بله | دارد | MinIO داخل شبکه؛ کلیدها از متغیرهای MinIO |
| `SecurityBootstrap__Secret` | api | بله | **بدون پیش‌فرض (خالی)** | در صورت خالی ماندن باید در لاگ هشدار داده شود — بررسی نشده |
| `DataProtection__KeysPath` | api/backoffice | بله | `/app/.keys` | volume ماندگار کلیدها |
| `CompanyResolution__AllowDevelopment*` | api | بله | `true` | فقط برای Development |
| `REDIS_PORT/MINIO_*/POSTGRES_PORT/API_PORT/BACKOFFICE_PORT/MINIO_BUCKET/MAXIMUM_UPLOAD_BYTES` | همه | بله | دارد | پورت‌ها و تنظیمات عمومی |
| `BARANG_TEST_DB_CONNECTION` | تست یکپارچه | — | — | فقط برای اجرای تست؛ باید به DB ایزوله اشاره کند |

فایل `.env` وجود ندارد؛ مقادیر پیش‌فرض dev کامیت‌شده فعال‌اند (برای محیط توسعه قابل قبول؛ برای production باید بازنگری شود). هیچ secret جدیدی در این گزارش بازتولید نشده است.

## 7. تست‌های اتصال و سلامت (اجرای تازه 2026-10-24)

| بررسی | نتیجه |
|---|---|
| `GET http://127.0.0.1:8080/health/ready` (API اصلی) | **200** |
| `GET http://127.0.0.1:5100/login` (Backoffice اصلی) | **200** |
| `GET http://127.0.0.1:5818/health/ready` (dashboard-accept) | **200** |
| PostgreSQL `pg_isready` | accepting connections |
| connectivity تست یکپارچه به DB ایزوله (۱۳۶ تست) | موفق (بخش ۸) |

## 8. نتایج build و تست (اجرای تازه — نه اعداد تاریخی)

**خط مرجع `step2-clean` (کد همسان با سیستم زنده):**

| Suite | نتیجه تازه | عدد تاریخی (گزارش 2026-10-17) |
|---|---|---|
| build `BARANG.Core.slnx` (Debug) | موفق — ۰ خطا | — |
| `BARANG.Core.UnitTests` | **۲۳۳/۲۳۳ پاس** | 262 |
| `BARANG.Core.ArchitectureTests` | **۴/۴ پاس** | 4 |
| `BARANG.Backoffice.Tests` | **۱۸۴/۱۸۴ پاس** | 154 |
| `BARANG.Core.IntegrationTests` (PostgreSQL ایزوله) | **۱۳۶/۱۳۶ پاس** (۴ دقیقه) | 160 |

اختلاف با اعداد تاریخی به‌احتمال زیاد ناشی از بازسازماندهی تست‌ها در مراحل پایانی step2 است؛ همه suiteها سبزند. تطبیق دقیق شمارش‌ها یک قلم P3 برای traceability است.

**Working tree اصلی `D:\00_BARANG\01_CORE` (تغییرات uncommitted):**
- build **fail** با خطای `CS0234` در `OrderContracts.cs(28)`: نوع `DecisionAttachmentView` در namespace `BARANG.Core.Application.Workflow` تعریف نشده — این نوع در step2 در `Workflow/WorkflowDesignerContracts.cs` تعریف شده ولی در working tree اصلاً وجود ندارد.
- تحلیل هم‌پوشانی: از ۱۷۱ فایل modified فقط ۲۸ فایل با نسخه step2 یکسان‌اند؛ ۱۴۲ فایل متفاوت‌اند؛ ۱۴۰ فایل untracked با نام مشابه در step2 ولی با محتوای متفاوت وجود دارند؛ ۶۰ فایل untracked فقط در working tree موجودند؛ ۲۷۲ فایل (شامل ۴۰ فایل migration و مستندات STEPها) فقط در step2 موجودند. working tree دارای ۱۳۹ فایل migration در برابر ۱۷۹ فایل در step2 است (از جمله یک migration با پسوند `Step2InventoryUnitContractsFinal` مورخ 2026-10-12).

**تفسیر:** محتوای uncommitted در worktree اصلی یک snapshot قدیمی‌تر/ناقص از همان خط کار step2 است (حدود 2026-10-12) که خط step2 بعداً در worktree جداگانه تا 2026-10-17 تکمیل، تست، build و deploy شده است. snapshot ناسازگار است و build نمی‌شود.

## 9. مسائل طبقه‌بندی‌شده

| کد | طبقه | شرح |
|---|---|---|
| 1 | **P0 — خطر از دست رفتن کار/حقیقت** | خط معتبر و deploy شده (۱۵۶ کامیت step2 با ۹۶ migration اعمال‌شده روی DB زنده) در `main` ادغام نشده؛ `main` نمایانگر وضعیت اعتبارسنجی‌شده نیست. هم‌زمان ۳۸۹ تغییر uncommitted در worktree اصلی بدون هیچ محافظتی (نه commit، نه stash، نه branch) رها شده‌اند. |
| 2 | **P1 — مانع گردش کار قابل اتکا** | build در worktree اصلی خراب است (`DecisionAttachmentView` گمشده)؛ کار مستقیم از `D:\00_BARANG\01_CORE` ممکن نیست. |
| 3 | **P1 — بهداشت مخزن** | ۹ فایل بکاپ DB در ریشه مخزن + پوشه‌های `recovery-snapshots/`, `modeldiff/`, `.artifacts/`, `TestResults/`, `tools/`, worktreeهای جانبی و فایل‌های زائد (`route_grid_hits.txt`, `.tmp-textcols.txt`) همه untracked و بدون پوشش `.gitignore`اند — خطر stage/commit تصادفی و حجم بالای مخزن. |
| 4 | **P2 — عملیات** | کانتینر `barang-step9b-acceptance-backoffice-1` در وضعیت Created متوقف است و `barang-step45-postgres` خروج شده؛ استک‌های acceptance سه‌گانه منابع را اشغال کرده‌اند (طبق قواعد پروژه بدون اجازه حذف/خاموش نمی‌شوند). |
| 5 | **P2 — traceability** | اختلاف شمارش تست تازه با گزارش‌های تاریخی (بخش ۸) مستند نشده است. |
| 6 | **P3** | `SecurityBootstrap__Secret` بدون پیش‌فرض است (وضعیت مقدار فعلی: redact/نامشخص)؛ اعتبارنامه‌های dev کامیت‌شده در compose (قابل قبول برای dev، نامناسب برای production)؛ روال خودکار بکاپ/بازیابی مستند نیست؛ `01_CORE/.gitattributes` فقط در step2 وجود دارد (پس از merge حل می‌شود). |

## 10. شواهد کلیدی

- `git status --porcelain=v1 -b` → `## main...origin/main` بدون ahead/behind؛ ۳۸۹ ردیف.
- `git rev-list --count main..codex/step2-inventory-unit-contracts` → `156`؛ عکس آن → `2`.
- `docker inspect barang-core-api-1` → label `com.docker.compose.project.config_files = D:\00_BARANG\step2-clean\01_CORE\compose.yaml` و env name `BARANG_BUILD_COMMIT` با مقدار `a7202d6`.
- `docker ps -a` → وضعیت همه کانتینرها (بخش ۴).
- `docker exec barang-core-postgres-1 psql ... "select count(*) from barang.__ef_migrations_history"` → `96`.
- `curl /health/ready` و `/login` → HTTP 200.
- خروجی `dotnet build`/`dotnet test` در دو worktree (بخش ۸).

## 11. مجهول‌های باقی‌مانده

1. پذیرش مرورگری (Chrome) اجرا نشده — خارج از scope این ممیزی (تعلق به تسک‌های بعدی گیت ۱/۲).
2. دو رکورد Draft مشکوک سفارش (`78f10752...`, `42375c38...`) بازبینی نشده‌اند (تسک 1.5).
3. اینکه آیا در میان ۶۰ فایل untracked منحصربه‌working-tree چیزی ارزشمند و قابل نگهداری وجود دارد یا خیر — نیازمند diff دقیق در مرحله تعیین تکلیف (نسخه‌بندی روی branch آرشیوی این بررسی را بدون فشار زمانی ممکن می‌کند).
4. علت تاریخی قرارگیری snapshot در worktree اصلی از خود Git قابل استخراج نیست.
5. مقدار `SecurityBootstrap__Secret` در محیط واقعی (به‌عمد redact شده) — هشدار راه‌اندازی در لاگ باید جداگانه بررسی شود.

## 12. گام‌های پیشنهادی بعدی (بدون اجرا — نیازمند تأیید کاربر)

1. **حفظ بدون از دست دادن:** تمام تغییرات uncommitted عملکردی working tree اصلی روی یک برنچ آرشیو (مثلاً `archive/main-worktree-snapshot-20261024`) commit شوند — بدون merge به main. فایل‌های زائد و بکاپ‌ها به `.gitignore` اضافه شوند و commit نشوند.
2. **ادغام خط معتبر:** `codex/step2-inventory-unit-contracts` در `main` ادغام شود (تنها ۲ کامیت اختلافی main دارد که محتوای `09afbe3` به‌صورت `39b1568` در step2 موجود است).
3. **بازنشانی worktree اصلی به main جدید؛** build و تست مجدد از `D:\00_BARANG\01_CORE`؛ تعیین تکلیف برنچ step1 (یک کامیت docs قابل cherry-pick).
4. پس از تثبیت: بازبینی استک‌های acceptance (نگهداری/حذف با اجازه صریح) و مستندسازی روال بکاپ/بازیابی.
