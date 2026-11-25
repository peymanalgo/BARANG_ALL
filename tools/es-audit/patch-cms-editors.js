const fs = require('fs');
const base = 'D:/00_BARANG/01_CORE/src/BARANG.Backoffice/Components/';
function patch(file, pairs) {
  const p = base + file;
  let s = fs.readFileSync(p, 'utf8');
  for (const [from, to] of pairs) {
    if (!s.includes(from)) { console.error('NOT FOUND in ' + file + ': ' + from.slice(0, 100)); continue; }
    s = s.split(from).join(to);
  }
  fs.writeFileSync(p, s);
  console.log('patched ' + file);
}

// ── Settings: name es + slogan es ──
patch('Pages/B2CSiteSettings.razor', [
  [`<label><span>@L("نام سایت (عربی)", "Site name (ar)", "اسم الموقع (عربي)")</span><input @bind="NameAr" dir="rtl" /></label>`,
   `<label><span>@L("نام سایت (عربی)", "Site name (ar)", "اسم الموقع (عربي)")</span><input @bind="NameAr" dir="rtl" /></label>
            <label><span>@L("نام سایت (اسپانیایی)", "Site name (es)", "اسم الموقع (إسباني)")</span><input @bind="NameEs" dir="ltr" /></label>`],
  [`<label><span>@L("شعار (عربی)", "Slogan (ar)", "الشعار (عربي)")</span><input @bind="SloganAr" dir="rtl" /></label>`,
   `<label><span>@L("شعار (عربی)", "Slogan (ar)", "الشعار (عربي)")</span><input @bind="SloganAr" dir="rtl" /></label>
            <label><span>@L("شعار (اسپانیایی)", "Slogan (es)", "الشعار (إسباني)")</span><input @bind="SloganEs" dir="ltr" /></label>`],
  [`private string NameFa = "", NameEn = "", NameAr = "", SloganFa = "", SloganEn = "", SloganAr = "", Email = "", Phone = "";`,
   `private string NameFa = "", NameEn = "", NameAr = "", NameEs = "", SloganFa = "", SloganEn = "", SloganAr = "", SloganEs = "", Email = "", Phone = "";`],
  [`NameFa = Text(x, "siteNameFa"); NameEn = Text(x, "siteNameEn"); NameAr = Text(x, "siteNameAr");`,
   `NameFa = Text(x, "siteNameFa"); NameEn = Text(x, "siteNameEn"); NameAr = Text(x, "siteNameAr"); NameEs = Text(x, "siteNameEs");`],
  [`SloganFa = Text(x, "sloganFa"); SloganEn = Text(x, "sloganEn"); SloganAr = Text(x, "sloganAr");`,
   `SloganFa = Text(x, "sloganFa"); SloganEn = Text(x, "sloganEn"); SloganAr = Text(x, "sloganAr"); SloganEs = Text(x, "sloganEs");`],
  [`siteNameFa = NameFa, siteNameEn = NameEn, siteNameAr = NameAr,`,
   `siteNameFa = NameFa, siteNameEn = NameEn, siteNameAr = NameAr, siteNameEs = NameEs,`],
  [`sloganFa = SloganFa, sloganEn = SloganEn, sloganAr = SloganAr,`,
   `sloganFa = SloganFa, sloganEn = SloganEn, sloganAr = SloganAr, sloganEs = SloganEs,`],
]);

