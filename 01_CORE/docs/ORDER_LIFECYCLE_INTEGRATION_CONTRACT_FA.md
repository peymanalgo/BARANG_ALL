# قرارداد یکپارچگی چرخه عمر سفارش BARANG — STEP 1

تاریخ: ۱۴۰۵/۰۷/۲۰ برابر با ۲۰۲۶-۱۰-۱۲

## وضعیت و دامنه

این سند خروجی STEP 1 از برنامه ۱۵ مرحله‌ای یکپارچگی است. هدف آن **تأیید وضعیت فعلی و تعریف قرارداد برای مراحل بعدی** است؛ هیچ integration، migration، reset، deploy یا تغییر داده‌ای در این مرحله انجام نشده است.

بازبینی از این منابع انجام شد:

- routeهای واقعی Backoffice: `src/BARANG.Backoffice/Components/Pages/OrderWorkspace.razor` با `/workspace/orders` و `OrderWizard.razor` با `/orders/new` و `/orders/{OrderId:guid}/edit`؛ stepهای wizard و اجزای `OrderPrintStep.razor` و `OrderStep4Panel.razor`.
- API: `src/BARANG.Core.Api/Orders/OrderEndpoints.cs`، `Inventory/InventoryEndpoints.cs`، `Finance/FinanceEndpoints.cs`، `Production/ProductionEndpoints.cs`، `Quality/QualityEndpoints.cs`، `Logistics/LogisticsEndpoints.cs` و `Workflow/WorkflowEndpoints.cs`.
- application/domain/infrastructure: `OrderContracts.cs`، `OrderModels.cs`، `OrderAdministration.cs`، `InventoryAdministration.cs`، `PricingAdministration.cs`، `WorkflowDefinitionRuntime.cs` و `WorkflowBusinessCompletionAdapter.cs`.
- قراردادهای مستند: `docs/ORDER_AGGREGATE.md`، `docs/INVENTORY_AND_PURCHASING.md`، `docs/INVENTORY_PURCHASING_STEP4_BLUEPRINT.md`، `docs/PRICING.md`، `docs/PRICING_MODULE_ANALYSIS.md`، `docs/FINANCE_AND_INVOICE.md`، `docs/WORKFLOW_AND_HUMAN_TASKS.md`، `docs/PACKAGING_SHIPPING_DELIVERY.md` و `docs/ARCHITECTURE.md`.
- تست‌ها: `tests/BARANG.Core.IntegrationTests/Database/OrderE2ETests.cs`، `PricingE2ETests.cs`، `FinanceE2ETests.cs`، `Step5WorkflowAcceptanceTests.cs`، `InventoryFoundationTests.cs` و تست‌های Backoffice سفارش/رزرو/خرید.

وضعیت‌ها در این سند با کدهای واقعی source نوشته شده‌اند؛ حالت‌های جدید فرض نشده‌اند.

## نتیجه اجرایی ممیزی

### وضعیت repository و runtime

| مورد | نتیجه |
|---|---|
| ریشه واقعی Git | `D:/00_BARANG`؛ مسیر کاری کد `D:/00_BARANG/01_CORE` است و repository root یک سطح بالاتر است |
| شاخه | `main` |
| remote | `origin = https://github.com/peymanalgo/BARANG_ALL.git` |
| working tree | از قبل بسیار کثیف؛ ۴۰۹ تغییر/فایل نامرتبط در شروع بررسی مشاهده شد. این STEP فقط دو فایل docs را اضافه می‌کند |
| deployment | `barang-core-api-1` و `barang-core-backoffice-1` healthy؛ PostgreSQL، Redis و MinIO نیز running/healthy |
| HTTP | Backoffice روی `http://localhost:5100/` به `/login` redirect می‌کند؛ `/health/ready` روی API پاسخ ۲۰۰ داد |
| DB | PostgreSQL توسعه با role `barang` و database `barang_core` از داخل container، فقط با SELECT، قابل دسترسی بود |
| ابزارها | .NET SDK `10.0.400`، Node `v24.18.1`، PowerShell 7.6.6، ripgrep 15.2.0، Docker Compose |

### snapshot خواندنی DB توسعه

این اعداد وضعیت همین محیط توسعه در زمان ممیزی هستند و نبود functionality را ثابت نمی‌کنند:

