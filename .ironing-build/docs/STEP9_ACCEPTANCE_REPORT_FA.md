# گزارش پذیرش STEP ۹ — Production jobs و Equipment

## وضعیت و provenance

این گزارش برای checkout step2-clean/01_CORE و branch codex/step2-inventory-unit-contracts است. baseline پذیرفته‌شدهٔ STEP ۸، application 09f5a656dc7fb25a95929ae8d2fe3a1564989e99 و report c29755c8e2ce5e0e7cabdbe2da411bc13d632487، حفظ شده است. source فعلی تست‌شده commit `31153344a3be65f89d295b7aa7b3b286a074acbc` است؛ این commit قرارداد canonical تجهیزات را در API و Backoffice هم‌تراز می‌کند و تغییر lifecycle جست‌وجو را نیز شامل می‌شود.

محیط browser ایزوله با Compose project `barang-step9b-acceptance`، API روی 5869، Backoffice روی 5560، PostgreSQL روی 5569، Redis روی 6399 و MinIO روی 9069/9070 اجرا شد. volume توسعه و `barang_core` استفاده نشد. migration chain کامل روی DB `barang_step9b_test` اجرا شد. candidate واقعی BARANG.ORDER از طریق APIهای پشتیبانی‌شدهٔ Designer import/save/validate/publish شد؛ نتیجه نسخهٔ ۴ در همین محیط ایزوله است و instanceهای قبلی دست‌کاری نشدند. image manifestهای نهایی پس از اصلاح API `sha256:cedf3019d50f9406a38bec1f09269b0a62a9a78faf2b57614143dc8866f4c632` و Backoffice `sha256:d08b9278bca628f69a0971b61ccde7f975d2997eaba2cd8ad20195787a2ff00a` هستند؛ هر دو کانتینر `BARANG_BUILD_COMMIT=31153344a3be65f89d295b7aa7b3b286a074acbc` را گزارش می‌کنند.

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
| Full Integration فعلی پس از اصلاح تجهیزات | PASS 133/133 | `.artifacts/step9-equipment-full/step9-equipment-full.trx`؛ زمان 3:43 با `--blame-hang-timeout 5m` |
| Integration diagnostics | PASS / complete | `.artifacts/step9-equipment-full/step9-equipment-full.trx` و blame/host logs؛ stall قبلی ناشی از cutoff سکوت ۹۰ثانیه‌ای بود، نه lock یا deadlock |
| Production browser موفقیت واقعی | PASS 1/1 | `.artifacts/step9-equipment-browser/step9-production-issue-gate.png`؛ fixture تازه از `tests/Acceptance/Step9ProductionFixture.ps1` و Generate retry، assignment، Plan، Start، Complete و reload از UI |
| Equipment browser CRUD/status/localization | PASS 4/4 | `EquipmentLocalizationE2ETests`؛ fa/ar RTL، en LTR، create/edit/deactivate، canonical option lookup و reload |
| Equipment API↔Backoffice contract | PASS | `CatalogFoundationTests.AuthenticatedCatalogFlowPersistsRelationshipsAuditAndConcurrency`؛ list/detail/create/update/activation/options، full Equipment fields و optimistic concurrency |
| API negative gates | PASS | `warehouse_issue_required`، `incompatible_equipment` و `equipment_unavailable` با status 422 در fixture ایزوله |

اجرای مقایسه‌ای قبلی در DBهای تازهٔ barang_step9d_test و barang_step9e_test تا مرحلهٔ test-host پیش رفت، اما summary کامل تولید نکرد: pg_stat_activity query فعال نداشت و processهای vstest باقی ماندند. آن اجرا PASS محسوب نشده است. اجرای نهایی با setup clean PostgreSQL، role initialization پشتیبانی‌شده، `-m:1`، logger کنسول/TRX، diag و `--blame-hang-timeout 5m` کامل شد و نتیجهٔ 133/133 را برای source فعلی ثبت کرد؛ cutoff سکوت ۹۰ثانیه‌ای علت توقف گزارش قبلی بود، نه lock یا deadlock.