// ── Pages editor: es title/body/seo/keywords ──
patch('Pages/B2CSitePages.razor', [
  [`TitleAr="@Editing.TitleAr" TitleArChanged="v => Editing.TitleAr = v"`,
   `TitleAr="@Editing.TitleAr" TitleArChanged="v => Editing.TitleAr = v"
        TitleEs="@Editing.TitleEs" TitleEsChanged="v => Editing.TitleEs = v"`],
  [`BodyAr="@Editing.BodyAr" BodyArChanged="v => Editing.BodyAr = v"`,
   `BodyAr="@Editing.BodyAr" BodyArChanged="v => Editing.BodyAr = v"
        BodyEs="@Editing.BodyEs" BodyEsChanged="v => Editing.BodyEs = v"`],
  [`SeoAr="@Editing.SeoAr" SeoArChanged="v => Editing.SeoAr = v"`,
   `SeoAr="@Editing.SeoAr" SeoArChanged="v => Editing.SeoAr = v"
        SeoEs="@Editing.SeoEs" SeoEsChanged="v => Editing.SeoEs = v"`],
  [`KeywordsAr="@Editing.KeywordsAr" KeywordsArChanged="v => Editing.KeywordsAr = v"`,
   `KeywordsAr="@Editing.KeywordsAr" KeywordsArChanged="v => Editing.KeywordsAr = v"
        KeywordsEs="@Editing.KeywordsEs" KeywordsEsChanged="v => Editing.KeywordsEs = v"`],
  [`Editing.TitleFa = Text(r.Value, "titleFa"); Editing.TitleEn = Text(r.Value, "titleEn"); Editing.TitleAr = Text(r.Value, "titleAr");`,
   `Editing.TitleFa = Text(r.Value, "titleFa"); Editing.TitleEn = Text(r.Value, "titleEn"); Editing.TitleAr = Text(r.Value, "titleAr"); Editing.TitleEs = Text(r.Value, "titleEs");`],
  [`Editing.BodyFa = Text(r.Value, "bodyFa"); Editing.BodyEn = Text(r.Value, "bodyEn"); Editing.BodyAr = Text(r.Value, "bodyAr");`,
   `Editing.BodyFa = Text(r.Value, "bodyFa"); Editing.BodyEn = Text(r.Value, "bodyEn"); Editing.BodyAr = Text(r.Value, "bodyAr"); Editing.BodyEs = Text(r.Value, "bodyEs");`],
  [`Editing.SeoFa = Text(r.Value, "seoDescriptionFa"); Editing.SeoEn = Text(r.Value, "seoDescriptionEn"); Editing.SeoAr = Text(r.Value, "seoDescriptionAr");`,
   `Editing.SeoFa = Text(r.Value, "seoDescriptionFa"); Editing.SeoEn = Text(r.Value, "seoDescriptionEn"); Editing.SeoAr = Text(r.Value, "seoDescriptionAr"); Editing.SeoEs = Text(r.Value, "seoDescriptionEs");`],
  [`Editing.KeywordsFa = Text(r.Value, "seoKeywordsFa"); Editing.KeywordsEn = Text(r.Value, "seoKeywordsEn"); Editing.KeywordsAr = Text(r.Value, "seoKeywordsAr");`,
   `Editing.KeywordsFa = Text(r.Value, "seoKeywordsFa"); Editing.KeywordsEn = Text(r.Value, "seoKeywordsEn"); Editing.KeywordsAr = Text(r.Value, "seoKeywordsAr"); Editing.KeywordsEs = Text(r.Value, "seoKeywordsEs");`],
  [`titleFa = Editing.TitleFa, titleEn = Editing.TitleEn, titleAr = Editing.TitleAr,`,
   `titleFa = Editing.TitleFa, titleEn = Editing.TitleEn, titleAr = Editing.TitleAr, titleEs = Editing.TitleEs,`],
  [`bodyFa = Editing.BodyFa, bodyEn = Editing.BodyEn, bodyAr = Editing.BodyAr,`,
   `bodyFa = Editing.BodyFa, bodyEn = Editing.BodyEn, bodyAr = Editing.BodyAr, bodyEs = Editing.BodyEs,`],
  [`seoDescriptionFa = Editing.SeoFa, seoDescriptionEn = Editing.SeoEn, seoDescriptionAr = Editing.SeoAr,`,
   `seoDescriptionFa = Editing.SeoFa, seoDescriptionEn = Editing.SeoEn, seoDescriptionAr = Editing.SeoAr, seoDescriptionEs = Editing.SeoEs,`],
  [`seoKeywordsFa = Editing.KeywordsFa, seoKeywordsEn = Editing.KeywordsEn, seoKeywordsAr = Editing.KeywordsAr,`,
   `seoKeywordsFa = Editing.KeywordsFa, seoKeywordsEn = Editing.KeywordsEn, seoKeywordsAr = Editing.KeywordsAr, seoKeywordsEs = Editing.KeywordsEs,`],
  [`public Guid? Id; public string Slug = ""; public string TitleFa = "", TitleEn = "", TitleAr = "";`,
   `public Guid? Id; public string Slug = ""; public string TitleFa = "", TitleEn = "", TitleAr = "", TitleEs = "";`],
  [`public string BodyFa = "", BodyEn = "", BodyAr = "";`,
   `public string BodyFa = "", BodyEn = "", BodyAr = "", BodyEs = "";`],
  [`public string SeoFa = "", SeoEn = "", SeoAr = "", KeywordsFa = "", KeywordsEn = "", KeywordsAr = "";`,
   `public string SeoFa = "", SeoEn = "", SeoAr = "", SeoEs = "", KeywordsFa = "", KeywordsEn = "", KeywordsAr = "", KeywordsEs = "";`],
]);