| داده | تعداد/نتیجه |
|---|---:|
| orders | ۳۵: `Draft=2`، `Submitted=11`، `Cancelled=22` |
| order_items | ۴۹ |
| current/finalized price snapshots | ۳۴؛ `14` نهایی/فعلی و `20` غیرنهایی/غیرفعال فعلی |
| discount_rules / tax_rules | ۱ / ۱ |
| stock_reservations / stock_transactions / stock_shortages | ۰ / ۰ / ۰ |
| production_jobs / qc_inspections | ۰ / ۰ |
| packages / shipments | ۰ / ۰ |
| notifications / outbox_messages | ۰ / ۰ |
| workflow_instances | ۱۴؛ `Running=11` و `Completed=3` |
| workflow_history / workflow_runtime_tasks | ۲۵۴ / ۳۵ |
| legacy `human_tasks` | ۰ |

### اصلاح ادعاهای ممیزی قبلی

1. **تأیید شد:** submit سفارش validation، freeze snapshot، شماره‌گذاری و شروع workflow definition را در یک transaction انجام می‌دهد.
2. **تأیید شد:** API و implementation مستقل reservation، release، consume و expiry وجود دارند و قفل row/انقضای idempotency را پیاده کرده‌اند.
3. **اصلاح لازم:** `OrderAdministration.FinalizeAsync` هیچ `IInventoryAdministration.ReserveAsync` را صدا نمی‌زند؛ پس submit فعلاً stock check/reservation ندارد.
4. **اصلاح لازم:** مسیر واقعی سفارش به `IWorkflowDefinitionRuntime` و `WorkflowDefinitionRuntime.StartAsync/CompleteTaskAsync` متصل است. `WorkflowBusinessCompletionAdapter.ExecuteAsync` در این مسیر فراخوانی نمی‌شود؛ آن adapter شامل Finance/Production و بررسی مستقیم stageهاست اما consumer واقعی تعریف-runtime نیست.
5. **تأیید جزئی:** Finance API واقعی است، اما definition-runtime در `CompleteTaskAsync` به Finance service call نمی‌کند. adapter قدیمی‌تر در مسیر دیگر، در FinancialApproval در صورت عدم approval، `DecideAsync(Approved)` را اجرا می‌کند؛ این معادل تأیید انسانی مستقل نیست.
6. **تأیید شد:** `PricingAdministration.CalculateInsideTransactionAsync` در finalization قیمت raw/print/service/design/packaging/shipping را snapshot می‌کند.
7. **شکاف تأییدشده:** final pricing path از `rule.Applies(date, order.CustomerId, order.Source)` استفاده می‌کند و `CustomerGroupId` را به آن نمی‌دهد؛ tax هم در همین path با انتخاب ساده‌ترین `TaxRule` محاسبه می‌شود و `ResolveTaxAsync`/tax status را فراخوانی نمی‌کند. بنابراین customer group و resolver کامل discount/tax در final snapshot فعلاً اثبات نشده و برای customer group به‌طور مستقیم مصرف نمی‌شود.
8. **تأیید شد:** receipt خرید در `InventoryAdministration.ReceiveAsync` در همان transaction stock movement با source `PURCHASE_ORDER` می‌سازد.
9. **تأیید شد:** production، quality و logistics مدل/API کامل دارند؛ اما DB توسعه در زمان بررسی از job/inspection/package/shipment خالی بود، پس runtime journey آنها قابل اثبات نیست.

## چرخه فعلی واقعی

### ۱. ساخت Draft و wizard