## browser acceptance انجام‌شده

با system Chrome و login واقعی `step9b.admin` در company ایزوله و fixture تازه ساخته‌شده از APIهای پشتیبانی‌شده:

- دو OrderItem مستقل با یک ProductVariant/SKU ساخته شد؛ برای هرکدام DesignPreparation، Print و HeatPress جداگانه (۶ job) ایجاد شد. jobهای Print هر دو به `ArtworkVersionId=5dc25e80-62d9-4144-9663-4f7d076e3160` و `FileVersionId=681336b6-d7de-4a24-bf07-6bedcb5f2dc5` bind شدند.
- Generate از UI و retry از UI اجرا شد؛ تعداد jobهای هر OrderItem از ۳ بیشتر نشد و generation keyها duplicate نساختند.
- operator و equipment برای HeatPress از UI ارسال شد؛ PRESS سازگار پذیرفته شد.
- Plan از UI با payload صحیح enum اجرا شد.
- fixture از مسیر reservation و warehouse issue واقعی ساخته شد؛ issue transactionها برای هر دو OrderItem persisted شد.
- Start معتبر از UI با status persisted `InProgress` و سپس Complete با quantity مستقل همان OrderItem انجام شد؛ پس از reload status به `Completed` رسید و history/actor/timestamp از endpoint history قابل مشاهده بود.
- Start بدون warehouse issue در order جداگانه با `422 production.warehouse_issue_required` رد شد؛ job به InProgress نرفت.
- equipment `CUTTER` برای HeatPress با `422 production.incompatible_equipment` رد شد و تجهیز پس از assignment با deactivation، Start را با `422 production.equipment_unavailable` رد کرد.
- پس از عملیات banner Blazor visible نشد؛ screenshot در `.artifacts/step9-browser/step9-production-issue-gate.png` و TRX focused در خروجی runner ثبت شد.
- صفحهٔ Equipment Management دیگر به `company-equipment` وابسته نیست: list و search از `GET /api/v1/equipment/`، detail از `GET /api/v1/equipment/{id}`، create/update از `POST/PUT /api/v1/equipment/`، status از `POST /api/v1/equipment/{id}/activation` و lookup از `GET /api/v1/equipment/options` استفاده می‌کند. API پاسخ canonical را بدون wrapper `equipment` برمی‌گرداند و تمام فیلدهای مدل Equipment در DTO/request حفظ شده‌اند.
- در مرورگر، lookupهای `equipment.type/status` با API مدیریت lookup در محیط ایزوله ایجاد/تأیید شدند؛ create، edit، status filter و reload واقعی با شناسه‌های تازه موفق شد. اصلاح lifecycle باعث شد پس از جست‌وجو spinner روی حالت loading نماند.

## مواردی که هنوز اثبات نشده‌اند

موارد باقی‌ماندهٔ خارج از این acceptance browser/API matrix: سناریوی کاربرِ شرکت بیگانه با identity جداگانه در این run دوباره seed نشد و از گیت‌های RLS/Integration و service company-scope پوشش دارد؛ برای rollout production باید همین سناریو در runbook fixture امنیتی نیز اجرا شود. تکمیل ناکافی و generic workflow bypass در Integration STEP5 و gateهای canonical پوشش دارند و در این run با production code تغییر نکردند. هیچ policy جدید QC یا مرحلهٔ STEP10 اضافه نشد.

با توجه به PASS کامل Integration 133/133، Unit 229/229، Backoffice 184/184، buildهای Release، graph واقعی Designer و successful/negative browser/API evidence، STEP ۹ **PASS** است. deployment توسعهٔ barang-core در این run انجام نشد؛ deployment ایزوله با image pair هم‌نسخه برای mutation acceptance انجام شد و کانتینرهای توسعه، داده، volume و credentialها دست‌نخورده ماندند. STEP ۱۰ و QC شروع نشده است.

## migration و recovery