// ── Blog editor: es title/excerpt/body/seo/keywords ──
patch('Pages/B2CSiteBlog.razor', [
  [`TitleAr="@Editing.TitleAr" TitleArChanged="v => Editing.TitleAr = v"`,
   `TitleAr="@Editing.TitleAr" TitleArChanged="v => Editing.TitleAr = v"
        TitleEs="@Editing.TitleEs" TitleEsChanged="v => Editing.TitleEs = v"`],
  [`BodyAr="@Editing.BodyAr" BodyArChanged="v => Editing.BodyAr = v"`,
   `BodyAr="@Editing.BodyAr" BodyArChanged="v => Editing.BodyAr = v"
        BodyEs="@Editing.BodyEs" BodyEsChanged="v => Editing.BodyEs = v"`],
  [`ExcerptEn="@Editing.ExcerptEn" ExcerptEnChanged="v => Editing.ExcerptEn = v"`,
   `ExcerptEn="@Editing.ExcerptEn" ExcerptEnChanged="v => Editing.ExcerptEn = v"
        ExcerptEs="@Editing.ExcerptEs" ExcerptEsChanged="v => Editing.ExcerptEs = v"`],
  [`SeoAr="@Editing.SeoAr" SeoArChanged="v => Editing.SeoAr = v"`,
   `SeoAr="@Editing.SeoAr" SeoArChanged="v => Editing.SeoAr = v"
        SeoEs="@Editing.SeoEs" SeoEsChanged="v => Editing.SeoEs = v"`],
  [`KeywordsAr="@Editing.KeywordsAr" KeywordsArChanged="v => Editing.KeywordsAr = v"`,
   `KeywordsAr="@Editing.KeywordsAr" KeywordsArChanged="v => Editing.KeywordsAr = v"
        KeywordsEs="@Editing.KeywordsEs" KeywordsEsChanged="v => Editing.KeywordsEs = v"`],
  [`Editing.TitleFa = Text(r.Value, "titleFa"); Editing.TitleEn = Text(r.Value, "titleEn"); Editing.TitleAr = Text(r.Value, "titleAr");`,
   `Editing.TitleFa = Text(r.Value, "titleFa"); Editing.TitleEn = Text(r.Value, "titleEn"); Editing.TitleAr = Text(r.Value, "titleAr"); Editing.TitleEs = Text(r.Value, "titleEs");`],
  [`Editing.ExcerptFa = Text(r.Value, "excerptFa"); Editing.ExcerptEn = Text(r.Value, "excerptEn");`,
   `Editing.ExcerptFa = Text(r.Value, "excerptFa"); Editing.ExcerptEn = Text(r.Value, "excerptEn"); Editing.ExcerptEs = Text(r.Value, "excerptEs");`],
  [`Editing.BodyFa = Text(r.Value, "bodyFa"); Editing.BodyEn = Text(r.Value, "bodyEn"); Editing.BodyAr = Text(r.Value, "bodyAr");`,
   `Editing.BodyFa = Text(r.Value, "bodyFa"); Editing.BodyEn = Text(r.Value, "bodyEn"); Editing.BodyAr = Text(r.Value, "bodyAr"); Editing.BodyEs = Text(r.Value, "bodyEs");`],
  [`Editing.SeoFa = Text(r.Value, "seoDescriptionFa"); Editing.SeoEn = Text(r.Value, "seoDescriptionEn"); Editing.SeoAr = Text(r.Value, "seoDescriptionAr");`,
   `Editing.SeoFa = Text(r.Value, "seoDescriptionFa"); Editing.SeoEn = Text(r.Value, "seoDescriptionEn"); Editing.SeoAr = Text(r.Value, "seoDescriptionAr"); Editing.SeoEs = Text(r.Value, "seoDescriptionEs");`],
  [`Editing.KeywordsFa = Text(r.Value, "seoKeywordsFa"); Editing.KeywordsEn = Text(r.Value, "seoKeywordsEn"); Editing.KeywordsAr = Text(r.Value, "seoKeywordsAr");`,
   `Editing.KeywordsFa = Text(r.Value, "seoKeywordsFa"); Editing.KeywordsEn = Text(r.Value, "seoKeywordsEn"); Editing.KeywordsAr = Text(r.Value, "seoKeywordsAr"); Editing.KeywordsEs = Text(r.Value, "seoKeywordsEs");`],
  [`titleFa = Editing.TitleFa, titleEn = Editing.TitleEn, titleAr = Editing.TitleAr,`,
   `titleFa = Editing.TitleFa, titleEn = Editing.TitleEn, titleAr = Editing.TitleAr, titleEs = Editing.TitleEs,`],
  [`excerptFa = Editing.ExcerptFa, excerptEn = Editing.ExcerptEn, excerptAr = Editing.ExcerptAr,`,
   `excerptFa = Editing.ExcerptFa, excerptEn = Editing.ExcerptEn, excerptAr = Editing.ExcerptAr, excerptEs = Editing.ExcerptEs,`],
  [`bodyFa = Editing.BodyFa, bodyEn = Editing.BodyEn, bodyAr = Editing.BodyAr,`,
   `bodyFa = Editing.BodyFa, bodyEn = Editing.BodyEn, bodyAr = Editing.BodyAr, bodyEs = Editing.BodyEs,`],
  [`seoDescriptionFa = Editing.SeoFa, seoDescriptionEn = Editing.SeoEn, seoDescriptionAr = Editing.SeoAr,`,
   `seoDescriptionFa = Editing.SeoFa, seoDescriptionEn = Editing.SeoEn, seoDescriptionAr = Editing.SeoAr, seoDescriptionEs = Editing.SeoEs,`],
  [`seoKeywordsFa = Editing.KeywordsFa, seoKeywordsEn = Editing.KeywordsEn, seoKeywordsAr = Editing.KeywordsAr,`,
   `seoKeywordsFa = Editing.KeywordsFa, seoKeywordsEn = Editing.KeywordsEn, seoKeywordsAr = Editing.KeywordsAr, seoKeywordsEs = Editing.KeywordsEs,`],
  [`public Guid? Id; public string Slug = ""; public string TitleFa = "", TitleEn = "", TitleAr = "";`,
   `public Guid? Id; public string Slug = ""; public string TitleFa = "", TitleEn = "", TitleAr = "", TitleEs = "";`],
  [`public string ExcerptFa = "", ExcerptEn = "", ExcerptAr = "";`,
   `public string ExcerptFa = "", ExcerptEn = "", ExcerptAr = "", ExcerptEs = "";`],
  [`public string BodyFa = "", BodyEn = "", BodyAr = "";`,
   `public string BodyFa = "", BodyEn = "", BodyAr = "", BodyEs = "";`],
  [`public string SeoFa = "", SeoEn = "", SeoAr = "", KeywordsFa = "", KeywordsEn = "", KeywordsAr = "";`,
   `public string SeoFa = "", SeoEn = "", SeoAr = "", SeoEs = "", KeywordsFa = "", KeywordsEn = "", KeywordsAr = "", KeywordsEs = "";`],
]);

