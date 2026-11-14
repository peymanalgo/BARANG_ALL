# BARANG — Z Code / Z AI Project Handoff
## مرجع اجرایی برای ادامه Core و سپس ساخت B2C Web و Mobile

**نسخه سند:** 2.0 — به‌روزرسانی بر اساس زمینه‌های بازیابی‌شده تا 2026-10-22  
**تاریخ تهیه:** 2026-10-22  
**زبان سند:** فارسی با نام‌های فنی انگلیسی  
**مخزن:** `https://github.com/peymanalgo/BARANG_ALL`  
**مسیر محلی اصلی:** `D:\00_BARANG`  
**هدف:** به Z Code / Z AI داده شود تا ابتدا وضعیت واقعی پروژه را بررسی کند، Core/Backoffice را تا پذیرش نهایی کامل کند، سپس B2C Web و Mobile را بر اساس همان Core و اسناد مصوب بسازد.

> **محدودیت مهم درباره دقت وضعیت:** این سند بر اساس اطلاعات قابل بازیابی از گفتگوها و یادداشت‌های پروژه تهیه شده است؛ به معنی بررسی زنده مخزن محلی یا GitHub در تاریخ تهیه نیست. هیچ وضعیت تاریخی نباید بدون بررسی فایل‌ها، Git، تست‌ها و سرویس‌های فعلی «وضعیت قطعی امروز» فرض شود. هر جا مدرک پیاده‌سازی کامل در دسترس نبوده، وضعیت «نیازمند ممیزی» درج شده است. Z Code باید قبل از هر تغییری یک ممیزی واقعی انجام دهد و این سند را با وضعیت کد تطبیق دهد.

---

# 1. دستورالعمل شروع برای Z Code / Z AI

## مأموریت کلی

BARANG یک سامانه مدیریت کسب‌وکار سفارش‌های چاپ پوشاک است که باید با معماری زیر تکمیل شود:

1. **Core + Backoffice** برای کارکنان و عملیات داخلی؛ اولین اولویت و باید مستقل، قابل استفاده و اعتبارسنجی‌شده باشد.
2. **B2C Web App** برای مشتریان.
3. **Customer Mobile App** برای مشتریان.

هر سه برنامه از یک Core و یک پایگاه داده مشترک استفاده می‌کنند؛ قواعد کسب‌وکار نباید جداگانه و ناسازگار در وب و موبایل دوباره پیاده‌سازی شوند.

### ترتیب اجباری کار

1. ممیزی repository و اسناد.
2. مشخص‌کردن دقیق قابلیت‌های واقعاً پیاده‌شده در Core و Backoffice.
3. رفع blockerهای موجود و تکمیل Core/Backoffice.
4. تکمیل آزمون‌های خودکار و پذیرش واقعی در مرورگر.
5. ثبت نتیجه، commit و push طبق سیاست Git و دستور کاربر.
6. ساختار repository در گزارش کاربر در 2026-10-07 به‌عنوان منتقل‌شده/اصلاح‌شده گزارش شده است؛ **این refactor را از نو انجام نده** مگر ممیزی فعلی نشان دهد ناقص یا برگشت‌خورده است.
7. پس از پذیرش نهایی Core/Backoffice، طراحی و پیاده‌سازی B2C Web بر پایه APIهای Core.
8. سپس طراحی و پیاده‌سازی Mobile بر پایه همان APIها و قراردادها.
9. اجرای آزمون‌های یکپارچه سه برنامه و تحویل.

**ممنوع:** شروع زودهنگام B2C یا Mobile برای دور زدن نقص‌های Core؛ بازنویسی کل پروژه؛ حذف قابلیت‌های موجود بدون تحلیل وابستگی؛ اعلام تکمیل صرفاً بر اساس build سبز یا ادعای عامل قبلی.

## مرحله صفر — فقط ممیزی، بدون تغییر کد

قبل از کدنویسی:

- `git status --short --branch`
- `git log --oneline --decorate -30`
- بررسی branch، HEAD، remote و تفاوت local با `origin/main`
- بررسی تمام پروژه‌ها، solutionها، migrations، API endpoints، tests، Docker Compose و مستندات
- بررسی `04_Document` با دقت؛ فایل‌های کاربر، Excel lock files و اسناد untracked را بدون اجازه تغییر نده
- بررسی سرویس‌های فعال و health endpointها؛ از روی گزارش‌های قدیمی نتیجه‌گیری نکن
- اجرای build/testهای موجود در محیط فعلی و ثبت دقیق نتیجه
- تهیه فهرست feature-by-feature با وضعیت `Implemented and verified`, `Implemented but not verified`, `Partial`, `Missing`, `Blocked`
- شناسایی منبع حقیقت نیازمندی‌ها در Business Analysis و Software Architecture
- **در این مرحله هیچ فایل کدی را تغییر نده، migration نساز، commit/push نکن و سرویس‌ها را خاموش نکن.**

خروجی ممیزی باید در گزارش مشخص کند: چه چیزی وجود دارد، مسیر کد آن کجاست، تست اثبات‌کننده چیست، چه چیزی ناقص است و گام بعدی دقیقاً چیست.

---

# 2. اطلاعات پایه و ساختار repository

## مسیرها و مخزن

- Root: `D:\00_BARANG`
- GitHub: `https://github.com/peymanalgo/BARANG_ALL`
- محل اولیه توسعه Core: `D:\00_BARANG\01_CORE`
- مستندات پروژه: `D:\00_BARANG\04_Document`
- تصویر صفحه ورود Backoffice: `D:\00_BARANG\04_Document\1.png`
- استاندارد UI/UX که در Library موجود است: `BARANG_BACKOFFICE_UI_UX_STANDARD_FINAL.md` و نسخه‌های هم‌نام/بازبینی‌شده؛ Z Code باید نسخه موجود در repository را شناسایی کند و یک نسخه مرجع را انتخاب کند، نه اینکه چند استاندارد متعارض بسازد.

## ساختار نهایی مورد انتظار

| پوشه | نقش نهایی |
|---|---|
| `01_CORE` | ASP.NET Core API، Domain، Application، Infrastructure، Blazor Backoffice، تست‌ها و مستندات فنی Core |
| `02_WEB_APP` | فقط وب‌اپلیکیشن B2C مشتریان |
| `03_MOB_APP` | اپلیکیشن موبایل مشتریان |
| `04_Document` | اسناد تحلیل کسب‌وکار، معماری، UI/UX و اسناد کاربر |