| مرحله/trigger | actor و UI | API/handler | سرویس و persistence | workflow/events | failure/retry/test |
|---|---|---|---|---|---|
| Create draft | کاربر دارای `order.create`؛ `/orders/new` | `POST /api/v1/orders` → `IOrderAdministration.CreateDraftAsync` | `orders` با status=`Draft` و context شرکت؛ سفارش تا identity ساخته می‌شود | timeline/audit سفارش؛ outbox business مشخصی مشاهده نشد | validation و company scope؛ تست در `OrderE2ETests` |
| Header/customer/address | کاربر دارای `order.manage`؛ wizard header و shipping step | `PUT /api/v1/orders/{id}`، `PUT /shipping-address` و business-profile API | header، customer، branch، address snapshot و profile؛ version concurrency | timeline/audit؛ بدون integration موجودی | فقط Draft قابل تغییر؛ stale version خطای conflict؛ `ORDER_AGGREGATE.md` |
| Item/SKU/quantity | wizard step item | `POST/PUT/POST remove /items` | `order_items`؛ quantity مثبت، same-company Product/Variant، حذف منطقی با `IsActive=false` | price تا submit فقط recalculation است | item/variant validation؛ tests order E2E و unit |
| Print/design/file | `OrderPrintStep` و Design Bank | `POST/PUT/remove print-operations` و file-link endpoints | `order_print_operations`, `file_links`, exact artwork/file version | upload به file-processing queue می‌رود؛ design price در final pricing می‌آید | فایل failed/invalid reject؛ `ArtworkFileAdministration` و `FileProcessingWorker` |
| Services/packaging/shipping fields | wizard step 4 | `POST/PUT /orders/{id}/services`، business-profile PUT | `order_service_selections`, `order_business_profiles` | tariff resolution در submit؛ logistics هنوز جداست | duplicate service/scope/quantity validation در `OrderStep4Panel` |
| Draft pricing | wizard pricing/review یا API | `POST /api/v1/orders/{id}/pricing/recalculate[‑with-manual-discount]` | current draft snapshots supersede می‌شوند؛ child snapshots overwrite نمی‌شوند | audit `pricing.order.recalculated` | order باید Draft و version صحیح باشد؛ `PricingE2ETests` |

### ۲. Submit و approval

| مرحله | وضعیت فعلی تأییدشده |
|---|---|
| Submit | `POST /api/v1/orders/{id}/submit` و `finalize` هر دو `FinalizeAsync` را صدا می‌زنند؛ validation، snapshot محصول، pricing finalize، sequence `barang.order_number_seq`، status=`Submitted`، timeline و audit در transaction انجام می‌شود. retry برای سفارش submitted workflow موجود را برمی‌گرداند و شماره جدید نمی‌سازد. |
| Workflow start | `OrderAdministration` از `IWorkflowDefinitionRuntime` استفاده می‌کند؛ definition فعال `BARANG.ORDER`، `WorkflowDefinitionVersion` و provider instance انتخاب می‌شوند و `workflow_instances`/`workflow_runtime_tasks`/history ساخته می‌شوند. |
| Approval task | در definition runtime، `CompleteTaskAsync` مجوز `workflow.task.complete`، claim/position، version، outcome مجاز و attachment را بررسی می‌کند؛ سپس task را complete و `AdvanceAfterTask` را اجرا می‌کند. |
| Rejection | `WorkflowOutcomeCodes.AuditReject` روی node `AUDIT` سفارش را `AuditRejected` می‌کند؛ outcome عادی rejected بدون اتصال به Order status در `AdvanceAfterTask` فقط transition را انتخاب می‌کند. mapping کامل reject business stages باید تأیید شود. |
| Finance approval | در مسیر definition runtime، `AdvanceAfterTask` هیچ `IFinanceAdministration` call ندارد. `WorkflowBusinessCompletionAdapter` چنین callی دارد اما در مسیر واقعی order definition به آن inject/call نشده است. |

### ۳. Inventory

`InventoryAdministration.ReserveAsync` موجودی را با `LockBalance` و `SELECT ... FOR UPDATE` قفل می‌کند؛ `Available` را می‌سنجد. اگر کافی نباشد `StockShortage` می‌سازد و success ساختاریافته با shortage برمی‌گرداند؛ اگر کافی باشد `StockBalance.Reserve` و `StockReservation.Create` را اجرا می‌کند. reservation شامل `CompanyId`, `OrderItemId`, `WarehouseId`, `ProductVariantId`, `RequestedQuantity`, `ReservedQuantity`, `RemainingQuantity`, `Status`, `ExpiresAtUtc`, `IdempotencyKey` و `Version` است. unique index روی `(CompanyId, IdempotencyKey)` وجود دارد.

اما این method از Order submit، workflow task completion یا production فراخوانی نمی‌شود. `WorkflowBusinessCompletionAdapter` در مسیر قدیمی فقط `AnyAsync` روی reservationهای order item را به‌عنوان readiness بررسی می‌کند؛ این stock check/reserve نیست.

`ReleaseAsync` reservation و balance را قفل و آزاد می‌کند؛ `IssueReservedAsync` reservation و balance را consume کرده و `StockTransaction` نوع `Issue` با source=`RESERVATION` می‌سازد؛ `ExpireAsync` reservationهای منقضی را آزاد می‌کند. هیچ‌یک از مسیر order cancellation یا quantity edit فعلی به‌صورت مستقیم call نمی‌شوند.

### ۴. Production، Quality، Packaging، Shipping، Delivery

