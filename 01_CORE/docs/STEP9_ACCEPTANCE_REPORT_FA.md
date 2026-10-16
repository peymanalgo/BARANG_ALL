# گزارش پذیرش STEP ۹ — Production jobs و Equipment

## وضعیت و provenance

این گزارش برای checkout step2-clean/01_CORE و branch codex/step2-inventory-unit-contracts است. baseline پذیرفته‌شدهٔ STEP ۸، application 09f5a656dc7fb25a95929ae8d2fe3a1564989e99 و report c29755c8e2ce5e0e7cabdbe2da411bc13d632487، حفظ شده است. source نهایی تست‌شده پس از اصلاح UI، commit eec768c2f0ed95941d16403c95e6d378498ba37b است؛ پیاده‌سازی canonical STEP ۹ از 71b61c057bb0b98abfdb82d9366c87a8083c7b7c آمده است.

محیط browser ایزوله با Compose project barang-step9-acceptance، API روی 5859، Backoffice روی 5550، PostgreSQL روی 5559، Redis روی 6389 و MinIO روی 9059/9060 اجرا شد. volume توسعه و barang_core استفاده نشد. migration chain کامل روی DB ایزوله اجرا شد. image فعلی Backoffice با provenance eec768c2f0ed95941d16403c95e6d378498ba37b ساخته شد و digest آن sha256:0d8a79aaf3da8cd2a62846f2b1c11e4da92f1d37362e809bf8eff917c5d24009 است. API ایزوله از همان source implementation با digest قبلی sha256:83e987a11c8eb573d686c9feb3b8b144c37784b12f1199cb545300c41f78d2a85 اجرا شد.

## پیاده‌سازی و defectهای اصلاح‌شده

- generation در ProductionAdministration برای هر OrderItem و PrintOperation job مستقل می‌سازد؛ GenerationKey، قفل order و retry idempotent از ادغام same-SKU جلوگیری می‌کنند. artwork/file version و quantity از operation/order item منجمد می‌شوند.
- runtime واقعی WorkflowDefinition و adapter legacy completion را به اعتبارسنجی canonical Production متصل می‌کند؛ completion عمومی بدون job لازم عبور نمی‌کند.
- Start/Resume، operator فعال، permission، company، equipment فعال و capability صریح، و warehouse issue برای همان item را دوباره بررسی می‌کنند. Complete مقدار persisted کافی را الزام می‌کند.
- defect browser واقعی اصلاح شد: workspace مقدار enum ProductionPriority را به شکل رشته Normal می‌فرستاد و API عدد enum می‌خواست؛ payload اکنون 1/Normal عددی است. تست نیز به‌جای انتخاب DesignPreparation، job دوم HeatPress را برای تجهیزات PRESS انتخاب می‌کند.
- assertion قبلی وجود #blazor-error-ui را با وجود DOM ثابت layout خطا می‌دانست؛ به visibility واقعی banner اصلاح شد. در اجرای نهایی banner مخفی ماند و هیچ circuit error ثبت نشد.

## نتایج آزمون

| دروازه | نتیجه | مدرک |
|---|---:|---|
| API Release build | PASS | current source، بدون warning/error |
| Backoffice Release build | PASS | current source، بدون warning/error |
| Unit | PASS 229/229 | .artifacts/step9-regression/step9-unit-final.trx |
| Backoffice | PASS 184/184 | .artifacts/step9-regression/step9-backoffice-final.trx |
| Full Integration قبلی روی clean DB همین source implementation | PASS 133/133 | .artifacts/step9-integration/step9-integration.trx |
| Full Integration rerun بعد از UI-only commit | INCOMPLETE/HANG | step9-integration-final*.trx؛ runner پس از بیش از ۹۰ ثانیه بدون query فعال و با process باقی‌مانده متوقف شد؛ stack/DB activity بررسی و process فقط پس از timeout terminate شد |
| Production browser نهایی | PASS 1/1 | .artifacts/step9-browser/step9-production-final.trx |

اجرای Full Integration جدید در DBهای تازهٔ barang_step9d_test و barang_step9e_test تا مرحلهٔ test-host پیش رفت، اما summary کامل تولید نکرد: pg_stat_activity query فعال نداشت و processهای vstest باقی ماندند. این را PASS حساب نکرده‌ام. نتیجهٔ 133/133 موجود مربوط به اجرای کامل قبلی همین implementation و migration chain clean است؛ تغییرات بعدی فقط Backoffice workspace و تست browser بودند و به API/Integration code دست نزدند.

## browser acceptance انجام‌شده

با system Chrome و login واقعی step9.admin در company ایزوله:

- production workspace باز شد و دو job مستقل برای fixture order مشاهده شد؛ job دوم HeatPress شامل OrderItem و فیلدهای Artwork/File بود.
- Generate از UI و retry از UI اجرا شد و تعداد jobها از ۲ بیشتر نشد.
- operator و equipment برای HeatPress از UI ارسال شد؛ PRESS سازگار پذیرفته شد.
- Plan از UI با payload صحیح enum اجرا شد.
- Start بدون warehouse issue با پاسخ معتبر 422 production.warehouse_issue_required رد شد؛ task/job به InProgress نرفت.
- پس از خطا banner Blazor visible نشد؛ screenshot نهایی در .artifacts/step9-browser/step9-production-issue-gate.png و TRX در .artifacts/step9-browser/step9-production-final.trx است.

## مواردی که هنوز اثبات نشده‌اند

این اجرا هنوز پذیرش کامل STEP ۹ نیست، چون fixture قابل‌استفادهٔ browser فقط یک OrderItem و دو job تولیدی داشت و warehouse issue معتبر در آن ایجاد نشده بود. بنابراین موارد زیر PASS اعلام نمی‌شوند:

1. دو OrderItem مستقل با SKU یکسان و jobهای جداگانه.
2. start و completion موفق پس از issue معتبر، با actor/timestamp/outcome persisted.
3. رد browser برای equipment inactive، foreign-company و disabled-after-assignment.
4. تکمیل ناکافی، generic workflow bypass، reload/history کامل و transition واقعی task.
5. candidate graph واقعی شامل ProductionPlanning/DesignPreparation/Printing/HeatPress از طریق Designer/versioning.

در نتیجه، deployment توسعهٔ barang-core انجام نشد و STEP ۹ در این commit FAIL / NOT ACCEPTED است؛ deployment ایزولهٔ بالا فقط برای acceptance اجرا شد. blockerها فنی/قابل‌تکرارند، نه مجوزی: باید setup معتبر warehouse issue، same-SKU دو-item و graph واقعی آماده و browser matrix کامل دوباره اجرا شود. STEP ۱۰ و QC شروع نشده است.

## migration و recovery

business schema جدیدی برای این اصلاح لازم نبود؛ migration 20261016085258_AddStep9ProductionWorkflowGates snapshot-alignment است و migration قبلی 20261016080912_AlignReceiptModel مالک receipt schema باقی ماند. migration chain کامل روی DB تازهٔ ایزوله اعمال شد. هیچ migration، reset، تغییر credential یا mutation روی development انجام نشد. برای rollout بعدی، backup و restore validation توسعه باید قبل از هر migration عملیاتی انجام شود.
