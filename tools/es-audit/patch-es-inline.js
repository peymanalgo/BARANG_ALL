const fs = require('fs');
const base = 'D:/00_BARANG/01_CORE/src/BARANG.Backoffice/Components/';
function patch(file, pairs) {
  const p = base + file;
  let src = fs.readFileSync(p, 'utf8');
  for (const [from, to] of pairs) {
    if (!src.includes(from)) { console.error('NOT FOUND in ' + file + ': ' + from.slice(0, 80)); continue; }
    src = src.split(from).join(to);
  }
  fs.writeFileSync(p, src);
  console.log('patched ' + file);
}

patch('OrderStep6Panel.razor', [
  ['"en" => code == "pricing.product_price.missing" ? ProductPriceMessage("en") : "No active pricing tariff is configured for the affected order component.","es"=> code == "pricing.product_price.missing" ? ProductPriceMessage("en") : "No active pricing tariff is configured for the affected order component.",',
   '"en" => code == "pricing.product_price.missing" ? ProductPriceMessage("en") : "No active pricing tariff is configured for the affected order component.","es"=> code == "pricing.product_price.missing" ? ProductPriceMessage("es") : "No hay tarifa de precios activa configurada para el componente del pedido afectado.",'],
  ['"en" => string.IsNullOrWhiteSpace(date) ? $"No active sales price exists for {label}." : $"No active sales price exists for {label} on {date}.","es"=>string.IsNullOrWhiteSpace(date) ? $"No active sales price exists for {label}." : $"No active sales price exists for {label} on {date}.",',
   '"en" => string.IsNullOrWhiteSpace(date) ? $"No active sales price exists for {label}." : $"No active sales price exists for {label} on {date}.","es"=>string.IsNullOrWhiteSpace(date) ? $"No existe un precio de venta activo para {label}." : $"No existe un precio de venta activo para {label} en la fecha {date}.",'],
  ['"en" => "More than one active tariff applies to an order component; pricing configuration must be corrected.","es"=> "More than one active tariff applies to an order component; pricing configuration must be corrected.",',
   '"en" => "More than one active tariff applies to an order component; pricing configuration must be corrected.","es"=> "Más de una tarifa activa se aplica a un componente del pedido; debe corregirse la configuración de precios.",'],
]);

patch('OrderStep7Panel.razor', [
  ['"en"=>"Quantity must be greater than zero.","es"=>"Quantity must be greater than zero.",',
   '"en"=>"Quantity must be greater than zero.","es"=>"La cantidad debe ser mayor que cero.",'],
  ['"en"=>$"No active sales price exists for {label}.","es"=>$"No active sales price exists for {label}.",',
   '"en"=>$"No active sales price exists for {label}.","es"=>$"No existe un precio de venta activo para {label}.",'],
  ['"en"=>code=="pricing.currency_mismatch"?"Order components use different currencies.":"No active pricing configuration was found for the affected component.","es"=>code=="pricing.currency_mismatch"?"Order components use different currencies.":"No active pricing configuration was found for the affected component.",',
   '"en"=>code=="pricing.currency_mismatch"?"Order components use different currencies.":"No active pricing configuration was found for the affected component.","es"=>code=="pricing.currency_mismatch"?"Los componentes del pedido usan monedas distintas.":"No se encontró configuración de precios activa para el componente afectado.",'],
]);

