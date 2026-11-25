const fs = require('fs');
const base = 'D:/00_BARANG/01_CORE/src/BARANG.Backoffice/Components/';
const files = [
  'NotificationBell.razor', 'InventorySection.razor', 'QualityInspectionDetail.razor',
  'Pages/DesignBank.razor', 'Pages/LogisticsWorkspace.razor', 'Pages/CustomerWallet.razor',
  'Pages/ManagementDashboard.razor', 'Pages/CustomerFeedbacks.razor', 'Pages/ProductionWorkspace.razor',
  'Pages/QualityWorkspace.razor', 'Pages/ShiftsPage.razor', 'Pages/NotificationCenter.razor',
  'Pages/WorkflowWorkspace.razor', 'Pages/ServicesCatalog.razor', 'Pages/DiscountRulesPanel.razor',
  'Pages/TaxRulesPanel.razor', 'Pages/Cartable.razor', 'Pages/CartableTaskDetail.razor', 'Pages/Home.razor',
  'GoodsReceiptSection.razor', 'PurchaseApprovalSection.razor', 'PurchaseHistorySection.razor',
  'PurchaseRequestSection.razor', 'PurchaseOrderSection.razor', 'ReservationsShortagesSection.razor',
  'WarehouseTransferSection.razor', 'StockAdjustmentSection.razor', 'WarehouseSection.razor',
  'InventoryReturnSection.razor', 'Pages/InventoryPurchasingWorkspace.razor',
  'WarehouseResponsibilitySection.razor', 'Pages/WarehouseResponsibilityPage.razor',
  'Pages/B2CSiteMenu.razor', 'Pages/B2CSiteFooter.razor', 'Pages/B2CSiteSettings.razor',
  'Pages/B2CSitePages.razor', 'Pages/B2CSiteBlog.razor', 'B2CSite/LocalizedContentEditor.razor', 'B2CSite/HtmlEditor.razor',
];
let changed = 0;
for (const f of files) {
  const p = base + f;
  let src = fs.readFileSync(p, 'utf8');
  const orig = src;
  // 1) es arm returns plain en param -> route through glossary
  src = src.replace(/"es"\s*=>\s*en\b/g, '"es"=>EsGlossary.Tr(en)');
  // 2) es arm returns member .En / .TitleEn (data fallback) -> glossary too
  src = src.replace(/"es"\s*=>\s*([A-Za-z_][\w]*)\.En\b/g, '"es"=>EsGlossary.Tr($1.En)');
  src = src.replace(/"es"\s*=>\s*edge\.TitleEn\b/g, '"es"=>EsGlossary.Tr(edge.TitleEn)');
  // 3) helpers without an es arm: T(fa,ar,en) => fa/ar/_=>en
  src = src.replace(/"fa"\s*=>\s*fa\s*,\s*"ar"\s*=>\s*ar\s*,\s*_\s*=>\s*en\s*\}/g, '"fa"=>fa,"ar"=>ar,"es"=>EsGlossary.Tr(en),_=>en}');
  src = src.replace(/\{\s*"fa"\s*=>\s*fa,\s*"ar"\s*=>\s*ar,\s*_\s*=>\s*en\s*\}/g, '{ "fa" => fa, "ar" => ar, "es" => EsGlossary.Tr(en), _ => en }');
  if (src !== orig) { fs.writeFileSync(p, src); changed++; console.log('patched ' + f); }
  else console.log('NO-CHANGE ' + f);
}
console.log('changed=' + changed);