- workflow definition runtime بعد از complete task فقط transition definition را انتخاب و `Traverse` می‌کند؛ owner serviceهای Production/Quality/Logistics را فراخوانی نمی‌کند.
- `ProductionAdministration.GenerateAsync` از endpoint `POST /api/v1/production/orders/{orderId}/jobs` قابل فراخوانی است و jobهای DesignPreparation، Print و HeatPress می‌سازد. adapter قدیمی نیز آن را صدا می‌زند، اما مسیر واقعی definition runtime خیر.
- Quality endpointها inspection، result، defect، approve/reject و reproduction را ارائه می‌کنند. definition runtime خودش فقط transition می‌دهد؛ verification owner service یا inspection approved در آن مسیر وجود ندارد.
- Logistics package/shipment APIها مستقل‌اند؛ مدل shipment وضعیت‌های `Ready → Shipped → InTransit → Delivered` و مسیر failure/return دارد. package completion مستنداً به QC approved نیاز دارد، اما در definition runtime service call/gate واقعی دیده نشد.
- DB runtime در این بررسی برای `production_jobs`, `qc_inspections`, `packages` و `shipments` رکوردی نداشت؛ نتیجه «عدم قابلیت» نیست، فقط journey واقعی مشاهده نشد.

### ۵. Purchasing، shortages و returns

- shortage در ReserveAsync ثبت می‌شود، اما اتصال خودکار به `POST /api/v1/purchasing/requests` یا approval workflow وجود ندارد.
- Purchase Request/Order/Receipt lifecycle مستقل است؛ receipt posted stock ledger را در همان transaction افزایش می‌دهد.
- shipment return فقط logistics state و returns warehouse را ثبت می‌کند و طبق `PACKAGING_SHIPPING_DELIVERY.md` refund/accounting یا stock reversal ایجاد نمی‌کند.

### ۶. Notifications/background/outbox

`NotificationAdministration.QueueAsync` فقط با درخواست صریح notification، `OutboxMessage` با event=`notification.requested` می‌سازد. `BackgroundSchedulers` outbox dispatch، SLA scheduler و file processing را ثبت می‌کند. producer صریح برای order submit، task completion، finance decision، shortage، production، QC یا delivery در مسیر بررسی پیدا نشد. DB نیز `notifications=0` و `outbox_messages=0` داشت.

## جدول رفتار فعلی در برابر رفتار موردنیاز

| موضوع | وضعیت فعلی از source/runtime | نیاز قرارداد بعدی | طبقه‌بندی |
|---|---|---|---|
| Draft stock deduction | هیچ deduction در Draft دیده نشد | Draft stock کم نکند | تأییدشده/مستند |
| Stock availability | فقط داخل `ReserveAsync` هنگام reserve | check در stage مصوب و همان owner service | ناقص |
| Reservation at submit | Order submit reserve نمی‌کند | نقطه رسمی reserve باید تعیین شود | شکاف تأییدشده |
| Reservation identity | schema برای company/order item/variant/warehouse/qty/uom ندارد uom در reservation؛ UOM از Product هنگام issue خوانده می‌شود | contract باید UOM snapshot/quantity دقیق را الزام کند | تصمیم لازم |
| Quantity/item change | Draft item edit وجود دارد؛ reservation integration ندارد | قبل از reserve آزاد؛ بعد از reserve delta release/add با version/idempotency | شکاف |
| Consume | فقط explicit reservation issue API | پس از production/QC یا issue point مصوب | تصمیم لازم |
| Release | explicit API و expiry job وجود دارد | cancel، change، reject و expiry باید آن را trigger کنند | شکاف |
| Workflow owner calls | definition runtime task را complete و graph را advance می‌کند | completion adapter باید owner service را call/validate کند | شکاف بحرانی |
| Finance approval | Finance state API موجود؛ runtime definition به آن وصل نیست | approval واقعی و state/version check | شکاف بحرانی |
| Customer group discount | generic resolver ورودی group دارد؛ final order calculator آن را پاس نمی‌دهد | group باید در final snapshot اثر قابل ردیابی داشته باشد | شکاف تأییدشده |
| Tax | generic resolver tax status دارد؛ final calculator انتخاب ساده دارد | tax resolver/eligibility واحد | شکاف |
| Shortage → purchasing | shortage record مستقل | shortage decision/PR linkage و approval | ناقص/تصمیم لازم |
| Production/QC/logistics gates | API و مدل مستقل؛ definition runtime gate owner ندارد | service-level completion + evidence | شکاف |
| Notification | queue/outbox abstraction مستقل | eventهای business پس از commit و retry | ناقص |