// ── Footer: dialog title es input + payload ──
patch('Pages/B2CSiteFooter.razor', [
  [`@if (Dialog.Type is "menu" or "image")
                {
                    <label><span>@L("عنوان", "Title", "العنوان")</span><input @bind="Dialog.Title" /></label>
                }`,
   `@if (Dialog.Type is "menu" or "image")
                {
                    <label><span>@L("عنوان (فارسی)", "Title (fa)", "العنوان")</span><input @bind="Dialog.Title" /></label>
                }
                @if (Dialog.Type is "menu" or "image")
                {
                    <label><span>@L("عنوان (اسپانیایی)", "Title (es)", "العنوان (إسباني)")</span><input @bind="Dialog.TitleEs" dir="ltr" /></label>
                }`],
  [`Text(x, "pageTitleFa"), Text(x, "titleFa"), Text(x, "titleEn"), Text(x, "titleAr"), Text(x, "url"),`,
   `Text(x, "pageTitleFa"), Text(x, "titleFa"), Text(x, "titleEn"), Text(x, "titleAr"), Text(x, "titleEs"), Text(x, "url"),`],
  [`TitleEn = w.TitleEn ?? "", // بدون فیلد ویرایش — برای حفظ عنوان انگلیسی موجود هنگام ذخیره
            TitleAr = w.TitleAr ?? "",`,
   `TitleEn = w.TitleEn ?? "", // بدون فیلد ویرایش — برای حفظ عنوان انگلیسی موجود هنگام ذخیره
            TitleAr = w.TitleAr ?? "",
            TitleEs = w.TitleEs ?? "",`],
  [`titleAr = d.EditId is null ? d.Title : (string.IsNullOrEmpty(d.TitleAr) ? d.Title : d.TitleAr),`,
   `titleAr = d.EditId is null ? d.Title : (string.IsNullOrEmpty(d.TitleAr) ? d.Title : d.TitleAr),
            titleEs = d.EditId is null ? (string.IsNullOrEmpty(d.TitleEs) ? null : d.TitleEs) : (string.IsNullOrEmpty(d.TitleEs) ? null : d.TitleEs),`],
  [`private sealed record WidgetRow(Guid Id, int Column, string Type, Guid? PageId, string? PageTitle, string? Title, string? TitleEn, string? TitleAr, string? Url, string? ImageUrl, string? ImageKey, string? Text, string? Html, int Order, long Version);`,
   `private sealed record WidgetRow(Guid Id, int Column, string Type, Guid? PageId, string? PageTitle, string? Title, string? TitleEn, string? TitleAr, string? TitleEs, string? Url, string? ImageUrl, string? ImageKey, string? Text, string? Html, int Order, long Version);`],
  [`public string Title = ""; public string TitleEn = ""; public string TitleAr = ""; public string Url = ""; public string Text = ""; public string Html = "";`,
   `public string Title = ""; public string TitleEn = ""; public string TitleAr = ""; public string TitleEs = ""; public string Url = ""; public string Text = ""; public string Html = "";`],
]);

