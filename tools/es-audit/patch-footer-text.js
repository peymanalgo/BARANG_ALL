const fs = require('fs');
function patch(p, pairs) {
  let s = fs.readFileSync(p, 'utf8');
  for (const [from, to] of pairs) {
    if (!s.includes(from)) { console.error('NOT FOUND in ' + p + ': ' + from.slice(0, 90)); continue; }
    s = s.split(from).join(to);
  }
  fs.writeFileSync(p, s);
  console.log('patched ' + p.split('/').pop());
}
const admin = 'D:/00_BARANG/01_CORE/src/BARANG.Core.Infrastructure/SiteContent/SiteContentAdministration.cs';
patch(admin, [
  // widget create command
  ['FooterWidget.Create(Guid.NewGuid(), Company, command.ColumnIndex, command.WidgetType, command.PageId, command.TitleFa, command.TitleEn, command.TitleAr, command.TitleEs, command.Text, command.Html, command.ImageStorageKey, command.Url, command.DisplayOrder, Now)',
   'FooterWidget.Create(Guid.NewGuid(), Company, command.ColumnIndex, command.WidgetType, command.PageId, command.TitleFa, command.TitleEn, command.TitleAr, command.TitleEs, command.TextFa, command.TextEn, command.TextAr, command.TextEs, command.Html, command.ImageStorageKey, command.Url, command.DisplayOrder, Now)'],
  // widget update command
  ['w.Update(command.ColumnIndex, command.WidgetType, command.PageId, command.TitleFa, command.TitleEn, command.TitleAr, command.TitleEs, command.Text, command.Html, command.ImageStorageKey, command.Url, command.DisplayOrder, command.Version, Now)',
   'w.Update(command.ColumnIndex, command.WidgetType, command.PageId, command.TitleFa, command.TitleEn, command.TitleAr, command.TitleEs, command.TextFa, command.TextEn, command.TextAr, command.TextEs, command.Html, command.ImageStorageKey, command.Url, command.DisplayOrder, command.Version, Now)'],
  // sync placement widget update
  ['w.Update(col, w.WidgetType, w.PageId, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.Text, w.Html, w.ImageStorageKey, w.Url, w.DisplayOrder, w.Version, Now);',
   'w.Update(col, w.WidgetType, w.PageId, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.TextFa, w.TextEn, w.TextAr, w.TextEs, w.Html, w.ImageStorageKey, w.Url, w.DisplayOrder, w.Version, Now);'],
  // broken-image repair (2 variants)
  ['w.Update(w.ColumnIndex, "image", w.PageId, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.Text, w.Html, null, path, w.DisplayOrder, w.Version, Now)',
   'w.Update(w.ColumnIndex, "image", w.PageId, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.TextFa, w.TextEn, w.TextAr, w.TextEs, w.Html, null, path, w.DisplayOrder, w.Version, Now)'],
  ['w.Update(w.ColumnIndex, "image", w.PageId, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.Text, w.Html, realKey, w.Url, w.DisplayOrder, w.Version, Now)',
   'w.Update(w.ColumnIndex, "image", w.PageId, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.TextFa, w.TextEn, w.TextAr, w.TextEs, w.Html, realKey, w.Url, w.DisplayOrder, w.Version, Now)'],
  // seed loop tuple + create
  ['foreach (var (type, pageSlug, titleFa, titleEn, titleAr, titleEs, text, url, imageKey, column, order) in SiteSeedData.FooterWidgets)',
   'foreach (var (type, pageSlug, titleFa, titleEn, titleAr, titleEs, textFa, textEn, textAr, textEs, url, imageKey, column, order) in SiteSeedData.FooterWidgets)'],
  ['FooterWidget.Create(Guid.NewGuid(), Company, column, type, pageId, titleFa, titleEn, titleAr, titleEs, text, null, seedKey, seedUrl, order, Now)',
   'FooterWidget.Create(Guid.NewGuid(), Company, column, type, pageId, titleFa, titleEn, titleAr, titleEs, textFa, textEn, textAr, textEs, null, seedKey, seedUrl, order, Now)'],
  // sync placement page widget create (all nulls incl texts)
  ['db.FooterWidgets.Add(FooterWidget.Create(Guid.NewGuid(), Company, col, "page", page.Id, null, null, null, null, null, null, null, null, page.DisplayOrder, Now).Value);',
   'db.FooterWidgets.Add(FooterWidget.Create(Guid.NewGuid(), Company, col, "page", page.Id, null, null, null, null, null, null, null, null, null, null, page.DisplayOrder, Now).Value);'],
  // view
  ['private FooterWidgetView View(FooterWidget w, SitePage? page) => new(w.Id, w.ColumnIndex, w.WidgetType, w.PageId, page?.Slug, page?.TitleFa, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.Text, w.Html,',
   'private FooterWidgetView View(FooterWidget w, SitePage? page) => new(w.Id, w.ColumnIndex, w.WidgetType, w.PageId, page?.Slug, page?.TitleFa, w.TitleFa, w.TitleEn, w.TitleAr, w.TitleEs, w.TextFa, w.TextEn, w.TextAr, w.TextEs, w.Html,'],
]);
console.log('done');