در ساختار تاریخی، Backoffice در `02_WEB_APP` قرار داشت. در 2026-10-07 کاربر گزارش کرد انتقال source، Docker و tests به `01_CORE` کامل شده، `02_WEB_APP` برای B2C خالی/رزرو شده، وابستگی به مسیر قدیمی باقی نمانده و build/tests/health موفق بوده‌اند؛ Chrome هنوز در دسترس نبود. بنابراین انتقال **گزارش‌شده به‌عنوان انجام‌شده است** و نباید دوباره انجام شود مگر ممیزی زنده ثابت کند ساختار فعلی با این وضعیت فرق دارد. مسیر واقعی را از repository بررسی کن.

---

# 3. پشته فناوری و تصمیم‌های معماری

## فناوری مصوب

- .NET 10
- ASP.NET Core
- PostgreSQL
- Modular Monolith
- REST API / OpenAPI
- Docker / Docker Compose
- Blazor برای Backoffice
- Next.js برای B2C Web
- Elsa Workflows برای workflowهای موردنیاز
- Hangfire برای background jobs/scheduled jobs
- Redis برای نیازهای مناسب زیرساختی
- MinIO با سازگاری S3 برای فایل‌ها

### موبایل: تصمیمی که باید قبل از شروع نهایی شود

Flutter در گفتگوهای قبلی به‌عنوان گزینه پیشنهادی مطرح شده، اما در اطلاعات قابل بازیابی، تصمیم قطعی و تأیید نهایی کاربر برای Flutter ثبت نشده است. بنابراین:

- Z Code نباید Flutter را تصمیم قطعی فرض کند.
- ابتدا اسناد موجود و پروژه‌های موبایل را بررسی کند.
- اگر هیچ انتخاب مصوبی وجود ندارد، یک مقایسه کوتاه و عملی بین Flutter و گزینه‌های معقول ارائه کند و برای انتخاب نهایی از کاربر سؤال کند؛ تا آن زمان API/Core را مستقل از فناوری موبایل طراحی کند.
- در هر صورت Mobile باید از APIهای Core استفاده کند و منطق کسب‌وکار را در کلاینت کپی نکند.

## قواعد معماری

- یک پایگاه داده مشترک؛ یک Core واحد به‌عنوان منبع قواعد کسب‌وکار.
- Modular Monolith؛ بدون microservices زودهنگام.
- REST/OpenAPI به‌عنوان قرارداد کلاینت‌ها.
- business rules در Domain/Application/Use Cases، نه در UI.
- workflow engine باید قواعد کسب‌وکار Core را مصرف کند، نه اینکه خودش منبع مستقل قواعد باشد.
- APIهای مناسب B2C و Mobile باید versioning، validation، authorization، pagination/filtering و error contracts سازگار داشته باشند.
- همه قابلیت‌های مهم باید با تست و مستندات API پوشش داده شوند.
- وابستگی‌ها تا حد ممکن محلی، قابل اجرا با Docker و بدون سرویس‌های پولی/دسترسی دشوار باشند.

---

# 4. وضعیت Core: چه چیزهایی تا الان گزارش شده است؟

این بخش یک **نقشه شواهد تاریخی** است، نه تأیید زنده کد. Z Code باید هر ردیف را در repository بررسی و وضعیت نهایی را ثبت کند.

## 4.1 زیرساخت پایه — گزارش پیاده‌سازی قبلی

پروژه‌ها/فایل‌های گزارش‌شده:

- `BARANG.Core.slnx`
- `BARANG.Core.Api`
- `BARANG.Core.Application`
- `BARANG.Core.Domain`
- `BARANG.Core.Infrastructure`
- `BARANG.Core.UnitTests`
- `BARANG.Core.IntegrationTests`
- `compose.yaml`
- `docker/Dockerfile`

Commitهای تاریخی گزارش‌شده:

- `48f5460 docs(core): define git workflow`
- `56d0606 docs(core): define naming conventions`
- `3f3d6c1 chore(core): bootstrap .NET 10 Docker foundation`

این hashها باید با `git log` تأیید شوند؛ وجود یا push شدن فعلی آن‌ها فرض نشود.

## 4.2 Company Context / Identity / Security — گزارش تاریخی

در یکی از مراحل، این قابلیت‌ها به‌عنوان پیاده‌سازی‌شده گزارش شده‌اند:

- endpoint مربوط به `auth/me`
- مدیریت شرکت‌ها و انتخاب شرکت
- قراردادهای identity/company/role
- دریافت `X-Company-ID` همراه با اعتبارسنجی
- company-scoped persistence
- forced Row-Level Security در موارد تعریف‌شده
- audit hooks و migrationهای مرتبط

با این حال، در گزارش آن مرحله، اعتبارسنجی نهایی به علت مشکل دسترسی named pipe مسدود شده و commit نهایی انجام نشده بود. در گزارش دیگری Phase 09 نیز ناقص بوده است: 39 تست از 133 تست fail شده، Docker/PostgreSQL/E2E در دسترس نبوده، اختلاف طول فیلدهای API/DB و موارد Lookup/RLS/auth/concurrency باقی مانده بودند؛ commit `73be122` گزارش شده اما push نشده بود. این گزارش‌ها مربوط به مقاطع مختلف‌اند و نباید بدون بررسی Git به‌عنوان وضعیت جاری تلقی شوند.

**کار Z Code:** وضعیت واقعی CompanyContext، authentication، authorization، tenant/company isolation، RLS، Lookup، concurrency و audit را در کد و تست‌ها بررسی کند. اگر تضاد بین گزارش‌هاست، وضعیت repository و تست‌های جاری ملاک باشد.

## 4.3 Backoffice / Workbench / Cartable V3 — گزارش اکتبر 2026

در گفتگوهای اکتبر، کار روی Workbench/Cartable V3 گزارش شده است:

### مراحل 1–3
گزارش‌شده به‌عنوان تکمیل‌شده، با این وضعیت‌های اعتبارسنجی در یک مقطع:
- Core: `262/262`
- Architecture: `4/4`
- Backoffice: `139/139`
- Release build موفق
- API/services سالم
- مراحل 2 و 3 build/test موفق؛ در یک گزارش Chrome browser acceptance هنوز pending بود.

### Step 4
موارد تأییدشده:
- Forward
- substitution backend/UI
- on-behalf-of audit
- security/RLS
- migration
- Core: `262/262`
- Backoffice: `139/139`
- Architecture: `4/4`
- مشکل Orders 500 که با تنظیم MinIO test-host برطرف شده بود
- PostgreSQL integration: `148/151`، با 3 خطای 422 که در آن گزارش unrelated توصیف شده بودند؛ باید دوباره بررسی شوند.