## قرارداد پیشنهادی برای مراحل بعد

گزینه پیشنهادی بر اساس قواعد موجود: **Draft هیچ stock deduction نداشته باشد؛ در مرحله `InventoryReservation` workflow، یک command idempotent برای هر OrderItem فعال اجرا شود؛ consume فقط در issue point مصوب پس از QC/قبل از shipment انجام شود؛ cancel/change قبل از consume release کند و بعد از consume فقط با return process جداگانه اصلاح شود.** این انتخاب با blueprint موجود سازگار است که payment/finalization را از consume جدا می‌کند و reservation را مرز صحیح می‌داند.

### هویت reservation

`CompanyId + OrderId + Exact OrderItemId + ProductVariantId + WarehouseId + Quantity + UomCode + IdempotencyKey`.

مقدار باید برای OrderItem فعال، same-company، variant متعلق به product و warehouse فعال باشد. `UomCode` باید در reservation یا allocation snapshot ثبت شود؛ گرفتن UOM از Product در زمان issue برای audit تاریخی کافی نیست اگر master بعداً تغییر کند. این یک تغییر schema پیشنهادی است و باید در مرحله ۲ تأیید شود.

### availability و concurrency

availability = `OnHand - Reserved` برای همان Company/Warehouse/Variant. check و reserve باید در یک transaction با row lock balance، optimistic reservation version و unique idempotency key انجام شود. کمبود باید success با `ShortageId`/مقدار available یا failure استاندارد باشد؛ partial reservation بدون تصمیم BA مجاز نیست.

### نقاط lifecycle پیشنهادی

| نقطه | رفتار پیشنهادی |
|---|---|
| Draft | بدون reserve و بدون movement؛ فقط price/validation |
| Submit | pricing snapshot و workflow start؛ reserve هنوز از نظر business باید با گزینه زیر تصویب شود |
| InventoryReservation task | availability check و reserve کامل همه itemها؛ شکست یکی، transaction کل stage را rollback کند؛ shortage task/record بسازد |
| بعد از quantity/item edit | فقط Draft و قبل از reservation آزاد؛ بعد از آن change request یا compensation رسمی |
| Consumption | در `InventoryIssue` مورد تأیید BA، پس از production/QC و پیش از shipping؛ `IssueReservedAsync` با reference یکتا |
| Cancel قبل از consume | release remaining reservation و cancel workflow/order طبق version/idempotency |
| Cancel بعد از consume | Order cancellation به‌تنهایی موجودی را برنمی‌گرداند؛ return/adjustment مستقل با دلیل، actor و audit |
| Expiry | scheduler reservation را آزاد کند؛ retry idempotent و audit |
| Shortage | reservation ایجاد نشود یا full reserve بماند؛ shortage به PR پیشنهادی/تأییدشده link شود، auto-PO انجام نشود مگر policy مصوب |
| Finance | ابتدا `OrderFinancialState` approved با actor/permission؛ سپس final invoice فقط با finalized snapshot و approval |
| Production/QC | owner service خود را اجرا/validate کند؛ workflow فقط در success جلو برود |
| Packaging/Shipping/Delivery | package complete پس از QC؛ shipment lifecycle مستقل اما order/workflow referenceدار؛ return بدون جعل refund/stock movement |
| Notifications | business event بعد از commit به outbox؛ consumer dedupe و dead-letter؛ notification failure نباید transaction اصلی را rollback کند |

### قرارداد رویداد و idempotency

رویدادهای پیشنهادی (پس از تصمیم BA و قبل از implementation):

`order.submitted`, `order.workflow.started`, `inventory.reservation.requested`, `inventory.reserved`, `inventory.shortage.created`, `inventory.reservation.released`, `inventory.reservation.consumed`, `production.jobs.generated`, `quality.inspection.approved/rejected`, `package.completed`, `shipment.delivered/returned`, `finance.approved`, `invoice.issued`, `order.cancelled`.

هر event باید `CompanyId`, `OrderId`, optional `OrderItemId`, aggregate/version، correlation، actor، occurred-at UTC، source reference و idempotency key داشته باشد. Event outbox باید در همان transaction aggregate ذخیره شود. consumerها dedupe key داشته باشند و retry/dead-letter قابل مشاهده باشد؛ payload نباید secret یا credential داشته باشد.