patch('Pages/DesignBank.razor', [
  ['"ar"=>"سجل","en"=>"records","es"=>"records",_=>"رکورد"', '"ar"=>"سجل","en"=>"records","es"=>"registros",_=>"رکورد"'],
  ['"es"=>key switch {"subcategory"=>"Design subgroup","type"=>"Design type","style"=>"Design style","theme"=>"Design subject","audience"=>"Design audience","license"=>"License type",_=>"Usage type"},',
   '"es"=>key switch {"subcategory"=>"Subgrupo de diseño","type"=>"Tipo de diseño","style"=>"Estilo de diseño","theme"=>"Tema de diseño","audience"=>"Audiencia de diseño","license"=>"Tipo de licencia",_=>"Tipo de uso"},'],
  ['"ar"=>"وسوم التصميم","en"=>"Design tags","es"=>"Design tags",_=>"برچسب‌های طرح"', '"ar"=>"وسوم التصميم","en"=>"Design tags","es"=>"Etiquetas de diseño",_=>"برچسب‌های طرح"'],
  ['"en"=>"The selected file is attached automatically when you save.","es"=>"The selected file is attached automatically when you save.",',
   '"en"=>"The selected file is attached automatically when you save.","es"=>"El archivo seleccionado se adjunta automáticamente al guardar.",'],
  ['"en"=>$"{score}% match","es"=>$"{score}% match",', '"en"=>$"{score}% match","es"=>$"{score}% de coincidencia",'],
  ['"en"=>"Select","es"=>"Select", "ar"=>"اختر", _=>"انتخاب کنید"', '"en"=>"Select","es"=>"Seleccionar", "ar"=>"اختر", _=>"انتخاب کنید"'],
  ['"en"=>"Unsaved changes","es"=>"Unsaved changes",', '"en"=>"Unsaved changes","es"=>"Cambios sin guardar",'],
  ['"en"=>"Your changes have not been saved. Do you want to discard them?","es"=>"Your changes have not been saved. Do you want to discard them?",',
   '"en"=>"Your changes have not been saved. Do you want to discard them?","es"=>"Sus cambios no se han guardado. ¿Desea descartarlos?",'],
  ['"en"=>"Keep editing","es"=>"Keep editing",', '"en"=>"Keep editing","es"=>"Seguir editando",'],
  ['"en"=>"Discard changes","es"=>"Discard changes",', '"en"=>"Discard changes","es"=>"Descartar cambios",'],
  ['"es"=>field switch { "code"=>"Enter a design code.", "title"=>"Enter a design title.", _=>"Select a design category." },',
   '"es"=>field switch { "code"=>"Introduzca un código de diseño.", "title"=>"Introduzca un título de diseño.", _=>"Seleccione una categoría de diseño." },'],
  ['"en"=>"A design with this code already exists. Enter a different code.","es"=>"A design with this code already exists. Enter a different code.",',
   '"en"=>"A design with this code already exists. Enter a different code.","es"=>"Ya existe un diseño con este código. Introduzca otro código.",'],
  ['"en"=>"This record was changed by someone else. Close the dialog, reopen it and try again.","es"=>"This record was changed by someone else. Close the dialog, reopen it and try again.",',
   '"en"=>"This record was changed by someone else. Close the dialog, reopen it and try again.","es"=>"Este registro fue modificado por otra persona. Cierre el diálogo, reábrela e inténtelo de nuevo.",'],
]);

patch('Pages/ProductManager.razor', [
  ['"es"=>key switch{"brand"=>"Brand","material"=>"Material / Fabric","gender"=>"Gender group","fit"=>"Fit","style"=>"Style",_=>key}',
   '"es"=>key switch{"brand"=>"Marca","material"=>"Material / Tela","gender"=>"Grupo de género","fit"=>"Corte","style"=>"Estilo",_=>key}'],
  ['"en"=>"Product image","es"=>"Product image",', '"en"=>"Product image","es"=>"Imagen del producto",'],
  ['"en"=>"No image","es"=>"No image",', '"en"=>"No image","es"=>"Sin imagen",'],
  ['"en"=>"Upload image","es"=>"Upload image",', '"en"=>"Upload image","es"=>"Subir imagen",'],
]);

patch('Pages/WorkflowWorkspace.razor', [
  ['"en" => x.TitleEn,"es"=>x.TitleEn, "ar" => x.TitleAr, _ => x.TitleFa', '"en" => x.TitleEn,"es"=>EsGlossary.Tr(x.TitleEn), "ar" => x.TitleAr, _ => x.TitleFa'],
]);

patch('Pages/DesignCategoryTree.razor', [
  ['"es"=>action switch { "view"=>"View", "edit"=>"Edit", "add-child"=>"Add subcategory", "activate"=>"Activate", "deactivate"=>"Deactivate", "delete"=>"Delete", _=>action },',
   '"es"=>action switch { "view"=>"Ver", "edit"=>"Editar", "add-child"=>"Añadir subcategoría", "activate"=>"Activar", "deactivate"=>"Desactivar", "delete"=>"Eliminar", _=>action },'],
  ['"ar"=>Ar,"en"=>En,"es"=>En,_=>Fa', '"ar"=>Ar,"en"=>En,"es"=>EsGlossary.Tr(En),_=>Fa'],
]);

patch('Pages/PricingWorkspace.razor', [
  ['private string Text(string fa,string ar,string en)=>CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch {"en"=>en,"es"=>en,"ar"=>ar,_=>fa};',
   'private string Text(string fa,string ar,string en)=>CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch {"en"=>en,"es"=>EsGlossary.Tr(en),"ar"=>ar,_=>fa};'],
]);

// DesignCategoryPicker: real Spanish for clear-category label
patch('DesignCategoryPicker.razor', [
  ['"ar" => "مسح الفئة", "en" => "Clear category","es"=> "Clear category",', '"ar" => "مسح الفئة", "en" => "Clear category","es"=> "Quitar categoría",'],
]);
console.log('done');