### Step 5
گزارش وضعیت پذیرش نهایی در یک مقطع:
- dedicated PostgreSQL tests: `7/7`
- full integration: `157/158`
- خطای باقی‌مانده: `BusinessProfileApiTests` bootstrap 409؛ rerun جداگانه در یک گزارش موفق شده بود.
- Substitute/on-behalf-of، Auditor و race tests پیاده‌سازی شده بودند.
- بقیه build/test/healthها در گزارش موفق بودند.
- در گزارش بعدی، PostgreSQL integration `158/158` و Step 5 `7/7` موفق ثبت شد.

### Step 6
گزارش شده بود:
- پیاده‌سازی انجام شده؛ Chrome visual acceptance هنوز pending بود.
- در RTL، Detail سمت چپ و Grid سمت راست؛ در LTR معکوس.
- نسبت تقریبی layout حدود 30/70.
- Grid، Detail، Stepper و actions با طراحی فشرده و حرفه‌ای.
- Quick Preview به‌عنوان summary مجزا حفظ شده بود.
- پیشنهاد شده بود Step 7 شروع شود، اما Chrome و final regression همچنان hard gate بودند.

### Workbench scope گزارش‌شده در مراحل 1–6
- typed backend/read models
- Master–Detail
- server-side advanced filters و Saved Views
- تب‌های جزئیات/فایل‌ها/سوابق/پیام‌ها
- runtime Stepper
- Claim / Release / Forward / Follow-up / Substitute / Decision Note / Attachments / actions
- reference-driven UI
- Core-first در `D:\00_BARANG\01_CORE`
- عدم دست‌کاری `02_WEB_APP` و `03_MOB_APP` در این batchها

در یک گزارش نهایی در اکتبر:
- PostgreSQL integration: `158/158`
- Step 5: `7/7`
- Core: `262/262`
- Architecture: `4/4`
- Backoffice tests در گزارش‌های نزدیک: `139/139`

**وضعیت دقیق نهایی Step 6 و اینکه Step 7 چه بوده یا اجرا شده، از اطلاعات موجود قطعی نیست.** اولین کار Z Code باید بازبینی نتیجه واقعی Step 6، تست‌های Chrome و regression و history گفت‌وگو/کد باشد؛ اگر هنوز acceptance مرورگر اجرا نشده، پیش از توسعه قابلیت جدید آن را انجام دهد.

## 4.4 Supplier — وضعیت گزارش‌شده

### Supplier Prompt 1 — Supplier Basic Data
هدف: ممیزی selectorهای Supplier و انتقال مقادیر قابل پیکربندی به داده‌های پایه تأمین‌کننده، از جمله:
- دسته‌بندی تأمین‌کننده
- درجه/رتبه
- نوع شخصیت حقوقی
- تماس و آدرس
- پرداخت، حساب
- قرارداد و اسناد
- خدمات
- ارسال/حمل‌ونقل
- QC، ارزیابی
- hold/block/reject
- واحد خرید و بسته‌بندی

الزامات: CRUD کامل، ترجمه fa/en/ar، اتصال selectorها به داده‌های مرجع، حذف hard-code و داده‌های تکراری.

### Supplier Prompt 3 — Product/SKU و قیمت
گزارش قبلی شامل:
- ارتباط Supplier با Product/SKU
- تأمین و قیمت‌گذاری
- price history و price lists
- preferred supplier rules
- purchasing-readiness API
- localization و company scope
- migration و Playwright E2E

پیاده‌سازی باید از روی کد تأیید شود؛ گزارش قدیمی به‌تنهایی کافی نیست.

### Supplier Prompt 4 — lifecycle
گزارش‌شده به‌عنوان پیاده‌سازی‌شده:
- lifecycle domain/API برای evaluation، quality، approval، hold، contracts و documents
- company-scoped persistence
- audit hooks
- expiry calculation
- MinIO entity-link validation
- migration
- lifecycle tabs/workspace در Backoffice
- resourceهای fa/ar/en و RTL/LTR

تست‌های تاریخی:
- Core `238/238`
- Backoffice components `110/110`
- PostgreSQL integration `31/31`
- browser regression `36/36`

این اعداد مربوط به نسخه‌ای قدیمی‌تر از وضعیت اکتبر هستند و ممکن است با اعداد جدیدتر Core/Backoffice اشتباه شوند.

**موانع باز ثبت‌شده برای Supplier Prompt 4:**
1. معیارهای evaluation قابل ویرایش نبودند و submissionها معیارهای خالی داشتند.
2. جریان کامل upload فایل و اتصال آن به سند/قرارداد و دانلود/بازکردن در مرورگر کامل نبود.
3. پذیرش کامل مرورگری CRUD برای evaluations، incidents، contracts، documents و MinIO files انجام نشده بود.
4. در گزارش آن مقطع commit/push انجام نشده بود.
5. در بازیابی بعدی، مورد امنیتی اثبات دسترسی غیرمجاز cross-company به فایل‌ها و اعتبارسنجی migration/model snapshot نیز به‌عنوان معیارهای لازم ذکر شده بود.

Supplier نباید کامل اعلام شود مگر تمام این موارد با تست واقعی رفع و اثبات شوند.

### Supplier Prompt 5 — منوی Basic Data و داده‌های مشترک
تعریف مصوب:
- `Basic Data` باید منوی اصلی/سطح اول شود، نه زیرمجموعه System Management.
- تمام زیرمنوهای Basic Data در همه ماژول‌ها audit شوند.
- داده‌های مشترک تکراری مانند Banks و Address Types یکپارچه شوند.
- یک بخش `Basic Data → Shared Basic Data` ایجاد شود.
- selectorهای تمام ماژول‌ها به منبع مشترک متصل شوند.
- backend، DB، migrations، UI، permissions، localization و E2E بررسی شوند.
- حذف entity/table یا داده فقط پس از تحلیل وابستگی و migration امن.
- Prompt 5 در یک گفتگوی قدیمی تا زمانی که نتیجه Prompt 4 دریافت و بررسی نشده بود، نباید شروع می‌شد. اکنون Z Code باید ابتدا وضعیت واقعی Prompt 4 را audit کند؛ اگر حل نشده، ابتدا آن را تکمیل کند و سپس Prompt 5 را اجرا کند.

## 4.5 فهرست کلی ماژول‌های Core که در roadmap مطرح شده‌اند

در roadmap قبلی، توالی کلی Core شامل این حوزه‌ها بوده است:
- Foundation / solution standards
- Company context, security, audit
- Customer / master data
- Orders
- Pricing
- Files
- Inventory
- Production
- Quality Control
- Logistics / shipping
- Workflow adapter
- Notifications / background jobs
- External API