## تصمیم‌های لازم از کسب‌وکار

| تصمیم | گزینه پیشنهادی | پیامد |
|---|---|---|
| زمان reserve | InventoryReservation workflow stage، نه Draft | جلوگیری از کسر زودهنگام؛ submit بدون تضمین stock باقی می‌ماند |
| آیا submit باید reserve را اتمیک کند؟ | **پیشنهاد:** submit فقط workflow را شروع کند و task reserve جدا باشد | order submitted می‌تواند shortage داشته باشد؛ باید status/report آن روشن شود |
| consume چه زمانی؟ | پس از production و QC approved، قبل از shipment | نیاز به issue point و عملیات idempotent |
| quantity change بعد از reserve | ممنوع در ordinary edit؛ change request/compensation | UX پیچیده‌تر ولی audit و stock امن‌تر |
| finance approval | task باید Finance state واقعی را approve کند، نه auto-approve | approval انسانی حفظ می‌شود؛ invoice بعد از آن |
| invoice creation | final invoice فقط approved finance + finalized snapshot؛ payment شرط جدا | invoice ≠ payment |
| shortage→purchasing | PR پیشنهادی با link، approval دستی | از auto-PO ناخواسته جلوگیری می‌کند |
| customer group | گروه از Customer به pricing request نهایی پاس شود | discount قابل ردیابی و snapshot می‌شود |
| tax | استفاده از `ResolveTaxAsync` واحد | tax status و specificity یکسان می‌شود |
| return بعد از consume | return-to-warehouse movement جدا + finance policy جدا | original issue/receipt دست‌نخورده می‌ماند |

## سناریوهای پذیرش قرارداد

۱. Draft بدون موجودی ساخته، چند بار ویرایش و cancel شود؛ هیچ reservation/transaction ایجاد نشود.

۲. Submit با pricing معتبر، customer group discount و tax status معتبر؛ price snapshot finalized و workflow instance یکتا ایجاد شود.

۳. دو درخواست هم‌زمان reserve برای یک SKU/warehouse؛ فقط مقدار قابل‌دسترس reserve شود، oversell رخ ندهد و retry همان idempotency نتیجه قبلی را برگرداند.

۴. reserve ناکافی؛ shortage با مقدار requested/available ثبت شود و هیچ reservation جزئی ناخواسته یا stock منفی ایجاد نشود.

۵. تغییر quantity/item قبل از reserve آزاد و بعد از reserve رد یا از change-request/compensation عبور کند.

۶. reservation release با cancel، retry، stale version و expiry؛ balance و reservation status دقیق و audit قابل ردیابی باشد.

۷. consume دوباره با همان reference؛ یک issue ledger بیشتر ایجاد نشود؛ مصرف بیش از remaining رد شود.

۸. workflow task Finance بدون Finance state approved جلو نرود؛ rejection/suspension دلیل و history داشته باشد.

۹. ProductionPlanning فقط یک بار job بسازد؛ quality reject به reproduction/design route برود؛ QC approve gate بعدی باشد.

۱۰. package بدون QC approved کامل نشود؛ shipment transitionهای واقعی و return reason/warehouse ثبت شوند.

۱۱. هر business event پس از commit outbox بسازد؛ dispatcher retry/dead-letter و notification dedupe قابل مشاهده باشد.

۱۲. سناریوی کامل مستقل در isolated PostgreSQL از Draft تا Completed، همراه با tenant/RLS، permission، concurrency و read-only Order 360 اجرا شود.

## محدودیت‌های باقی‌مانده

- DB توسعه با SELECT بررسی شد، اما داده‌ها برای production/QC/logistics/reservation خالی‌اند؛ عدم مشاهده record به معنی عدم قابلیت نیست.
- workflowهای قدیمی (`human_tasks`/`WorkflowAdministration`) و definition runtime (`workflow_runtime_tasks`/`WorkflowDefinitionRuntime`) هم‌زمان در schema/source دیده می‌شوند؛ مالکیت مسیر واقعی باید قبل از STEP 5 یکپارچه و مستند شود.
- browser flow و مجوزهای چند نقش runtime در این مرحله اجرا نشد؛ HTTP 200 و health فقط سلامت process را ثابت می‌کند.
- تصمیم‌های ستون «تصمیم لازم» تا تأیید BA قرارداد قطعی نیستند.

**وضعیت STEP 1:** قرارداد و شواهد آماده است؛ application integrations عمداً اصلاح نشده‌اند.
