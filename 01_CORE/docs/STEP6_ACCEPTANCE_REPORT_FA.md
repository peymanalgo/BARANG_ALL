# گزارش پذیرش STEP ۶ — Pricing canonical

تاریخ بررسی: ۲۰۲۶-۱۰-۱۶  
شاخه: `codex/step2-inventory-unit-contracts`

## نتیجه

وضعیت: **CONDITIONAL GO**. هستهٔ canonical pricing، migration chain و regressionهای فعلی سبز هستند؛ اما browser acceptance کاملِ احراز‌شده برای estimate/finalization/reload یک سفارش با fixture ایزوله در این اجرا تکمیل نشده است. بنابراین PASS نهایی ادعا نمی‌شود.

## provenance و baseline

- baseline پذیرفته‌شده: application `604c0f3c3d37b6bc648597609825ede6e0254158`، report `8f7b290d599fcdef07bc7bd0bb0508888e8874ca`.
- source تست‌شده و deploy‌شده: `937454328c52e7f570eab190a03d0a181ad2fb41`.
- backup توسعه پیش از migration: `D:\00_BARANG\barang_core_step6_predeploy.backup`؛ archive با `pg_restore --list` خوانده شد (TOC=1734، حدود ۵MB). این فایل خارج از repository و خارج از commit است.
- migration توسعه: `20261015211147_AddCanonicalPricingResolution` با موفقیت روی `barang_core` اعمال شد؛ DB volume و داده‌های قبلی حفظ شدند.

## رفتار پیاده‌سازی‌شده

1. Customer و CustomerGroup به resolver canonical می‌رسند. ترتیب تخفیف، بدون stacking، Customer > CustomerGroup > source/partner > general است و فقط یک قانون با بالاترین specificity اعمال می‌شود.
2. `CustomerFinancialProfile.TaxStatusCode` برای انتخاب قانون tax استفاده می‌شود؛ قانون status-specific بر DEFAULT مقدم است.
3. `Customer.PriceGroupId` به tariffهای `price.group` متصل است؛ tariff گروه‌دارِ منطبق بر customer بر tariff عمومی مقدم است. این مسیر از `customer.group` مستقل است.
4. snapshot componentها `PricingRuleId` و `PricingRuleVersion` را به‌همراه breakdown قبلی حفظ می‌کنند. محاسبه با decimal و precision موجود دامنه انجام می‌شود و total کلاینت trusted نیست.
5. نمایش breakdown موجود Order/Pricing (component، discount، tax، total و currency) حفظ شد؛ تغییر غیرضروری در wizard انجام نشد.

## تغییرات فنی

- Domain/Application: scope گروه مشتری، PriceGroupId tariff، TaxStatusCode، و rule id/version در snapshot.
- Infrastructure: resolver مشترک estimate/finalization، انتخاب tariff بر اساس price.group، eligibility شرکت/فعال/بازهٔ زمانی و precedence تخفیف/مالیات.
- Database: migration فقط ستون‌های جدید `tariffs.price_group_id`، `price_component_snapshots.pricing_rule_id` و `pricing_rule_version`، index/FKهای لازم و index resolution مالیات را اضافه می‌کند؛ ستون‌های قدیمی `discount_rules.customer_group_id` و `tax_rules.customer_tax_status_code` دوباره ایجاد نمی‌شوند.
- مستندات: `docs/PRICING.md`، برنامهٔ پیاده‌سازی و قرارداد lifecycle به‌روزرسانی شدند.

## شواهد آزمون

| بررسی | نتیجه |
|---|---:|
| Unit current source | **224/224 PASS** |
| Backoffice | **184/184 PASS** |
| Integration کامل روی PostgreSQL ایزوله `127.0.0.1:5545 / barang_step45_test` | **130/130 PASS**، 0 fail، 0 skip، حدود ۳:۲۷ |
| Pricing integration منتخب | **4/4 PASS** |
| Pricing workspace browser smoke (بدون mutation ذخیره‌شده) | **1/1 PASS** |
| Release build API | PASS، 0 warning/error |
| Release build Backoffice | PASS، 0 warning/error |
| Fresh/restored migration chain | زنجیرهٔ ایزوله در اجرای Integration اعمال شد؛ migration توسعه نیز با موفقیت اعمال شد |

Unitهای جدید به‌طور مشخص هم‌خوانی customer-group را از گروه دیگر جدا می‌کنند و وجود price-group/tax-status را در قرارداد مدل بررسی می‌کنند.

## تصمیم‌های صریح و محدودیت‌ها

- stacking تخفیف، exemptionهای خارج از `TaxStatusCode` و تبدیل ارز policy مصوب در این STEP ندارند؛ resolver عمداً رفتار جدیدی برای آن‌ها اختراع نمی‌کند.
- Finance/invoice و STEP ۷ خارج از scope است.
- browser acceptance احراز‌شده برای انتخاب customer/group، نمایش breakdown در Order Wizard، finalization و reload snapshot در یک deployment ایزوله در این اجرا موجود نیست؛ smoke فعلی فقط workspace Pricing را پوشش می‌دهد. این مورد blocker پذیرش نهایی است.
- regression browser جدید برای STEP3 تا STEP5 در این تغییر backend اجرا نشد؛ شواهد پذیرفته‌شدهٔ قبلی حفظ شده و Integration فعلی تمام گیت‌های backend را پوشش می‌دهد.

## deployment و GitHub

deployment محلی پس از build از همین SHA انجام شد: API image manifest `sha256:bda96f8099e9605ffbf8aa40ee542ff07326239da91202340370553b6249d34c` و Backoffice image manifest `sha256:6638c997487528cf8e40eaa7a3e2575ee38accff8bbcc0eade45daba99ae8d97`. لاگ هر دو سرویس همین SHA را اعلام می‌کند؛ API health و Backoffice `/login` با HTTP 200 تأیید شدند. volumeهای PostgreSQL/Redis/MinIO و key volumeها حفظ شدند.

وضعیت push/PR/merge باید با خروجی Git پس از commit نهایی ثبت شود؛ merge از روی 404 unauthenticated نتیجه‌گیری نمی‌شود.