در استاندارد UI، صفحات/حوزه‌های اولویت‌دار Backoffice شامل:
1. Organizational Chart
2. Persons
3. User & Access Management
4. Customers
5. Products / SKU
6. Suppliers
7. Inventory/Purchasing
8. Orders
9. Production
10. Finance

این‌ها **roadmap و فهرست حوزه‌ها** هستند، نه اثبات اینکه هر ماژول کامل شده است. Z Code باید برای هر ماژول، موجودیت‌ها، endpointها، CRUD، business rules، migration، permission و تست واقعی را ثبت کند.

---

# 4.6 آخرین وضعیت زمانیِ بازیابی‌شده — اکتبر 2026

این قسمت از خلاصه‌های دقیق‌تر گفتگوهای نزدیک به تاریخ تهیه سند آمده و باید نسبت به گزارش‌های قدیمی‌تر اولویت داشته باشد، اما همچنان وضعیت runtime را باید دوباره بررسی کرد.

## آخرین گزارش تست Core/Backoffice — 2026-10-17

کاربر گزارش کرد که اصلاحات تاریخ با `LocaleDateService` متمرکز انجام شده‌اند و تست‌های زیر سبز بوده‌اند:

- Core: `262/262`
- Architecture: `4/4`
- Backoffice: `154/154`
- PostgreSQL Integration: `160/160`
- **Chrome visual/browser acceptance هنوز pending بوده است.**

این اعداد از آخرین گزارش مشخص‌شده در یادداشت‌های قابل بازیابی هستند، نه نتيجة اجرای جدید توسط این سند. Z Code باید دستورات تست، تعداد فعلی تست‌ها و نتیجه واقعی را از repository/runner استخراج کند.

## وضعیت تاریخ‌ها و سفارش‌ها — 2026-10-17

- پنج سفارش Draft خراب از طریق **Order API رسمی** اصلاح شده‌اند.
- دو رکورد مشکوک عمداً دست‌نخورده مانده‌اند و نباید خودسرانه اصلاح یا حذف شوند؛ برای هر دو ابتدا شواهد و تصمیم مستند لازم است:
  - `78f10752-6bda-4362-8ba0-5ee65490575c` — تاریخ ثبت‌شده `0784-04-27`
  - `42375c38-9802-4a3b-a605-355d2909bc54` — تاریخ ثبت‌شده `1405-07-01`
- قیمت گزارش‌شده برای `SKU-DEV-002`: `750,000 IRR`، با `EffectiveFrom` برابر `2026-09-26` (معادل تاریخ شمسی گزارش‌شده `1405/07/04`). پیش از هر تغییر، مقدار فعلی DB/API را بررسی کن؛ این اطلاعات را به‌عنوان وضعیت فعلی قطعی تلقی نکن.
- در ارزیابی قبلی، سیستم در وضعیت **Partial Pass** بود. blockerهای پذیرش عبارت بودند از: تکمیل inventory همه سطوح تاریخ در کل سیستم، تعیین تکلیف مستند دو Draft مشکوک، و پذیرش rendered-component/browser برای مسیرهای معیوب New Order/Pricing.
- هدف بعدی: تکمیل بررسی تاریخ‌ها و UI واقعی؛ صرف موفقیت unit/integration tests برای عبور از gate کافی نیست.

## Order 360 و Cartable/Workbench

در گزارش کاربر بتاريخ 2026-10-07، یک مشخصه مشترک Order 360 ثبت شده بود: افزودن گره `Order Items` و نمایش `Full Order Specification` به‌صورت read-only در Orders، Cartable Received و My Referrals. پیاده‌سازی به‌عنوان انجام‌شده گزارش شده بود، اما Chrome acceptance هنوز pending بود. بررسی کن که این قابلیت در نسخه فعلی وجود دارد، داده‌هایش از API واقعی می‌آیند، و در RTL/LTR و سه مسیر یادشده درست کار می‌کند.

## Phase 21 — وضعیت تاریخی قدیمی‌تر

در 2026-09-01 گزارش شده بود Phase 21 ناقص و uncommitted است: Core `265` تست، Backoffice `20` تست، سرویس‌ها سالم، `origin/main` برابر `82b7226`؛ جریان‌های mutation و E2E پوشش‌داده‌شده کمبود داشتند. این گزارش قدیمی‌تر از تست‌های اکتبر است و **نباید به‌عنوان وضعیت فعلی استفاده شود**؛ آن را صرفاً برای تاریخچه نگه دار و با Git history بررسی کن.

## انتقال Backoffice به Core — 2026-10-07

کاربر گزارش کرد که source، Docker و tests مربوط به Backoffice به `01_CORE` منتقل شده‌اند؛ `02_WEB_APP` فقط برای B2C رزرو شده و وابستگی‌ای به مسیر قدیمی نمانده است. build، tests و health در آن گزارش موفق بودند؛ Chrome در آن زمان unavailable/pending بود. بنابراین اولین ممیزی Z Code باید انتقال انجام‌شده را تأیید کند، نه اینکه دوباره آن را اجرا کند.

# 5. مهم‌ترین کارهای Core که باید تعیین تکلیف شوند

Z Code باید پس از ممیزی، فهرست دقیق زیر را با مسیر فایل‌ها و شواهد پر کند:

## A. Build / runtime / repository
- [ ] تمام solution/projectها و references سالم هستند.
- [ ] buildهای Debug و Release موفق‌اند.
- [ ] تست‌ها در محیط واقعی اجرا می‌شوند.
- [ ] Docker Compose services و health checks سالم‌اند.
- [ ] migrationها با PostgreSQL واقعی اجرا و model snapshot تطبیق داده شده است.
- [ ] هیچ فایل یا تغییر نامرتبط در Git stage نشده است.
- [ ] API OpenAPI/Swagger در محیط توسعه قابل مشاهده است.
- [ ] browser regression روی Backoffice انجام شده است.

## B. Identity / company / security
- [ ] login/logout و current-user
- [ ] role/permission enforcement در backend
- [ ] انتخاب شرکت و CompanyContext
- [ ] `X-Company-ID` فقط در صورت اعتبارسنجی صحیح پذیرفته می‌شود
- [ ] tenant/company isolation در queryها و mutationها
- [ ] RLS و آزمون دسترسی بین شرکت‌ها
- [ ] audit trail
- [ ] race/concurrency tests
- [ ] negative authorization tests

