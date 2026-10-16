# گزارش پذیرش STEP ۹ — Production jobs و Equipment

## وضعیت و provenance

این گزارش برای checkout step2-clean/01_CORE و branch codex/step2-inventory-unit-contracts است. baseline پذیرفته‌شدهٔ STEP ۸، application 09f5a656dc7fb25a95929ae8d2fe3a1564989e99 و report c29755c8e2ce5e0e7cabdbe2da411bc13d632487، حفظ شده است. source نهایی تست‌شده و image pair ایزوله، commit `e4463181d006a66b277221a7e7c3ae5768ee65d7` است؛ پیاده‌سازی canonical STEP ۹ از 71b61c057bb0b98abfdb82d9366c87a8083c7b7c آمده است.

محیط browser ایزوله با Compose project `barang-step9b-acceptance`، API روی 5869، Backoffice روی 5560، PostgreSQL روی 5569، Redis روی 6399 و MinIO روی 9069/9070 اجرا شد. volume توسعه و `barang_core` استفاده نشد. migration chain کامل روی DB `barang_step9b_test` اجرا شد. candidate واقعی BARANG.ORDER از طریق APIهای پشتیبانی‌شدهٔ Designer import/save/validate/publish شد؛ نتیجه نسخهٔ ۴ در همین محیط ایزوله است و instanceهای قبلی دست‌کاری نشدند. image manifestهای نهایی API و Backoffice به‌ترتیب `sha256:19e0fd9cc74b49fbdb62d557cbaf392d68dda4a4a3efaada1d8b7f8aaa3cabe0` و `sha256:8dcea62e2fbef80e0a6855e17c401307ecac5507a0c9167f017c45e3afb94bc2` هستند؛ هر دو کانتینر `BARANG_BUILD_COMMIT=e4463181d006a66b277221a7e7c3ae5768ee65d7` را گزارش می‌کنند.

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
| Full Integration فعلی پس از اصلاح RLS endpoint | PASS 133/133 | `.artifacts/step9-integration-postfix/step9-integration-postfix.trx`؛ زمان 3:42 با `--blame-hang-timeout 5m` |
| Integration diagnostics | PASS / complete | `.artifacts/step9-integration-postfix.log` و host/datacollector logs؛ stall قبلی ناشی از cutoff سکوت ۹۰ثانیه‌ای بود، نه lock یا deadlock |
| Production browser موفقیت واقعی | PASS 1/1 | `.artifacts/step9-browser/step9-production-issue-gate.png`؛ Generate retry، assignment، Plan، Start، Complete و reload از UI |
| API negative gates | PASS | `warehouse_issue_required`، `incompatible_equipment` و `equipment_unavailable` با status 422 در fixture ایزوله |

اجرای مقایسه‌ای قبلی در DBهای تازهٔ barang_step9d_test و barang_step9e_test تا مرحلهٔ test-host پیش رفت، اما summary کامل تولید نکرد: pg_stat_activity query فعال نداشت و processهای vstest باقی ماندند. آن اجرا PASS محسوب نشده است. اجرای نهایی با setup clean PostgreSQL، role initialization پشتیبانی‌شده، `-m:1`، logger کنسول/TRX، diag و `--blame-hang-timeout 5m` کامل شد و نتیجهٔ 133/133 را برای source فعلی ثبت کرد؛ cutoff سکوت ۹۰ثانیه‌ای علت توقف گزارش قبلی بود، نه lock یا deadlock.

## browser acceptance انجام‌شده

با system Chrome و login واقعی `step9b.admin` در company ایزوله و fixture ساخته‌شده از APIهای پشتیبانی‌شده:

- دو OrderItem مستقل با یک ProductVariant/SKU ساخته شد؛ برای هرکدام DesignPreparation، Print و HeatPress جداگانه (۶ job) ایجاد شد. jobهای Print هر دو به `ArtworkVersionId=5dc25e80-62d9-4144-9663-4f7d076e3160` و `FileVersionId=681336b6-d7de-4a24-bf07-6bedcb5f2dc5` bind شدند.
- Generate از UI و retry از UI اجرا شد؛ تعداد jobهای هر OrderItem از ۳ بیشتر نشد و generation keyها duplicate نساختند.
- operator و equipment برای HeatPress از UI ارسال شد؛ PRESS سازگار پذیرفته شد.
- Plan از UI با payload صحیح enum اجرا شد.
- fixture از مسیر reservation و warehouse issue واقعی ساخته شد؛ issue transactionها برای هر دو OrderItem persisted شد.
- Start معتبر از UI با status persisted `InProgress` و سپس Complete با quantity مستقل همان OrderItem انجام شد؛ پس از reload status به `Completed` رسید و history/actor/timestamp از endpoint history قابل مشاهده بود.
- Start بدون warehouse issue در order جداگانه با `422 production.warehouse_issue_required` رد شد؛ job به InProgress نرفت.
- equipment `CUTTER` برای HeatPress با `422 production.incompatible_equipment` رد شد و تجهیز پس از assignment با deactivation، Start را با `422 production.equipment_unavailable` رد کرد.
- پس از عملیات banner Blazor visible نشد؛ screenshot در `.artifacts/step9-browser/step9-production-issue-gate.png` و TRX focused در خروجی runner ثبت شد.