// ── Menu: custom item es title + culture-aware label ──
patch('Pages/B2CSiteMenu.razor', [
  [`<label><span>@L("عنوان (فارسی)", "Title (fa)", "العنوان")</span><input @bind="Draft.Title" /></label>
            <label><span>URL</span><input @bind="Draft.Url" dir="ltr" /></label>`,
   `<label><span>@L("عنوان (فارسی)", "Title (fa)", "العنوان")</span><input @bind="Draft.Title" /></label>
            <label><span>@L("عنوان (اسپانیایی)", "Title (es)", "العنوان (إسباني)")</span><input @bind="Draft.TitleEs" dir="ltr" /></label>
            <label><span>URL</span><input @bind="Draft.Url" dir="ltr" /></label>`],
  [`Text(x, "pageSlug"), Text(x, "pageTitleFa"), Text(x, "titleFa"), Text(x, "url"),`,
   `Text(x, "pageSlug"), Text(x, "pageTitleFa"), Text(x, "pageTitleEn"), Text(x, "pageTitleAr"), Text(x, "titleFa"), Text(x, "titleEn"), Text(x, "titleAr"), Text(x, "titleEs"), Text(x, "url"),`],
  [`itemType = Draft.Type, pageId, titleFa = Draft.Title, titleEn = Draft.Title, titleAr = Draft.Title, url = Draft.Url, displayOrder = Draft.Order`,
   `itemType = Draft.Type, pageId, titleFa = Draft.Title, titleEn = Draft.Title, titleAr = Draft.Title, titleEs = string.IsNullOrWhiteSpace(Draft.TitleEs) ? Draft.Title : Draft.TitleEs, url = Draft.Url, displayOrder = Draft.Order`],
  [`private sealed record MenuRow(Guid Id, string Type, Guid? PageId, string PageSlug, string PageTitleFa, string TitleFa, string Url, bool Pinned, int Order)
    {
        // آیتم صفحه‌محور عنوان خودش را ندارد؛ عنوان صفحه نمایش داده می‌شود
        public string Label => Type == "blog" ? "وبلاگ" : !string.IsNullOrWhiteSpace(TitleFa) ? TitleFa : PageTitleFa;
    }`,
   `private sealed record MenuRow(Guid Id, string Type, Guid? PageId, string PageSlug, string PageTitleFa, string PageTitleEn, string PageTitleAr, string TitleFa, string TitleEn, string TitleAr, string TitleEs, string Url, bool Pinned, int Order)
    {
        // آیتم صفحه‌محور عنوان خودش را ندارد؛ عنوان صفحه (به زبان کاربر) نمایش داده می‌شود
        public string Label
        {
            get
            {
                string culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                string Self(string fa, string en, string ar, string es) => culture == "en" ? (string.IsNullOrWhiteSpace(en) ? fa : en) : culture == "ar" ? (string.IsNullOrWhiteSpace(ar) ? fa : ar) : culture == "es" ? !string.IsNullOrWhiteSpace(es) ? es : (string.IsNullOrWhiteSpace(en) ? fa : en) : fa;
                return Type == "blog" ? Self("وبلاگ", "Blog", "المدونة", "Blog") : Self(!string.IsNullOrWhiteSpace(TitleFa) ? TitleFa : PageTitleFa, !string.IsNullOrWhiteSpace(TitleFa) ? TitleEn : PageTitleEn, !string.IsNullOrWhiteSpace(TitleFa) ? TitleAr : PageTitleAr, !string.IsNullOrWhiteSpace(TitleFa) ? TitleEs : "");
            }
        }
    }`],
  [`private sealed class DraftModel { public string Type = "page"; public string PageId = ""; public string Title = ""; public string Url = ""; public int Order = 10; }`,
   `private sealed class DraftModel { public string Type = "page"; public string PageId = ""; public string Title = ""; public string TitleEs = ""; public string Url = ""; public int Order = 10; }`],
]);
console.log('all done');
