const fs = require('fs');
const base = 'D:/00_BARANG/02_WebAPP_B2C/BARANG.B2C.Web/';
function patch(file, pairs) {
  const p = base + file;
  let s = fs.readFileSync(p, 'utf8');
  for (const [from, to] of pairs) {
    if (!s.includes(from)) { console.error('NOT FOUND in ' + file + ': ' + from.slice(0, 90)); continue; }
    s = s.split(from).join(to);
  }
  fs.writeFileSync(p, s);
  console.log('patched ' + file);
}
patch('Pages/Account/Login.cshtml', [
  ['@T($"کد ورود به {Model.MaskedTarget} ارسال شد.", $"A sign-in code was sent to {Model.MaskedTarget}.", $"تم إرسال رمز الدخول إلى {Model.MaskedTarget}.")',
   '@T($"کد ورود به {Model.MaskedTarget} ارسال شد.", $"A sign-in code was sent to {Model.MaskedTarget}.", $"تم إرسال رمز الدخول إلى {Model.MaskedTarget}.", $"Se envió un código de acceso a {Model.MaskedTarget}.")'],
]);
patch('Pages/Account/Register.cshtml', [
  ['@T($"کد تأیید به {Model.MaskedTarget} ارسال شد.", $"A verification code was sent to {Model.MaskedTarget}.", $"تم إرسال رمز التحقق إلى {Model.MaskedTarget}.")',
   '@T($"کد تأیید به {Model.MaskedTarget} ارسال شد.", $"A verification code was sent to {Model.MaskedTarget}.", $"تم إرسال رمز التحقق إلى {Model.MaskedTarget}.", $"Se envió un código de verificación a {Model.MaskedTarget}.")'],
]);
patch('Pages/Account/Order.cshtml', [
  ['@T($"پرداخت {wp.Remaining:N0} تومانی از کیف پول", $"Pay {wp.Remaining:N0} Toman from wallet", $"دفع {wp.Remaining:N0} تومان من المحفظة")',
   '@T($"پرداخت {wp.Remaining:N0} تومانی از کیف پول", $"Pay {wp.Remaining:N0} Toman from wallet", $"دفع {wp.Remaining:N0} تومان من المحفظة", $"Pagar {wp.Remaining:N0} tomans desde el monedero")'],
  ['@T($"شارژ {(wp.Remaining - wp.WalletBalance).ToString("N0")} تومان", $"Top up {(wp.Remaining - wp.WalletBalance).ToString("N0")} Toman", $"شحن {(wp.Remaining - wp.WalletBalance).ToString("N0")} تومان")',
   '@T($"شارژ {(wp.Remaining - wp.WalletBalance).ToString("N0")} تومان", $"Top up {(wp.Remaining - wp.WalletBalance).ToString("N0")} Toman", $"شحن {(wp.Remaining - wp.WalletBalance).ToString("N0")} تومان", $"Recargar {(wp.Remaining - wp.WalletBalance).ToString("N0")} tomans")'],
]);
patch('Pages/Catalog.cshtml', [
  ['@T($"{Model.ConceptResults.Count} نتیجه مفهومی", $"{Model.ConceptResults.Count} concept matches", $"{Model.ConceptResults.Count} نتيجة مفاهيمية")',
   '@T($"{Model.ConceptResults.Count} نتیجه مفهومی", $"{Model.ConceptResults.Count} concept matches", $"{Model.ConceptResults.Count} نتيجة مفاهيمية", $"{Model.ConceptResults.Count} coincidencias conceptuales")'],
  ['@T($"{Model.Total} طرح آماده چاپ", $"{Model.Total} print-ready designs", $"{Model.Total} تصميم جاهز للطباعة")',
   '@T($"{Model.Total} طرح آماده چاپ", $"{Model.Total} print-ready designs", $"{Model.Total} تصميم جاهز للطباعة", $"{Model.Total} diseños listos para imprimir")'],
]);
console.log('done');