## C. Master Data و داده‌های مشترک
- [ ] Customers / Persons / Organizational Chart
- [ ] Products / SKU
- [ ] Supplier master data
- [ ] Shared Basic Data و حذف تکرارها
- [ ] سرچ و pagination سمت سرور
- [ ] CRUD واقعی و persistence پس از reload
- [ ] selectorهای searchable و localized
- [ ] import/export فقط اگر در اسناد مصوب تعریف شده است

## D. Orders / Cartable / Workbench
- [ ] Order CRUD و قواعد وضعیت
- [ ] pricing، discount، tax، shipping طبق اسناد
- [ ] Cartable/Workbench master-detail
- [ ] claim/release/forward/follow-up/substitute/decision note/attachments
- [ ] Stepper با وضعیت واقعی workflow
- [ ] Saved Views و advanced server-side filters
- [ ] history/messages/files
- [ ] on-behalf-of audit و authorization
- [ ] Chrome acceptance برای RTL/LTR و layout
- [ ] regression کامل پس از Step 6/7

## E. Supplier lifecycle/files
- [ ] Evaluation criteria قابل مدیریت
- [ ] CRUD واقعی evaluations/incidents/contracts/documents
- [ ] upload/association/download/open-browser
- [ ] MinIO metadata و entity-link integrity
- [ ] cross-company access denied
- [ ] expiry behavior
- [ ] migration/model snapshot valid
- [ ] browser E2E تمام مسیرها
- [ ] Prompt 5: Basic Data و Shared Basic Data

## F. Localization / UI / dates
- [ ] فارسی، انگلیسی و عربی در همه UIها
- [ ] fa/ar RTL و en LTR
- [ ] تغییر زبان در زمان اجرا
- [ ] تاریخ شمسی در fa و میلادی در en/ar
- [ ] ذخیره‌سازی تاریخ مستقل از locale
- [ ] صفحه ورود و مسیرهای خطا/validation localized
- [ ] UI استاندارد و دسترس‌پذیر
- [ ] selectorها، loading/empty/error/success states
- [ ] browser acceptance و persistence

## G. مستندسازی و API
- [ ] OpenAPI کامل و سازگار
- [ ] مستند endpointها و error contracts
- [ ] API contract برای B2C/Mobile
- [ ] migration/runbook محلی
- [ ] README راه‌اندازی و health checks
- [ ] traceability نیازمندی → پیاده‌سازی → تست → پذیرش

**هر checkbox فقط با مدرک واقعی تیک بخورد.** اگر کد وجود دارد اما تست/مرورگر آن را تأیید نکرده، وضعیت را «پیاده‌سازی شده ولی تأییدنشده» ثبت کن.

---

# 6. تعریف پذیرش نهایی Core/Backoffice

Core/Backoffice زمانی آماده مرحله بعد است که:

1. نیازمندی‌های Business Analysis و Software Architecture به traceability table تبدیل شده باشند.
2. هر نیازمندی به کد، endpoint/use case و تست مشخص متصل باشد.
3. هیچ blocker شناخته‌شده‌ای باقی نمانده باشد یا کاربر صریحاً آن را به تعویق انداخته باشد.
4. تمام unit tests و integration tests مربوط موفق باشند؛ failure با «غیرمرتبط» برچسب نخورد مگر علت آن اثبات و ثبت شده باشد.
5. PostgreSQL واقعی، migrationها و model snapshot تأیید شده باشند.
6. Backoffice در browser واقعی کار کند؛ CRUD، actions و persistence پس از refresh بررسی شوند.
7. Workbench/Cartable V3 و Supplier file lifecycle پذیرش مرورگری داشته باشند.
8. زبان‌های fa/en/ar، RTL/LTR و تاریخ‌ها تست شوند.
9. security/RLS/company isolation و negative tests پاس شوند.
10. Docker services لازم سالم و در دسترس باشند؛ volumeها حفظ شوند.
11. diff بازبینی شده و تغییر خارج از scope وجود نداشته باشد.
12. گزارش نهایی، commit و push مطابق سیاست جاری انجام شده باشد.

انتقال Blazor Backoffice به `01_CORE` در 2026-10-07 به‌عنوان انجام‌شده گزارش شده است. پس از پذیرش Core، فقط ساختار را تأیید کن؛ انتقال مجدد نکن مگر ممیزی زنده نقصی را اثبات کند. سپس build/test/runtime دوباره بررسی شوند.

---

# 7. قوانین دائمی توسعه و Git

## Codex / Z Code
- ابتدا audit؛ سپس تغییر.
- فقط scope مشخص‌شده.
- از rewrite یا refactor گسترده بدون نیاز خودداری شود.
- قابلیت ناقص را کامل اعلام نکن.
- تست را برای سبز شدن ضعیف یا حذف نکن.
- خطاها را swallow نکن و موفقیت جعلی نشان نده.
- قبل از تغییر UI، استاندارد Backoffice UI/UX را بخوان.
- تغییرات database باید migration و تست داشته باشند.
- فایل‌های کاربر و اسناد untracked را بدون اجازه تغییر نده.

## Git
- `main` باید نمایانگر وضعیت اعتبارسنجی‌شده باشد.
- commitهای اتمیک با Conventional Commits و scope مناسب.
- پیش از commit، status و diff را بازبینی کن.
- `force-push`، reset مخرب، حذف داده یا cleanup مخرب ممنوع است مگر اجازه صریح.
- `docker compose down` اجرا نکن، مگر کاربر صریحاً درخواست دهد یا cleanup ضروری و تأیید شده باشد.
- volumeها حفظ شوند.
- پس از اعتبارسنجی موفق، کاربر می‌خواهد نسخه معتبر به GitHub push شود؛ بااین‌حال، قبل از push، دستور جاری گفتگو و وضعیت branch/remote را بررسی کن.
- در پایان هر مرحله، commit hash، وضعیت push، تست‌ها و URLهای سرویس‌ها گزارش شوند.

---

# 8. الزامات مشترک B2C Web و Mobile

این دو برنامه نباید backend موازی یا قواعد کسب‌وکار مستقل بسازند. هر دو باید از Core API استفاده کنند.

## تجربه مشتری

حداقل حوزه‌هایی که باید از اسناد Business Analysis استخراج و تأیید شوند:
- ثبت‌نام/ورود و مدیریت نشست
- پروفایل مشتری
- مرور کاتالوگ محصولات و SKU
- گزینه‌های قابل سفارش/چاپ و جزئیات محصول
- آپلود یا انتخاب طرح چاپ، فقط مطابق نیازمندی مصوب
- محاسبه قیمت شفاف بر اساس قواعد Core
- ایجاد سفارش و مشاهده وضعیت
- آدرس‌ها و اطلاعات تحویل
- پرداخت، تخفیف و مالیات فقط بر اساس نیازمندی مستند
- تاریخچه سفارش‌ها و جزئیات هر سفارش
- اعلان‌ها و پیگیری سفارش
- پشتیبانی/پیام‌ها در صورت تعریف‌شدن در تحلیل کسب‌وکار
- مدیریت فایل‌های مشتری و سفارش با مجوزهای درست