business schema جدیدی برای این اصلاح لازم نبود؛ migration 20261016085258_AddStep9ProductionWorkflowGates snapshot-alignment است و migration قبلی 20261016080912_AlignReceiptModel مالک receipt schema باقی ماند. migration chain کامل روی DB تازهٔ ایزوله اعمال شد. هیچ migration، reset، تغییر credential یا mutation روی development انجام نشد. برای rollout بعدی، backup و restore validation توسعه باید قبل از هر migration عملیاتی انجام شود.

## تحویل local development

پس از backup مالک دیتابیس در `artifacts/barang_core_step9_delivery_20261016.dump`، migrationهای pending از مسیر رسمی EF CLI و با role مهاجرت privileged اجرا شدند. migration history توسعه اکنون به `20261016085258_AddStep9ProductionWorkflowGates` رسیده است؛ runtime همچنان با role کم‌اختیار `barang_app` اجرا می‌شود. این اصلاح equipment migration جدیدی ندارد؛ همهٔ ستون‌های موردنیاز در schema فعلی موجود بودند. volumeهای PostgreSQL، MinIO، Redis و کلیدهای Data Protection، داده‌های business و credentialهای administrator حفظ شدند.

Compose project موجود `barang-core` پس از گذر regression gate با pair هم‌نسخهٔ source `31153344a3be65f89d295b7aa7b3b286a074acbc` بازسازی شد:

- API image: `sha256:607dd5d895c5456a229dca5fde2578ecf0a5e70b25d80654a7221003f3aa2de5`
- Backoffice image: `sha256:8294cc7a3b28d4549fa2cced0fc9fd269e150f141b0e46ab26296e42992f7b8c`
- API readiness روی `:8080` و login روی `http://localhost:5100` هر دو HTTP 200 هستند.

Smoke احراز‌شدهٔ UI login، شرکت جاری و navigation پایه موفق شد. Production stages فعلی definition `BARANG.ORDER` همچنان به version فعال `17` متصل‌اند و nodeهای `WAREHOUSE` و `PRODUCTION` موجودند؛ هیچ graph یا instance توسعه‌ای import، publish یا migrate نشد. candidate version 4 فقط در محیط ایزوله باقی ماند و برای instanceهای جدید نیازمند تصمیم/rollout جداگانه است؛ instanceهای جاری روی version 17 می‌مانند.

ناسازگاری قبلی `company-equipment` یک defect واقعی و task-related بود و در commit `31153344...` اصلاح شد؛ دیگر blocker صفحهٔ Equipment Management نیست. برای historyهای خرابی/تعمیر/نگهداری/بازرسی، در source فعلی entity/service canonical و route متناظر وجود ندارد؛ این commit endpoint جعلی ایجاد نکرد و این تب‌ها خارج از CRUD/status/capability/options قرارداد فعلی باقی مانده‌اند. این موضوع یک تصمیم/بستهٔ مستقل برای گسترش canonical Equipment history است، نه مانع مسیر تولید STEP 9.

## شواهد image و smoke پس از اصلاح

در محیط ایزوله، هر دو سرویس پس از rebuild مقدار `BARANG_BUILD_COMMIT=31153344a3be65f89d295b7aa7b3b286a074acbc` را گزارش کردند. image manifestها: API `sha256:cedf3019d50f9406a38bec1f09269b0a62a9a78faf2b57614143dc8866f4c632` و Backoffice `sha256:d08b9278bca628f69a0971b61ccde7f975d2997eaba2cd8ad20195787a2ff00a`. health/login و مرورگر Equipment روی همین pair موفق شدند.

در development rollout، هیچ graph/instance/workflow definitionی تغییر داده نشد. نسخهٔ فعال `BARANG.ORDER` همچنان 17 است و nodeهای `WAREHOUSE` و `PRODUCTION` را دارد؛ سرویس‌های canonical مرحله‌های STEP 3–5/9 در مسیرهای runtime اعمال می‌شوند، اما این گزارش publish شدن یک graph جدید یا مهاجرت instanceها را ادعا نمی‌کند. اگر rollout نسخهٔ candidate لازم باشد، برای instanceهای جدید باید جداگانه تصمیم و publish supported انجام شود؛ instanceهای جاری روی binding نسخهٔ 17 باقی می‌مانند.
