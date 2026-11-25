const fs = require('fs');
const p = 'D:/00_BARANG/01_CORE/src/BARANG.Backoffice/Components/Pages/B2CSiteFooter.razor';
let s = fs.readFileSync(p, 'utf8');
const R = [
  // dialog: replace single text textarea with 4 language fields
  [`                @if (Dialog.Type == "text")
                {
                    <label class="span-2"><span>@L("متن", "Text", "النص")</span><textarea @bind="Dialog.Text" rows="4"></textarea></label>
                }`,
   `                @if (Dialog.Type == "text")
                {
                    <label class="span-2"><span>@L("متن (فارسی)", "Text (fa)", "النص (فا)")</span><textarea @bind="Dialog.TextFa" rows="3"></textarea></label>
                    <label class="span-2"><span dir="ltr">@L("متن (انگلیسی)", "Text (en)", "النص (إنجليزي)")</span><textarea @bind="Dialog.TextEn" rows="3" dir="ltr"></textarea></label>
                    <label class="span-2"><span>@L("متن (عربی)", "Text (ar)", "النص (عربي)")</span><textarea @bind="Dialog.TextAr" rows="3" dir="rtl"></textarea></label>
                    <label class="span-2"><span dir="ltr">@L("متن (اسپانیایی)", "Text (es)", "النص (إسباني)")</span><textarea @bind="Dialog.TextEs" rows="3" dir="ltr"></textarea></label>
                }`],
  // grid preview of text widget: pick per culture
  [`@<span class="fw-widget-text"><span>@Excerpt(w.Text)</span>`,
   `@<span class="fw-widget-text"><span>@Excerpt(TextFor(w))</span>`],
  // load: parse text fields
  [`Text(x, "pageTitleFa"), Text(x, "titleFa"), Text(x, "titleEn"), Text(x, "titleAr"), Text(x, "titleEs"), Text(x, "url"), Text(x, "imageUrl"), Text(x, "imageStorageKey"), Text(x, "text"), Text(x, "html"),`,
   `Text(x, "pageTitleFa"), Text(x, "titleFa"), Text(x, "titleEn"), Text(x, "titleAr"), Text(x, "titleEs"), Text(x, "url"), Text(x, "imageUrl"), Text(x, "imageStorageKey"), Text(x, "text"), Text(x, "textFa"), Text(x, "textEn"), Text(x, "textAr"), Text(x, "textEs"), Text(x, "html"),`],
  // edit populate
  [`            Text = w.Text ?? "",`,
   `            Text = w.Text ?? "",
            TextFa = w.TextFa ?? "",
            TextEn = w.TextEn ?? "",
            TextAr = w.TextAr ?? "",
            TextEs = w.TextEs ?? "",`],
  // save payload
  [`text = d.Text, html = d.Html, imageStorageKey = d.ImageKey, url,`,
   `textFa = d.TextFa, textEn = d.TextEn, textAr = d.TextAr, textEs = d.TextEs, html = d.Html, imageStorageKey = d.ImageKey, url,`],
  // row record
  [`private sealed record WidgetRow(Guid Id, int Column, string Type, Guid? PageId, string? PageTitle, string? Title, string? TitleEn, string? TitleAr, string? TitleEs, string? Url, string? ImageUrl, string? ImageKey, string? Text, string? Html, int Order, long Version);`,
   `private sealed record WidgetRow(Guid Id, int Column, string Type, Guid? PageId, string? PageTitle, string? Title, string? TitleEn, string? TitleAr, string? TitleEs, string? Url, string? ImageUrl, string? ImageKey, string? Text, string? TextFa, string? TextEn, string? TextAr, string? TextEs, string? Html, int Order, long Version);`],
  // dialog model
  [`public string Title = ""; public string TitleEn = ""; public string TitleAr = ""; public string TitleEs = ""; public string Url = ""; public string Text = ""; public string Html = "";`,
   `public string Title = ""; public string TitleEn = ""; public string TitleAr = ""; public string TitleEs = ""; public string Url = ""; public string Text = ""; public string TextFa = ""; public string TextEn = ""; public string TextAr = ""; public string TextEs = ""; public string Html = "";`],
  // helper for culture-aware text in grid
  [`    private string TypeText(string t) => t switch`,
   `    private static string TextFor(WidgetRow w)
    {
        string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return culture switch
        {
            "en" => w.TextEn is { Length: > 0 } ? w.TextEn : w.Text ?? "",
            "ar" => w.TextAr is { Length: > 0 } ? w.TextAr : w.Text ?? "",
            "es" => w.TextEs is { Length: > 0 } ? w.TextEs : (w.TextEn is { Length: > 0 } ? w.TextEn : w.Text ?? ""),
            _ => w.Text is { Length: > 0 } ? w.Text : (w.TextFa ?? ""),
        };
    }
    private string TypeText(string t) => t switch`],
];
let bad = 0;
for (const [a, b] of R) { if (!s.includes(a)) { console.error('MISS: ' + a.slice(0, 90)); bad++; } else s = s.split(a).join(b); }
fs.writeFileSync(p, s);
console.log('done bad=' + bad);