این فهرست **فهرست کشف نیازمندی** است، نه تأیید اینکه تمام قابلیت‌ها در اسناد فعلی الزام شده‌اند. Z Code باید هر مورد را با سند کسب‌وکار تطبیق دهد و موارد نامشخص را به‌عنوان تصمیم باز علامت بزند؛ چیزی را خودسرانه به دامنه اضافه نکند.

## API contract
برای هر قابلیت کلاینت:
- endpoint و HTTP method
- request/response DTO
- validation rules
- authentication/authorization
- pagination/filtering/sorting
- error response استاندارد
- idempotency برای عملیات حساس، اگر نیازمندی/معماری اقتضا کند
- file upload/download strategy
- API versioning
- OpenAPI examples
- integration tests

## امنیت
- هیچ secret یا credential در کلاینت قرار نگیرد.
- قیمت و وضعیت سفارش در سمت سرور محاسبه و اعتبارسنجی شوند.
- client-supplied price یا role قابل اعتماد نیست.
- دسترسی به سفارش‌ها، فایل‌ها و پروفایل باید به مالک/مجوز صحیح محدود شود.
- فایل‌های خصوصی از طریق لینک‌های عمومی دائمی عرضه نشوند.
- اعتبارسنجی سمت کلاینت جایگزین validation سمت سرور نیست.
- rate limiting و abuse prevention در صورت نیاز معماری/تهدید بررسی شود.

## چندزبانه و تاریخ
- UI هر دو کلاینت: fa/en/ar.
- fa و ar راست‌به‌چپ؛ en چپ‌به‌راست.
- تغییر زبان در زمان اجرا.
- fa: تقویم شمسی؛ en/ar: میلادی.
- تاریخ‌های ذخیره‌شده در API مستقل از locale باشند؛ از قرارداد روشن ISO 8601/UTC برای timestampها و تاریخ canonical استفاده شود.
- قیمت و اعداد با locale مناسب نمایش داده شوند؛ مقدارهای پولی در سرور به‌صورت دقیق/decimal مدیریت شوند.
- تمام خطاها و validationها localized باشند.

---

# 9. B2C Web App — اطلاعات شروع

## فناوری
- Next.js
- TypeScript و UI stack باید با استانداردهای موجود repository هم‌راستا باشد؛ پیش از افزودن کتابخانه جدید، packageهای فعلی را بررسی کن.
- مصرف‌کننده Core REST API
- responsive، mobile-first برای تجربه مشتری، قابل دسترس و قابل تست
- پشتیبانی fa/en/ar، RTL/LTR و تقویم مطابق زبان

## مراحل پیشنهادی پس از پذیرش Core
1. **Audit/spec:** اسناد B2C و APIهای Core را استخراج و شکاف‌ها را مشخص کن.
2. **Foundation:** app shell، routing، localization، RTL/LTR، design tokens، error/loading states، API client و auth/session design.
3. **Catalog:** محصولات/SKU و داده‌های لازم از APIهای موجود.
4. **Customer profile/address:** فقط مطابق business analysis.
5. **Order configuration:** انتخاب محصول/گزینه‌های چاپ و ارسال داده به Core برای محاسبه معتبر قیمت.
6. **Cart/checkout/order creation:** با قواعد و وضعیت‌های سرور.
7. **Order tracking/history:** خواندن وضعیت‌های واقعی Core.
8. **Files/notifications:** بر اساس API و مجوزهای Core.
9. **E2E/accessibility/security:** مسیرهای کلیدی و سه زبان.
10. **Release/deployment:** Docker/build/runbook طبق معماری پروژه.

## معیارهای پذیرش B2C
- هیچ داده ساختگی یا mock در مسیر release وجود نداشته باشد.
- UI به API واقعی Core متصل باشد.
- خطاهای API، loading و empty states روشن باشند.
- قیمت و وضعیت سفارش از سرور دریافت شوند.
- ثبت سفارش و refresh/بازگشت صفحه داده را از بین نبرد.
- زبان، جهت، تاریخ و اعداد درست باشند.
- تست E2E برای مسیرهای مصوب خرید/سفارش موفق باشد.
- authorization و دسترسی به سفارش‌های دیگران آزمون شود.

---

# 10. Mobile App — اطلاعات شروع

## فناوری
- انتخاب نهایی فناوری موبایل در اطلاعات قابل بازیابی قطعی نیست.
- Flutter گزینه‌ای است که قبلاً پیشنهاد شده، نه انتخاب قطعی تأییدشده.
- تا زمان تصمیم نهایی، Core API و قراردادهای مستقل از پلتفرم طراحی شوند.
- موبایل نباید به PostgreSQL مستقیم متصل شود؛ فقط از APIهای مجاز Core استفاده کند.

## مراحل پیشنهادی
1. استخراج requirements موبایل از Business Analysis.
2. تأیید فناوری، نسخه‌ها، ساختار پروژه و راهبرد build.
3. طراحی navigation، session/auth، API client و localization.
4. پیاده‌سازی پروفایل و کاتالوگ.
5. پیاده‌سازی پیکربندی سفارش و upload امن فایل در صورت نیاز.
6. cart/checkout/order placement بر اساس APIهای Core.
7. order history/tracking.
8. push notifications فقط پس از تعیین provider/architecture و ثبت تصمیم.
9. مدیریت offline/retry فقط در حد نیازمندی مشخص؛ از ساخت پیچیدگی بی‌دلیل پرهیز شود.
10. تست دستگاه/شبیه‌ساز، RTL/LTR، سه زبان، accessibility و API security.
11. build و انتشار آزمایشی مطابق پلتفرم و تصمیم کاربر.

## معیارهای پذیرش Mobile
- اتصال واقعی به Core API.
- مدیریت امن token/session و secrets.
- پشتیبانی fa/en/ar و RTL/LTR.
- تاریخ شمسی/میلادی مطابق زبان.
- مسیرهای کلیدی سفارش و پیگیری با API واقعی کار کنند.
- خطای شبکه، retry و وضعیت‌های loading/error به‌درستی مدیریت شوند.
- تست‌ها روی دستگاه/شبیه‌ساز هدف اجرا شوند.
- هیچ business rule یا قیمت‌گذاری حساس فقط در موبایل پیاده‌سازی نشده باشد.