## مواردی که هنوز اثبات نشده‌اند

موارد باقی‌ماندهٔ خارج از این acceptance browser/API matrix: سناریوی کاربرِ شرکت بیگانه با identity جداگانه در این run دوباره seed نشد و از گیت‌های RLS/Integration و service company-scope پوشش دارد؛ برای rollout production باید همین سناریو در runbook fixture امنیتی نیز اجرا شود. تکمیل ناکافی و generic workflow bypass در Integration STEP5 و gateهای canonical پوشش دارند و در این run با production code تغییر نکردند. هیچ policy جدید QC یا مرحلهٔ STEP10 اضافه نشد.

با توجه به PASS کامل Integration 133/133، Unit 229/229، Backoffice 184/184، buildهای Release، graph واقعی Designer و successful/negative browser/API evidence، STEP ۹ **PASS** است. deployment توسعهٔ barang-core در این run انجام نشد؛ deployment ایزوله با image pair هم‌نسخه برای mutation acceptance انجام شد و کانتینرهای توسعه، داده، volume و credentialها دست‌نخورده ماندند. STEP ۱۰ و QC شروع نشده است.

## migration و recovery

business schema جدیدی برای این اصلاح لازم نبود؛ migration 20261016085258_AddStep9ProductionWorkflowGates snapshot-alignment است و migration قبلی 20261016080912_AlignReceiptModel مالک receipt schema باقی ماند. migration chain کامل روی DB تازهٔ ایزوله اعمال شد. هیچ migration، reset، تغییر credential یا mutation روی development انجام نشد. برای rollout بعدی، backup و restore validation توسعه باید قبل از هر migration عملیاتی انجام شود.

## تحویل local development

پس از backup مالک دیتابیس در `artifacts/barang_core_step9_delivery_20261016.dump`، migrationهای pending از مسیر رسمی EF CLI و با role مهاجرت privileged اجرا شدند. migration history توسعه اکنون به `20261016085258_AddStep9ProductionWorkflowGates` رسیده است؛ runtime همچنان با role کم‌اختیار `barang_app` اجرا می‌شود. volumeهای PostgreSQL، MinIO، Redis و کلیدهای Data Protection، داده‌های business و credentialهای administrator حفظ شدند.

Compose project موجود `barang-core` با pair هم‌نسخهٔ source `e4463181d006a66b277221a7e7c3ae5768ee65d7` بازسازی شد:

- API image: `sha256:e96a25beb4b74603c69587012d134e929d8c436c1e3555d8dba4798e28837c5e`
- Backoffice image: `sha256:06eb0e9fb0a7e06ee3470c660b8341320efba882088ab9f0079fa93e55f8e1dd`
- هر دو کانتینر مقدار `BARANG_BUILD_COMMIT=e4463181d006a66b277221a7e7c3ae5768ee65d7` دارند.
- API readiness روی `:8080` و login روی `http://localhost:5100` هر دو HTTP 200 هستند.

Smoke احراز‌شدهٔ UI login، شرکت جاری و navigation پایه موفق شد. Production stages فعلی definition `BARANG.ORDER` همچنان به version فعال `17` متصل‌اند و nodeهای `WAREHOUSE` و `PRODUCTION` موجودند؛ هیچ graph یا instance توسعه‌ای import، publish یا migrate نشد. candidate version 4 فقط در محیط ایزوله باقی ماند و برای instanceهای جدید نیازمند تصمیم/rollout جداگانه است؛ instanceهای جاری روی version 17 می‌مانند.

محدودیت کشف‌شده در smoke توسعه: صفحهٔ مدیریتی تجهیزات برای فهرست/جزئیات به `/api/v1/company-equipment` درخواست می‌فرستد، در حالی که API فعلی route canonical `/api/v1/equipment` و `/api/v1/equipment/options` دارد؛ بنابراین این درخواست در deployment فعلی 404 می‌شود. این ناسازگاری در این تحویل اصلاح نشده چون خارج از بستهٔ accepted STEP 9 است و اصلاح آن نیازمند تغییر مستقل Backoffice/API و regression جداگانه است. همچنین دیتابیس توسعه رکورد تجهیزات مناسب برای assertion جدولی نداشت؛ در نتیجه test localization وابسته به row با empty-state شکست خورد و به‌عنوان PASS ثبت نشده است. این مورد blocker صریح برای پذیرش کامل صفحهٔ مدیریت تجهیزات است، نه نشانهٔ شکست image provenance یا migration.
