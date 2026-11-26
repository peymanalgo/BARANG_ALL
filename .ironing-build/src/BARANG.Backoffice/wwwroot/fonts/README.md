# BARANG local webfonts

The shared application shell loads `barang-fonts.css` through the existing ASP.NET Core static-asset pipeline. Login and authenticated Backoffice use the same local font faces. No CDN, remote fallback, JavaScript font loader, or base64 font data is required at runtime.

The language-aware family tokens and all visual rules remain in `../barang-design-system.css`: Persian uses Vazirmatn, Arabic uses Noto Sans Arabic, and English uses Inter. The existing `sans-serif` system fallback remains unchanged.

## Asset selection

- Normal-style variable WOFF2 faces cover the used weight range 400-900, including 550, 650, 720, 750, and 850, without one file per weight.
- Arabic subsets cover Persian and Arabic. Latin and Latin Extended preserve Latin text, accented names, punctuation, and currency characters within each selected family.
- Unused italic, Cyrillic, Greek, Vietnamese, specialist math, and symbol-only distributions are not bundled.
- Eight subset files total 450208 bytes before HTTP compression. Browsers load only the subsets needed by the displayed text.

## Provenance and licensing

These are unmodified variable WOFF2 assets from the official Google Fonts distribution, fetched once during vendoring on 2026-09-13. The distribution versions are Inter v20, Noto Sans Arabic v33, and Vazirmatn v16. `manifest.json` records each immutable upstream asset URL, byte count, and SHA-256. Upstream URLs are provenance metadata only and are never font-loading fallbacks.

The original SIL Open Font License notices are included under `licenses/`:

- [inter license](licenses/inter-OFL.txt), [upstream notice](https://raw.githubusercontent.com/google/fonts/main/ofl/inter/OFL.txt).
- [notosansarabic license](licenses/notosansarabic-OFL.txt), [upstream notice](https://raw.githubusercontent.com/google/fonts/main/ofl/notosansarabic/OFL.txt).
- [vazirmatn license](licenses/vazirmatn-OFL.txt), [upstream notice](https://raw.githubusercontent.com/google/fonts/main/ofl/vazirmatn/OFL.txt).

Maintain the family names, Unicode ranges, variable weights, and licensing notices when updating assets. Verify local HTTP 200 responses and real-browser font loading, including with all external requests blocked, before deployment.