---

# 11. مدل کسب‌وکار سفارش و قیمت

در اسناد اولیه، فرمول قیمت سفارش چاپ تی‌شرت چنین گزارش شده است:

**قیمت نهایی = قیمت خام تی‌شرت + چاپ جلو + چاپ پشت + چاپ اضافه + بسته‌بندی + ارسال − تخفیف + مالیات**

Z Code باید این فرمول را با اسناد فعلی Business Analysis تطبیق دهد. آن را فقط یک سرنخ کسب‌وکار در نظر بگیر تا زمانی که سند مصوب فعلی تأییدش کند. قواعد قیمت باید در Core محاسبه شوند و B2C/Mobile فقط نتیجه معتبر سرور را نمایش دهند.

---

# 12. استاندارد UI/UX Backoffice

استاندارد UI/UX موجود در Library می‌گوید:
- سبک سازمانی فشرده و حرفه‌ای
- sidebar تیره navy/charcoal
- accent زرد/amber
- محتوای خنثی و گرم
- grid و فرم فشرده، toolbar روشن، modal CRUD استاندارد
- selectorهای قابل جست‌وجو
- reuse shared components
- fa/en/ar، RTL/LTR، تاریخ مرکزی
- loading/empty/error/success states
- keyboard/focus/accessibility
- browser E2E و persistence پس از refresh
- تست را برای عبور صوری ضعیف نکن
- اولویت قواعد: Business Analysis، سپس Security/Authorization/Tenant isolation، سپس UI/UX standard، سپس shared components، سپس ترجیح صفحه‌ای.

Z Code باید نسخه نهایی موجود در repository را شناسایی کند. در سوابق، مسیرهای زیر مطرح شده‌اند:
- `D:\00_BARANG\04_Document\01_UI_UX\BARANG_BACKOFFICE_UI_UX_STANDARD.md`
- `D:\00_BARANG\01_CORE\docs\BARANG_BACKOFFICE_UI_UX_STANDARD.md`

این دو مسیر جایگزین احتمالی‌اند؛ بدون بررسی فایل واقعی، یکی را فرض نکن و دو نسخه متعارض نساز.

---

# 13. ترتیب اجرایی و gateها

## Gate 0 — Repository Audit
خروجی: inventory پروژه‌ها، وضعیت Git، تست‌های فعلی، سرویس‌ها، نیازمندی‌ها، feature matrix و blockers. بدون تغییر کد.

## Gate 1 — Core Blockers
رفع نقص‌های اثبات‌شده در Core/Backoffice؛ اولویت با امنیت، داده، migration، API correctness و قابلیت‌های ناقص موجود.

## Gate 2 — Workbench/Cartable V3
تکمیل/تأیید Step 6 browser acceptance و final regression؛ سپس بررسی اینکه Step 7 واقعاً چیست و آیا اجرا شده است. هیچ مرحله‌ای صرفاً بر اساس شماره پیش نرود.

## Gate 3 — Supplier Prompt 4
معیارهای evaluation، CRUD lifecycle، upload/association/download/open-browser، cross-company denial، migration/model snapshot و browser E2E.

## Gate 4 — Supplier Prompt 5
Basic Data منوی سطح اول؛ Shared Basic Data؛ deduplication ایمن؛ تمام selectorها و تست‌ها.

## Gate 5 — Core/Backoffice Acceptance
Traceability به اسناد، تمام تست‌ها، مرورگر، امنیت، localization، تاریخ، Docker و مستندات.

## Gate 6 — Repository Structure Verification
انتقال Backoffice در 2026-10-07 به‌عنوان انجام‌شده گزارش شده است. ساختار فعلی، references، Docker و tests را بررسی کن؛ اگر درست است، refactor را تکرار نکن. اگر انتقال ناقص یا regression اثبات شد، کمترین اصلاح لازم را با شواهد انجام بده. `02_WEB_APP` باید فقط B2C باشد.

## Gate 7 — B2C Web
نیازمندی‌ها → API contracts → app foundation → customer flows → E2E → release.

## Gate 8 — Mobile
تأیید فناوری → API/client foundation → customer flows → device tests → release.

## Gate 9 — End-to-end System Acceptance
سه برنامه، یک Core، یک DB؛ بررسی end-to-end سفارش، قیمت، فایل، وضعیت، مجوزها، زبان و تاریخ.

---

# 14. فرمت گزارش هر مرحله برای Z Code

در پایان هر task این گزارش را تولید کن:

1. **Scope:** هدف مرحله.
2. **Files changed:** فهرست مسیرها.
3. **Implemented:** قابلیت‌هایی که واقعاً اضافه/اصلاح شدند.
4. **Tests:** نام تست، تعداد pass/fail/skip و دستور دقیق اجرا.
5. **Browser/device acceptance:** سناریوهای واقعی اجراشده.
6. **Database:** migration و نتیجه PostgreSQL.
7. **Security:** authorization, company isolation, RLS, file access.
8. **Localization:** fa/en/ar، RTL/LTR، dates.
9. **Runtime:** URLها، health status، Docker containers.
10. **Git:** branch، commit hash، push status، فایل‌های untracked.
11. **Blockers:** نقص‌های باقی‌مانده، با اولویت و مدرک.
12. **Next step:** فقط گام بعدیِ ضروری.

هیچ تستی را اجراشده اعلام نکن اگر واقعاً اجرا نشده است. تعداد تست‌های historical را به‌عنوان نتیجه جدید گزارش نکن.

---

# 15. Prompt آماده برای اولین اجرای Z Code

این Prompt را به‌عنوان شروع بده:

> You are continuing the existing BARANG project. Read `BARANG_ZCODE_PROJECT_HANDOFF.md` first. Work strictly in the existing repository; do not rebuild the product from scratch.
>
> **Task 0 — Audit only; do not modify code yet.**
> 1. Inspect repository root, Git branch/status/log/remotes, all solutions/projects, migrations, APIs, tests, Docker Compose, and project documentation.
> 2. Read the approved Business Analysis, Software Architecture, and the repository's final Backoffice UI/UX standard. Treat business/security requirements as authoritative.
> 3. Produce a feature-by-feature Core/Backoffice implementation matrix with evidence: code paths, API endpoints, migrations, tests, browser acceptance, and status (`verified`, `implemented-unverified`, `partial`, `missing`, `blocked`).
> 4. Specifically audit CompanyContext/auth/RLS, Master Data and shared lookups, Orders, Workbench/Cartable V3 Steps 1–6 and the actual status of Step 7, Order 360, Supplier Prompt 4 blockers, and whether Supplier Prompt 5 has already been implemented. Verify the reported Backoffice relocation into `01_CORE`; do not repeat it if already complete.
> 5. Check the latest reported gates: Core `262/262`, Architecture `4/4`, Backoffice `154/154`, PostgreSQL Integration `160/160` (reported 2026-10-17) and pending Chrome visual acceptance. Re-run the actual current suites; do not reuse historical counts as fresh results.
> 6. Review centralized `LocaleDateService`, inventory every date input/display/storage surface, and inspect New Order/Pricing browser acceptance. Preserve the two suspicious Draft records pending documented evidence and user-approved disposition: `78f10752-6bda-4362-8ba0-5ee65490575c` (`0784-04-27`) and `42375c38-9802-4a3b-a605-355d2909bc54` (`1405-07-01`).
> 7. Record current build/test results and Docker/API/PostgreSQL/Redis/MinIO health. Distinguish freshly executed checks from historical reports.
> 8. Identify the single highest-priority next Core blocker and provide a traceable remediation plan with acceptance criteria.
>
> **Constraints:** No code changes, migrations, commits, pushes, tags, destructive Git commands, database resets, or `docker compose down` in this audit task. Preserve all data and user files, especially `04_Document`. Do not touch B2C or Mobile yet. Do not assume Flutter is approved. Preserve fa/en/ar, RTL/LTR, centralized Jalali/Gregorian date policy, company isolation, authorization, audit, and existing business behavior. Do not claim a feature is complete without evidence.
>
> Return the audit report and wait for approval before implementing the next task.

---

# 16. مواردی که هنوز باید از repository/اسناد استخراج شوند

این اطلاعات در یادداشت‌های قابل بازیابی کامل و قطعی نبودند؛ Z Code باید آن‌ها را از فایل‌های واقعی استخراج کند:

- فهرست کامل entityها و ماژول‌های موجود Core
- وضعیت جاری هر endpoint و API contract
- نسخه دقیق schema و migrationهای PostgreSQL
- فهرست دقیق Business Analysis و Software Architecture files و traceability هر requirement
- وضعیت قطعی Step 7 Workbench و Chrome acceptance
- وضعیت جاری Supplier Prompt 4 و 5
- وضعیت فعلی commit/push و remote
- فهرست واقعی سرویس‌ها، ports، credentials handling و health endpoints (بدون چاپ secretها)
- وضعیت فعلی Next.js یا هر کد موجود در `02_WEB_APP`
- وجود یا عدم وجود کد موبایل در `03_MOB_APP`
- فناوری نهایی موبایل
- نیازمندی‌های قطعی مشتری، checkout، payment، shipping، notifications و upload در B2C/Mobile

---

# 17. اصل نهایی

**اول کد و اسناد واقعی را بررسی کن؛ سپس کار کن. ابتدا Core/Backoffice را تکمیل و اعتبارسنجی کن؛ ساختار انتقال‌یافته را فقط تأیید کن؛ سپس B2C و Mobile را بر پایه Core API بساز.**

هیچ گزارش تاریخی جای بررسی زنده را نمی‌گیرد. هیچ قابلیت ناقصی کامل اعلام نشود. هیچ تغییری خارج از scope انجام نشود. هیچ داده یا فایل کاربر در اثر عملیات Git یا Docker از بین نرود. تمام قابلیت‌های مهم باید هم از نظر کد و هم از نظر تجربه واقعی کاربر و امنیت تأیید شوند.


---

# 18. تصمیم‌ها و موارد باز مهم برای برنامه‌های آینده

## 18.1 B2C و موبایل هنوز نباید منطق Core را تکرار کنند

- Core API مرجع قیمت، اعتبار سفارش، وضعیت workflow، مجوزها و دسترسی فایل‌هاست.
- وب و موبایل فقط مصرف‌کننده APIهای Core هستند؛ اتصال مستقیم به PostgreSQL ممنوع است.
- ابتدا API contractها و نیازمندی‌های Business Analysis را استخراج کن؛ بعد UI بساز.
- در صورت نبود endpoint موردنیاز، آن را در Core با تست و مستندات اضافه کن؛ کلاینت را با mock یا محاسبه موازی پر نکن.

## 18.2 وضعیت B2C/Mobile در زمان handoff

در اطلاعات قابل بازیابی، وجود و تکمیل برنامه Next.js در `02_WEB_APP` یا برنامه واقعی در `03_MOB_APP` اثبات نشده است. همچنین فناوری نهایی Mobile تأیید قطعی نشده است. در نتیجه، هر دو مورد باید با repository بررسی شوند؛ اگر کدی وجود دارد، آن را ادامه بده و بازنویسی نکن.

## 18.3 موارد باز با اولویت بالا

1. ممیزی زنده repo/Git/runtime و تعیین وضعیت دقیق current branch/commit/push.
2. اجرای مجدد تست‌های فعلی و browser acceptance؛ آخرین اعداد گزارش‌شده در 2026-10-17 عبارت‌اند از Core 262/262، Architecture 4/4، Backoffice 154/154 و PostgreSQL Integration 160/160؛ Chrome هنوز pending بوده است.
3. inventory کامل فیلدهای تاریخ و تأیید `LocaleDateService` در همه صفحات/DTOها/DB mappingها.
4. پذیرش مرورگری مسیرهای New Order/Pricing و Order 360.
5. مستندسازی شواهد و تعیین تکلیف دو Draft مشکوک؛ بدون دست‌کاری خودسرانه.
6. audit و رفع موانع Supplier Prompt 4؛ سپس بررسی/اجرای Prompt 5 در صورت تکمیل‌نبودن آن.
7. تأیید ساختار انتقال Backoffice که در 2026-10-07 انجام‌شده گزارش شده است.
8. پس از Core acceptance، طراحی B2C و Mobile بر اساس اسناد و APIهای واقعی.

# 19. قاعده اولویت‌بندی شواهد

اگر گزارش‌ها متعارض‌اند، این ترتیب را رعایت کن:

1. وضعیت زنده فایل‌ها، Git، DB، تست‌ها و runtime در repository فعلی؛
2. آخرین گزارش کاربر با تاریخ مشخص؛
3. گزارش‌های قبلی اجرای عامل؛
4. فرض‌ها یا roadmapهای پیشنهادی.

اعداد تست تاریخی را در گزارش جاری به‌عنوان تست تازه اجراشده ننویس. یک قابلیت تنها وقتی `verified` است که شواهد کد، تست و پذیرش متناسب با نوع قابلیت موجود باشد.
