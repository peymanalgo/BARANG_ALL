# BARANG Backoffice UI/UX Standard

**Document status:** Governing UI/UX standard
**Applies to:** All current and future BARANG Backoffice UI
**Primary UI framework:** Blazor Interactive Server
**Supported languages:** Persian (`fa`), English (`en`), Arabic (`ar`)
**Direction policy:** `fa` and `ar` = RTL, `en` = LTR
**Date policy:** `fa` = Jalali/Shamsi; `en` and `ar` = Gregorian; canonical persisted dates remain locale-independent
**Purpose:** Define one mandatory visual and interaction standard for every BARANG Backoffice screen so the product remains visually consistent, professional, compact, enterprise-grade, and predictable as the system grows.
**BARANG shell exclusions:** No global smart assistant, no sidebar service-status/consumption panel, no `Workspace / فضای کاری` navigation group. The global language selector contains Persian, Arabic, and English.

---

# 1. Mandatory Rule for Codex

Before implementing or modifying ANY Backoffice UI, Codex MUST read and comply with this document.

This requirement applies to every:

- main menu
- submenu
- page
- entity
- use case
- grid
- card view
- tree
- organizational chart
- modal
- popup
- drawer
- form
- tab
- toolbar
- filter area
- search area
- selector
- date input
- button
- action
- status badge
- dashboard widget
- empty state
- loading state
- error state
- permission-sensitive control
- localization resource
- responsive behavior
- browser interaction

Codex MUST:

1. Reuse existing BARANG shared UI components before creating page-specific components.
2. Preserve one visual language across the complete Backoffice.
3. Never introduce a second competing UI pattern.
4. Never weaken business rules, permissions, RLS, CompanyContext, or validation for visual convenience.
5. Keep fa/en/ar localization complete.
6. Keep RTL/LTR behavior correct.
7. Use centralized date/calendar components.
8. Use searchable selectors for non-trivial selections.
9. Validate important UI interactions in a rendered browser when feasible.
10. Fix the application when required UI behavior fails; do not weaken tests merely to obtain PASS.

## 1.1 Selector and calendar interaction contract

Every custom selector, searchable dropdown, autocomplete, category tree, date picker, and calendar MUST use the shared popup lifecycle where one exists. The control MUST:

- close on pointer/touch interaction outside both its trigger/input and popup, including when the popup is rendered through a portal;
- close on `Escape`, restore focus to the trigger, and leave the containing modal open;
- close after a final option/date selection, while category branch expand/collapse keeps the tree open;
- preserve the last committed value when dismissed without a final selection;
- allow typing, scrolling, keyboard navigation, and clicks inside the popup without premature dismissal;
- close any previously open selector or calendar when another one opens;
- register and remove document listeners exactly once per component lifetime, including disposal;
- let outside pointer events continue to their intended target without global propagation suppression.

Native `<select>` controls retain native browser behavior and MUST use an empty disabled placeholder option when a placeholder is required.

The localized empty selection hint is a placeholder only, never a selectable option, editable query, DTO value, or persisted value: `انتخاب کنید` (`fa`), `Select` (`en`), and `اختر` (`ar`). Searchable selectors start with an empty query, persist only the real option ID/code, preserve edit-mode values, and clear optional values back to empty. Loading, no-results, API-error, and required-validation states remain distinct from the placeholder. Required fields MUST reject empty values with precise localized validation text. All selector behavior MUST preserve dependent filtering, keyboard accessibility, and RTL/LTR behavior.

---

# 2. Overall Design Direction

BARANG Backoffice must feel like a mature enterprise application.

The visual character must be:

- compact;
- structured;
- calm;
- professional;
- dense without becoming crowded;
- optimized for daily ERP operations;
- predictable;
- visually consistent;
- easy to scan;
- fast for repetitive work;
- restrained in color;
- strong in information hierarchy;
- desktop-first but responsive;
- fully RTL/LTR compatible.

The application must NOT feel like:

- a generic admin template;
- a prototype;
- a marketing website;
- an oversized card dashboard;
- a collection of unrelated CRUD screens;
- an application with excessive whitespace;
- a system exposing technical resource keys and internal identifiers.

---

# 3. Core Visual Identity

## 3.1 Sidebar

Use the Login-derived deep-purple enterprise sidebar.

Recommended base:

```css
--barang-sidebar-bg: #342464;
--barang-sidebar-bg-soft: #493481;
--barang-sidebar-text: #f2efff;
--barang-sidebar-muted: #bbb2dd;
```

Characteristics:

- dark background;
- light text;
- compact spacing;
- clear section grouping;
- active menu highlighted with the Login purple accent;
- selected child menu visually obvious;
- restrained icons;
- account area near bottom;
- logout near bottom;
- optional compact environment/status block.

Recommended width:

`240px – 265px`

Do not use an oversized sidebar.

## 3.2 Accent

The primary accent is the purple used by the Login page. The Login page is the authoritative color/theme reference for the entire Backoffice.

Recommended:

```css
--barang-accent: #6c4df6;
--barang-accent-hover: #5034d8;
--barang-accent-soft: #f0edff;
```

Use accent for:

- primary CTA;
- selected menu;
- selected tab underline;
- active navigation emphasis;
- highlighted controls;
- important but non-danger state.

Do not overuse the accent. Pink/coral from Login may be used only as a restrained secondary flourish; it must not replace semantic status colors.

## 3.3 Main Content Area

Recommended:

```css
--barang-page-bg: #f7f5ff;
--barang-surface: #ffffff;
--barang-surface-muted: #faf9ff;
--barang-border: #ebe8f4;
--barang-border-strong: #d9d3ec;
```

The main content area should be warm neutral, not stark white everywhere.

## 3.4 Text

```css
--barang-text: #29233d;
--barang-text-secondary: #625b75;
--barang-text-muted: #827c94;
```

## 3.5 Semantic Colors

```css
--barang-success: #22955f;
--barang-success-bg: #e8f7ef;

--barang-warning: #c98810;
--barang-warning-bg: #fff4d8;

--barang-danger: #c94747;
--barang-danger-bg: #fdecec;

--barang-info: #5269bd;
--barang-info-bg: #edf1ff;
```

Semantic colors must remain consistent across all modules.

---

# 4. Design Tokens

Use centralized CSS variables/design tokens.

Minimum token categories:

- colors;
- borders;
- radius;
- shadows;
- spacing;
- control heights;
- typography;
- z-index layers;
- status colors;
- modal sizes.

Recommended baseline:

```css
--barang-radius-sm: 6px;
--barang-radius-md: 9px;
--barang-radius-lg: 12px;

--barang-shadow-sm: 0 2px 8px rgba(23,32,51,.04);
--barang-shadow-md: 0 8px 24px rgba(23,32,51,.07);

--barang-control-height: 40px;
--barang-button-height: 38px;
```

Do not scatter arbitrary values in page-specific CSS.

---

# 5. Spacing Standard

Use a compact spacing scale:

- 4px
- 8px
- 12px
- 16px
- 20px
- 24px
- 32px

Recommended:

- page horizontal padding: 20–28px;
- section vertical gap: 20–24px;
- card padding: 14–18px;
- grid cell vertical padding: 10–13px;
- form field gap: 12–16px;
- toolbar gap: 8–12px.

Avoid excessive vertical whitespace.

---

# 6. Radius Standard

Recommended:

- input/select: 7–9px;
- button: 7–9px;
- cards: 9–12px;
- modal: 12–16px;
- badges: pill only when appropriate.

Do not use large rounded corners everywhere.

---

# 7. Typography

## 7.1 Permanent language-aware font-family rule

The active UI language owns the primary font family across the entire Backoffice:

- Persian (`fa`): **Vazirmatn**.
- Arabic (`ar`): **Noto Sans Arabic**.
- English (`en`): **Inter**.

Centralized language-aware inheritance is mandatory for every current and future page/component, including Login, shell, breadcrumbs, titles, tabs, grids, cards, forms, controls/placeholders, buttons, modals, tooltips, validation, toasts, selectors, calendars, and organizational charts.

- Define language stacks and the active family only in shared BARANG design tokens, selected by the root `html[lang]` attribute.
- Normal UI text MUST inherit that family. Do not introduce page-specific stacks, decorative serif headings, or a separate Login font rule.
- Preserve the existing size, weight, color, line-height, emphasis, control heights, grid density, modal geometry, and responsive hierarchy. This rule standardizes font family, not all typography.
- Technical codes/IDs and semantic code/pre/kbd/samp content may retain the shared or existing technical monospace stack. This is the only ordinary font-family exception.
- Self-host the approved font families under the Backoffice static assets and load one centralized `@font-face` stylesheet through the shared application shell for Login and authenticated pages. Runtime Google Fonts/CDN requests, remote font fallbacks, and base64 embedding are prohibited. Use variable WOFF2 assets for the weights actually used by the UI; retain the shared system-font fallback stacks, source checksums, and redistribution licenses. Do not duplicate font assets, add unused weight/style files, or introduce per-page font downloads.
- Interactive language changes MUST update the root language/font/direction immediately without document reload, persist the preference through the existing culture endpoint, and propagate the culture through shared components without recreating the current page or discarding its state.
- Preserve `fa/ar = RTL`, `en = LTR`, and the central date engine's canonical values and calendar policy.
- Audit every routed page and first-party font declaration. Real-browser validation must check loaded faces and computed fonts, placeholders, runtime switching, localized captions, representative modals/calendars, and responsive geometry.

## 7.2 Preserved typography hierarchy

Recommended hierarchy:

- Page title: 26–30px, bold
- Section title: 18–22px, bold
- Card title: 15–17px, semibold
- Grid/body: 12.5–14px
- Field label: 12–13px, semibold
- Helper text: 11.5–12.5px
- Technical code: monospace only when useful

Do not use oversized 36–44px titles for ordinary ERP pages.

---

# 8. Main Application Shell

## 8.1 Header

Target height:

`56–64px`

Header may contain:

- breadcrumb/current area;
- compact company context;
- compact branch context;
- the mandatory three-language switcher;
- theme control only if BARANG explicitly keeps this feature;
- user/session action if not already in sidebar.

The language switcher MUST contain exactly these supported UI languages:

- فارسی (`fa`)
- العربية (`ar`)
- English (`en`)

The order may adapt to RTL/LTR layout, but all three options must always be available.

BARANG does NOT have an AI assistant entry in the global header. Do not add an AI assistant, smart-assistant button, chat launcher, or equivalent global action unless a future approved BARANG requirement explicitly introduces that feature.

Keep header compact.

## 8.2 BARANG Global Shell Exclusions

The following elements are explicitly NOT part of the BARANG Backoffice standard and MUST NOT be added merely as visual decoration:

- no global `Smart Assistant / دستیار هوشمند` action;
- no service-health/status panel in the main sidebar;
- no service-consumption/progress panel in the main sidebar;
- no main navigation group named `Workspace / فضای کاری`;
- no unrelated infrastructure/status widgets copied into the product shell.

Operational service health belongs to technical monitoring/operations tooling, not the ordinary BARANG business navigation, unless a future approved use case explicitly requires an administrator-facing monitoring screen.

The sidebar must start from BARANG's actual business navigation groups and approved menus.

## 8.3 Sidebar Structure

Example:

```text
BARANG

خانه

داده‌های پایه
  چارت سازمانی
  اشخاص
  داده‌های مرجع
  محصولات
  SKU
  تأمین‌کنندگان
  تجهیزات

مشتریان
  مشتریان

سفارش‌ها
  سفارش‌های فروش
  سفارش جدید

موجودی و خرید
  انبارها
  موجودی
  خرید

مدیریت کاربران و دسترسی‌ها
  کاربران
  نقش‌ها
  دسترسی‌ها
```

Rules:

- parent groups bold;
- children smaller and indented;
- active group expanded;
- active child clearly highlighted;
- inactive groups compact/collapsed;
- consistent icon family;
- the primary sidebar has a reversible collapse/expand control that works without reloading the page, reflows the main content, and uses a state-dependent sidebar icon with localized tooltip and `aria-label` text.

---

# 9. Breadcrumb Standard

Breadcrumb must reflect business navigation.

Good:

`داده‌های پایه / اشخاص`

Bad:

`Admin_users / management`

Breadcrumb is visually secondary.

---

# 10. Page Header Standard

Every primary page should follow this hierarchy:

```text
Breadcrumb

Page Title                                      [Primary Action]
Short Description

(optional KPI cards)

(optional Tabs)

Search / Filters / Toolbar

Main Content
```

The primary action normally uses the Login purple accent.

Example:

`+ افزودن مشتری`

---

# 11. Button Hierarchy

## Primary

Login-purple filled.

Used for:

- Add
- Create
- Save when appropriate
- New Order

## Secondary

White/neutral with subtle border.

Used for:

- Refresh
- Cancel
- Export
- secondary navigation

## Tertiary

Text/icon-only.

## Danger

Red treatment.

Never visually compete with Primary.

## Icon Buttons

Compact square button with:

- tooltip;
- aria-label;
- hover state;
- focus state;
- consistent icon.

Non-submit action buttons MUST use:

`type="button"`.

---

# 12. Iconography

Use ONE icon family throughout the application.

Recommended style:

- outline;
- 16–18px;
- consistent stroke;
- no emojis in production UI.

Common icons:

- Eye
- Pencil
- Trash
- Plus
- Search
- Refresh
- Filter
- MoreVertical
- Building
- GitBranch
- Users
- Package
- Warehouse
- Shield
- Calendar

---

# 13. Data Grid Standard

All major list pages should use a compact enterprise DataGrid.

Requirements:

- white surface;
- subtle border;
- pale header background;
- compact row height;
- clear column alignment;
- subtle row hover;
- localized headers;
- server-side paging where data can grow;
- sorting;
- search;
- filters;
- empty state;
- loading state;
- error state;
- stable row identity;
- responsive strategy.

Recommended row height:

`48–58px`

Permanent ordinary-cell rule:

- every ordinary DataGrid value, including primary/name columns, codes, usernames, and secondary metadata, must render as clean plain text or unframed entity content;
- borders, filled boxes, pills, button-like containers, and cards must never be used merely to emphasize an ordinary cell;
- only semantic status badges, tags/chips with real business meaning, and actual action/icon buttons may retain their corresponding styled surfaces;
- apply this contract centrally through shared DataGrid/entity-cell styles, including legacy grid renderers; do not add page-by-page overrides;
- preserve sorting, row/entity click behavior, search/filter/paging, responsive layout, CRUD, fa/ar/en, and RTL/LTR; validate rendered cells and semantic exceptions in the real browser.

---

# 14. Entity Cell Pattern

Use human-readable primary information.

Example:

```text
پیمان رضایی
peyman@example.com
```

Primary:
bold.

Secondary:
small muted text.

Use typography and spacing, not a border, background, pill, or card, to distinguish primary and secondary information. Interactive entity names remain semantic links/buttons with their existing click and keyboard behavior, but must look like plain entity text. A transient accessible keyboard focus outline and text underline on hover are allowed; a persistent button surface is not. Use the shared `.entity-cell`, `.entity-link`/`.table-link`, and grid code-value styles rather than introducing local emphasis containers.

Do not show a long technical identifier as the main label.

If technical ID is useful:
show as secondary metadata.

---

# 15. Grid Actions

Creation actions have a separate, mandatory placement rule:

> Add is always a visible standalone plus-icon button with the localized label Add. It is never placed inside a three-dot menu. Row operations remain in three-dot menus.

Use `افزودن` (`fa`), `Add` (`en`), and `إضافة` (`ar`). Equivalent creation captions such as Create, New, ایجاد, جدید, and ثبت جدید use the standard Add caption. Save, Update, Submit, and other distinct workflow actions retain their own labels.

Mandatory action-menu rule:

> Every grid uses a vertical three-dot dropdown for row actions. Row actions belong to the row menu; legitimate non-creation grid-level actions may belong to a separate toolbar menu. Add/Create/New actions are always standalone visible plus-icon buttons and never belong to either dropdown. Separate row action buttons/icons are not used. Menus preserve permissions, confirmations, localization, accessibility, and outside-click dismissal.

Implementation checklist (mandatory for new and migrated grids):

- One BarangActionMenu trigger is rendered per actionable row; read-only rows with no operation do not render an empty menu.
- Menu entries are full-width, localized fa/en/ar action rows with one icon and text. Existing handlers, authorization, disabled/loading states, confirmations, and navigation are preserved.
- Add/Create/New remains a visible standalone button with the shared plus icon and localized label (افزودن, Add, إضافة); it is never a row-menu entry.
- Portal positioning must use the shared popup lifecycle so menus escape scrolling containers, close on outside pointer/Escape, restore focus, support keyboard navigation, and clean up on disposal.
- Do not implement this contract by rewriting the DOM at runtime. Razor must own the menu and action markup.

Mandatory implementation details:

- The shared vertical three-dot trigger has no permanent boxed border, native button chrome, shadow, or opaque background; only a subtle hover/pressed state and a visible keyboard focus indicator are allowed.
- The popup is anchored directly to the clicked trigger using one coordinate system, opens below it with a small gap, flips/clamps only when viewport space requires it, and recalculates during scrolling and resizing. Row and toolbar menus retain separate scopes.

Preferred visible row actions:

- View / Eye
- Edit / Pencil
- More / ⋮

Put less frequent operations inside More:

- Activate
- Deactivate
- Delete
- Archive
- History
- Duplicate
- special actions

Avoid 4–6 tiny unexplained icons in every row.

---

# 16. Status Badges

Use compact status badges.

Examples:

- فعال
- غیرفعال
- مسدود
- پیش‌نویس
- منتشرشده
- در انتظار
- تکمیل‌شده

Rules:

- compact;
- semantic color;
- localized text;
- consistent across modules.

---

# 17. Search and Filter Toolbar

Standard:

```text
[Search................................] [Status ▾] [Type ▾] [Refresh]
```

At normal desktop widths, search, selectors, filters, Refresh, and related actions MUST remain on one horizontal row whenever the available space permits. Search may flex/grow; selectors and buttons remain compact rather than becoming full-width. Use an 8–12px shared gap. Wrapping must begin only at a defined tablet/mobile breakpoint and must produce intentional grouped rows instead of unnecessary one-control-per-line stacking.

Search must:

- have clear placeholder;
- use search icon;
- remain compact;
- support server-side search when needed.

Do not use an unnecessarily huge search input.

---

# 18. Quick Filters

Use compact tabs/chips for frequent filters:

`همه | فعال | غیرفعال | مسدود`

Active filter:
Login-purple accent/underline or subtle active state.

---

# 19. KPI Cards

Use only when they add operational value.

Good examples:

- کل مشتریان
- کاربران فعال
- نقش‌ها
- سفارش‌های امروز
- در انتظار تأیید
- آماده ارسال

Visual rules:

- compact;
- white surface;
- subtle border;
- little/no shadow;
- small label;
- prominent number;
- semantic number color when useful.

Do not add KPI cards mechanically to every page.

---

# 20. Tabs

Use a thin enterprise tab pattern.

Example:

`کاربران | نقش‌ها | دسترسی‌ها`

Active tab:

- bold/darker text;
- Login-purple underline.

Avoid large pill-style tab cards unless justified.

Complex multi-tab editors with many tabs should use vertical navigation when horizontal tabs become crowded. The navigation belongs on the logical start side, mirrors for RTL/LTR, and the selected state must remain clear without discarding editor state.

Ordinary modal and tab content must avoid horizontal scrolling at supported desktop widths. Prefer a wider modal, responsive columns, wrapping/truncation, compact action columns, and vertical scrolling for long content.

### Permanent modal sizing rule

Shared BARANG modal variants must size to the actual information density of the editor and the available viewport. Ordinary Create/Edit/View forms and nested reference editors must be large enough for their fields, searchable-selector result lists, and visible footer actions at supported desktop widths; they must not introduce internal scrolling solely because a modal was undersized. Vertical scrolling is reserved for content that genuinely exceeds the usable viewport or contains a long list. Horizontal scrolling is not permitted in modal bodies. Implement this through the shared modal size/layout foundations and responsive variants, not page-specific fixed dimensions.

---

# 21. Card View

Use Card View when summary comparison is useful.

Card structure:

- title;
- secondary technical code;
- status badges;
- concise metadata;
- actions at bottom;
- subtle border;
- low radius;
- minimal shadow.

Do not create oversized marketing-style cards.

---

# 22. Grid / Card View Toggle

Allowed when both representations are meaningful.

Example:

`کارت | جدول`

Persist user preference where practical.

Do not add a toggle purely decoratively.

---

# 23. Default CRUD Pattern

For ordinary master-data entities:

`Grid/Tree → Modal Popup → Create/View/Edit`

Do not navigate to separate CRUD pages unless the workflow is genuinely complex.

---

# 24. Modal Standard

Default Create/Edit/View should use a centered modal.

Recommended widths:

- Small: 420–520px
- Medium: 620–760px
- Large: 820–980px
- Extra Large: 1100–1300px

Mobile:
near/full-screen.

Modal requirements:

- centered;
- clear title;
- optional subtitle;
- one reliable explicit close/cancel action; a separate close icon is optional only when it does not duplicate an identical Cancel action;
- sticky header/footer for long content;
- scrollable body;
- Save/Cancel always reachable;
- Escape support;
- focus trap;
- focus returns to triggering button.

Modal and form safe spacing is mandatory. The scrollable modal body must use 16–24px inline padding; form sections use 14–18px inner padding; and field grids use consistent 12–16px gaps. Inputs, selects, and textareas must never touch or visually crowd dialog edges. Sticky headers and footers remain outside the scrolling field area and must not overlap form content.

---

# 25. Modal Structure

```text
┌─────────────────────────────────────────────┐
│ Title                                  ×   │
│ Short description                          │
├─────────────────────────────────────────────┤
│ Section                                     │
│ Field                Field                  │
│ Field                Field                  │
│                                             │
│ Section                                     │
│ ...                                         │
├─────────────────────────────────────────────┤
│                       Cancel     Save        │
└─────────────────────────────────────────────┘
```

---

# 26. Create / View / Edit Reuse

Use one editor component with modes:

- Create
- View
- Edit

View:
- read-only;
- Edit button if authorized.

Edit:
- current values loaded;
- editable fields enabled;
- Save/Cancel.

Do not build three separate forms.

---

# 27. Form Density

Recommended:

- input height: 38–42px;
- compact labels;
- two columns on desktop where logical;
- one column on mobile.

Two-column desktop fields must remain balanced and label/control alignment must be consistent. Full-width fields must use the same safe inline margins as the field grid; mobile forms collapse to one column without removing modal-body or section padding.

Examples:

`نام | نام خانوادگی`

`موبایل | ایمیل`

`استان | شهر`

Full width:

- Address
- Notes
- Long Description

---

# 28. Form Sections

Group fields into meaningful sections.

Examples:

- اطلاعات عمومی
- اطلاعات حقوقی
- اطلاعات تماس
- آدرس
- اطلاعات مالی
- تنظیمات سیستم

Avoid large flat forms.

---

# 29. Progressive Disclosure

Show only relevant fields.

Examples:

- Customer Type = Individual → individual fields
- Customer Type = Legal → legal fields
- Credit enabled → credit controls
- Advanced settings → collapsed section

---

# 30. Unsaved Changes

If a user changes a form and closes/navigates away:

show localized confirmation.

Example:

`تغییرات ذخیره نشده‌اند. آیا مطمئن هستید؟`

Only show when changes actually exist.

---

# 31. Save Behavior

Save actions should:

- display loading state;
- prevent duplicate submission;
- wait for real API result;
- show success only after persistence;
- show localized business error if failed.
- reload the authoritative saved record and refresh the owning grid;
- close or exit the approved modal/editor workflow after success;
- remain open with the localized error after failure.

For repetitive workflows, consider:

- Save
- Save & Close
- Save & New

only when useful.

---

# 32. Selectors

All non-trivial selectors MUST be searchable.

Requirements:

- multi-character typing;
- debounce for remote data;
- Arrow Up/Down;
- Enter;
- Escape;
- localized no-result;
- loading state;
- error state;
- stable ID/code stored;
- translated caption displayed.

Never store translated display text as identity.

---

# 33. Dependent Selectors

Examples:

- Country → Province → City
- Company → Branch
- Branch → Unit
- Product → SKU
- Customer Group → Price List

Invalid combinations must be prevented.

---

# 34. Date / Calendar Standard

Mandatory system-wide.

## Persian (`fa`)

- Jalali/Shamsi display;
- Jalali/Shamsi input;
- real Jalali calendar picker.

Do not use Gregorian browser `<input type="date">`.

## English (`en`)

Gregorian.

## Arabic (`ar`)

Gregorian.

## Canonical Value

Persist canonical locale-independent dates.

Use:

- `DateOnly` when time is not needed;
- UTC for timestamps when applicable.

Runtime language switch must convert representation of the SAME date.

The shared date picker must support day selection, previous/next month, direct localized month selection, direct year selection, keyboard operation, and correct RTL/LTR direction. Page-specific date converters or calendars are prohibited.

Mandatory regression:

`1364/03/18 == 1985/06/08`

Never reinterpret digits.

---

# 35. Localization Standard

Every user-facing string must support exactly the BARANG UI language set:

- فارسی (`fa`)
- العربية (`ar`)
- English (`en`)

These three languages must be available in the global language selector. Do not add or remove UI languages without an explicit approved BARANG product decision.

Includes:

- sidebar;
- menu;
- breadcrumb;
- page title;
- section title;
- tabs;
- grid headers;
- buttons;
- placeholders;
- selector values;
- statuses;
- modal titles;
- validation;
- errors;
- tooltips;
- confirmation;
- empty states;
- loading states.

Never expose:

- resource keys;
- `Admin_users`;
- `Module_*`;
- raw technical localization names.

---

# 36. RTL / LTR

Persian and Arabic:
RTL.

English:
LTR.

All shared components must support direction change.

Use CSS logical properties where practical:

- margin-inline
- padding-inline
- inset-inline
- border-inline

Avoid page-specific RTL hacks.

Tree node rows, nesting guides, disclosure controls, and collapsed-arrow direction MUST mirror between RTL and LTR using logical layout properties. In `fa` and `ar`, disclosure controls appear on the visual RTL inline-start side and indentation progresses in the RTL direction; `en` uses the corresponding LTR placement.

---

# 37. Loading State

For page/grid/card:
use skeleton loading where practical.

For actions:
small inline spinner + disabled button.

Do not leave the user wondering whether a click happened.

---

# 38. Empty State

Good:

`هنوز هیچ مشتری ثبت نشده است.`
`برای شروع اولین مشتری را ایجاد کنید.`
`[+ افزودن مشتری]`

Bad:

`No data`

Empty state should explain next action.

---

# 39. Error State

Errors must be visible and actionable.

Example:

`اطلاعات دریافت نشد.`
`ارتباط با سرور برقرار نشد.`
`[تلاش مجدد]`

Never expose raw stack traces.

---

# 40. Toast / Feedback

Success:

`✓ تغییرات ذخیره شد.`

Error:
show specific business reason.

Do not use fake success before API confirmation.

---

# 41. Confirmation

Confirmation intensity must match risk.

Low risk:
simple confirm.

High risk:
strong warning.

Very destructive:
may require typing entity name/code.

Do not show confirmation for every harmless action.

---

# 42. Activity Timeline

Important entities may include a readable timeline.

Examples:

- created;
- edited;
- status changed;
- assignment changed;
- approved;
- shipped.

Do not expose raw audit logs.

---

# 43. Dashboard Standard

Dashboard must prioritize operational information.

Recommended:

- سفارش‌های امروز
- در انتظار تأیید مالی
- در حال تولید
- QC ناموفق
- آماده ارسال
- تحویل امروز
- کارهای من
- کمبود موجودی

Use compact KPI cards and operational lists.

Do not simply duplicate menu shortcuts.

---

# 44. Organizational Chart Standard

Organizational Chart is tree-based, not grid-based.

Hierarchy:

```text
Company
└── Branch
    └── Unit
        └── Person / Manager
            └── Subordinate
```

## Company Node

Show:

- Company Name
- optional Code
- Status
- Eye
- Edit
- Add Branch
- More/status actions

## Branch Node

Show:

- Branch Name
- Status
- Eye
- Edit
- Add Unit

## Unit Node

Show:

- Unit Name
- Manager if useful
- Eye
- Edit
- Add Person
- Add Child Unit if supported

## Person Node

Show:

- Full Name
- Position
- View/Edit Assignment
- Add Subordinate
- More

Visual rules:

- compact;
- white/neutral surface;
- subtle border;
- no large blue rows;
- actions may become stronger on hover;
- tree indentation clear;
- RTL indentation correct.

All Create/View/Edit operations must open standard BARANG modals.

No Company/Branch/Unit grid in the chart workflow.

---

# 45. Persons Standard

Persons is Grid-first.

Create/Edit/View:
large modal or structured editor.

Exactly four primary tabs:

1. اطلاعات کامل
2. چارت سازمانی
3. اطلاعات کاربری
4. نقش‌ها و دسترسی‌های مؤثر

Use standard thin tab bar with Login-purple active underline.

---

# 46. User & Access Management Standard

One main page.

Three tabs:

1. کاربران
2. نقش‌ها
3. دسترسی‌ها

## Users

- compact grid;
- Add User;
- Person search;
- selected Person summary;
- User Account panel.

## Roles

- compact grid;
- Add Role;
- standard role modal.

## Access

- Role selector;
- User selector;
- permission tree/matrix;
- View/Create/Edit/Delete columns;
- inherited/direct/effective permission distinction.

Optional KPI row:

- total users;
- active users;
- roles;
- granted permissions.

---

# 47. Customers Standard

Main page:
compact enterprise grid.

Optional KPIs:

- total customers;
- active;
- legal;
- blocked.

Create/Edit:
large modal.

Suggested sections/tabs:

- General
- Addresses
- Contacts
- Financial
- Bank Accounts

Individual/Legal fields must be conditionally displayed.

---

# 48. Products / SKU Standard

Default:
grid.

Optional:
card view for catalog-like browsing.

Cards:
compact;
summary-oriented;
status;
technical code secondary;
actions at bottom.

---

# 49. Orders Standard

Orders are complex and may use a full page/full-screen editor.

Recommended:

- compact order header;
- customer summary;
- status;
- totals always visible;
- tabs/sections;
- sticky actions;
- timeline.

Do not force complex order editing into a small modal.

---

# 50. Keyboard UX

Required:

- Tab navigation;
- Enter in selectors;
- Escape to close modal/dropdown;
- Arrow keys in selectors.

Recommended:

- Ctrl+S for Save;
- Ctrl+K for command palette/search.

---

# 51. Focus Management

When modal opens:
focus first logical editable field.

When modal closes:
return focus to trigger.

Validation:
focus first invalid field when appropriate.

---

# 52. Accessibility

Minimum:

- semantic buttons;
- real labels;
- aria-label for icon-only buttons;
- focus ring;
- keyboard operation;
- modal focus trap;
- sufficient contrast;
- no color-only meaning.

---

# 53. Responsive Behavior

Desktop-first.

Tablet:
reduce columns.

Mobile:

- sidebar becomes drawer;
- forms become one-column;
- large modal becomes full-screen;
- grid may become cards;
- actions remain reachable.

Avoid horizontal overflow in core workflows.

---

# 54. Shared BARANG UI Components

The Backoffice should converge on shared components equivalent to:

- `BarangPageHeader`
- `BarangBreadcrumb`
- `BarangSidebar`
- `BarangToolbar`
- `BarangButton`
- `BarangIconButton`
- `BarangDataGrid`
- `BarangCardGrid`
- `BarangModal`
- `BarangConfirmDialog`
- `BarangTabs`
- `BarangFormSection`
- `BarangField`
- `BarangSearchableSelect`
- `BarangDateInput`
- `BarangStatusBadge`
- `BarangKpiCard`
- `BarangEmptyState`
- `BarangErrorState`
- `BarangSkeleton`
- `BarangToast`
- `BarangPagination`
- `BarangOrganizationTree`

Codex must search for an existing equivalent before creating a new component.

Toolbar wrapping, modal/form spacing, tree directionality, and sidebar collapse/expand behavior MUST be solved in these shared foundations. Page-specific CSS overrides are not an acceptable primary fix for these global behaviors.

## Cross-module action and grid interaction rules

The following rules are permanent and apply to existing and future Backoffice modules:

- sortable DataGrid headers must look like ordinary column labels with a small adjacent sort indicator; they must never use a boxed or button-like visual treatment;
- ordinary DataGrid cells, including primary/name and code values, must follow the permanent unframed entity-cell rule in sections 13 and 14; only semantic badges, business tags/chips, and action/icon buttons are exceptions;
- the active ascending or descending state must be clear, while hover and keyboard focus remain subtle and accessible;
- at normal desktop widths, DataGrids must fit the available content area whenever reasonably possible through deliberate column sizing, compact metadata, wrapping, and safe truncation; horizontal overflow or a card fallback is reserved for genuinely narrow layouts;
- duplicate modal actions with identical meaning must be avoided; when Cancel is the form's explicit close action, do not also expose a redundant Close action;
- Cancel in Edit must reliably leave Edit or close the editor and discard unsaved values, using the standard unsaved-changes confirmation when dirty-state protection exists;
- a selectable entity must never fail silently: selection must complete or the UI must present a localized reason, and the same restriction must be enforced by the server;
- User Account Edit is separate from Person or other master-profile editing: account settings may be edited in User & Access Management while Person master data remains read-only there;
- all action icons must use one shared icon family, the shared `BarangIconButton` component or its centrally governed equivalent, and centralized action tokens; page-specific action-icon styling is prohibited;
- icon-only actions must have a localized tooltip/title and `aria-label` in fa, ar, and en;
- action buttons use a compact square target of approximately 34–38 px with an approximately 17–19 px glyph, one shared radius/border treatment, and consistent hover, focus, disabled, and RTL/LTR behavior;
- action meaning and order must not change between pages: View is informational blue, Edit is amber/orange, Add and Activate are success green (or Login purple for a primary creation action), Deactivate is warning orange, Delete and destructive removal are danger red, Move and Change Manager are informational blue-neutral, and More is neutral;
- Organizational Chart actions must remain readable at every nesting level, gain appropriate emphasis on node hover/focus, retain consistent ordering, and align correctly in both RTL and LTR.

---

# 55. CSS Architecture

Use:

- centralized design tokens;
- shared component CSS;
- limited page-specific CSS only for real layout differences.

Avoid:

- inline styles;
- duplicate button CSS;
- duplicate modal CSS;
- duplicate grids;
- page-specific RTL fixes;
- arbitrary `!important`;
- z-index escalation without cause.

---

# 56. Runtime Interactivity

A control is NOT accepted as working merely because:

- `@onclick` exists;
- source compiles;
- component test passes;
- API endpoint exists.

Critical UI actions must work in rendered application.

For CRUD:

- click;
- modal opens;
- form loads;
- save calls real API;
- persistence survives reload.

---

# 57. Browser E2E

Critical modules should include real browser E2E tests.

At minimum:

- login;
- navigate through menu;
- New;
- View;
- Edit;
- Save;
- Cancel;
- refresh persistence;
- language switching;
- RTL/LTR;
- date picker where applicable.

Tests must not be weakened to match broken UI.

Fix the application.

---

# 58. Forbidden Anti-Patterns

Codex MUST NOT introduce:

- a global smart-assistant/chat button when BARANG has no such feature;
- a sidebar service-health/status/consumption widget;
- a `Workspace / فضای کاری` main navigation group;
- a language selector that omits Persian, Arabic, or English;
- excessive whitespace;
- oversized page titles;
- huge rounded cards everywhere;
- raw resource keys;
- technical route names as titles;
- English labels in Persian UI;
- Persian labels in English/Arabic UI;
- untranslated selector values;
- duplicate modal systems;
- duplicate grid systems;
- non-searchable large selectors;
- page-specific date conversion;
- Gregorian-only Persian date input;
- silent catches;
- fake success messages;
- many unexplained row icons;
- old grid navigation from approved tree workflows;
- technical IDs as primary display labels;
- page-specific visual hacks;
- relaxed E2E assertions to achieve PASS.

---

# 59. Visual Acceptance Checklist

Before a UI task is complete:

## Shell

- [ ] dark compact sidebar
- [ ] Login-purple active state
- [ ] compact header
- [ ] breadcrumb correct
- [ ] company/branch context compact

## Page

- [ ] title compact
- [ ] description concise
- [ ] primary CTA uses Login purple
- [ ] no excessive whitespace

## Content

- [ ] correct grid/card/tree pattern
- [ ] enterprise density
- [ ] restrained radius
- [ ] restrained shadow
- [ ] compact status badges
- [ ] consistent row actions

## Forms

- [ ] modal used where appropriate
- [ ] form sections
- [ ] compact fields
- [ ] Create/View/Edit reuse
- [ ] Save/Cancel clear
- [ ] unsaved changes handled

## Localization

- [ ] fa
- [ ] en
- [ ] ar
- [ ] RTL/LTR
- [ ] no raw resource keys

## Inputs

- [ ] searchable selectors
- [ ] dependent selectors
- [ ] centralized dates

## UX

- [ ] loading
- [ ] empty
- [ ] error
- [ ] success
- [ ] confirmation
- [ ] keyboard/focus

## Runtime

- [ ] CRUD is actually clickable
- [ ] data persists after refresh
- [ ] critical browser E2E passes

---

# 60. Migration Strategy for Existing BARANG UI

After functional blockers are fixed, migrate existing UI in this order.

## Phase UI-1 — Design System Foundation

Build/normalize:

- design tokens;
- typography;
- buttons;
- icon system;
- inputs;
- modal;
- tabs;
- badges;
- page header;
- toolbar;
- searchable select;
- date input.

## Phase UI-2 — Application Shell

Refactor:

- sidebar;
- header;
- breadcrumb;
- company context;
- account panel.

## Phase UI-3 — Shared Data Components

Refactor:

- DataGrid;
- CardGrid;
- pagination;
- filters;
- KPI cards;
- loading;
- empty;
- error states.

## Phase UI-4 — Priority Pages

Migrate in this order:

1. Organizational Chart
2. Persons
3. User & Access Management
4. Customers
5. Products / SKU
6. Suppliers
7. Inventory & Purchasing
8. Orders
9. Production
10. Finance

## Phase UI-5 — Regression

Validate:

- fa/en/ar;
- RTL/LTR;
- dates;
- selectors;
- real browser E2E;
- responsive behavior;
- accessibility.

Do not break validated business behavior during visual migration.

---

# 61. Mandatory Codex Prompt Clause

Every future UI-related Codex prompt should include:

> Before implementing or modifying any Backoffice UI, read and comply with `04_Document/01_UI_UX/BARANG_BACKOFFICE_UI_UX_STANDARD_REVISED.md`. Treat it as the governing visual and interaction standard for BARANG. Use the Login-derived purple/deep-purple product palette, compact ERP density, restrained borders/radius/shadows, clear page hierarchy, compact grids/cards, standard modal CRUD, searchable selectors, exactly three UI languages (fa/ar/en), RTL/LTR behavior, centralized Jalali/Gregorian date handling, accessibility requirements, and strict rendered-browser acceptance. Do not add a global smart assistant, sidebar service-status/consumption panel, or `Workspace / فضای کاری` navigation group. Preserve BARANG business rules and security. Reuse shared BARANG components and do not introduce a competing pattern.

---

# 62. Definition of Done

A BARANG Backoffice feature is UI/UX complete only when:

- it follows this standard;
- it uses shared UI components;
- business workflow works;
- CRUD is real, not decorative;
- localization is complete;
- RTL/LTR is correct;
- dates follow centralized calendar rules;
- selectors are searchable where required;
- density is consistent;
- technical strings do not leak;
- loading/empty/error states exist;
- accessibility baseline passes;
- critical interactions work in rendered browser;
- persisted changes survive reload;
- security/business rules remain enforced.

---

# 63. Final Design Goal

Every BARANG screen should feel as though it belongs to the same mature enterprise product.

The consistent characteristics must be:

- dark enterprise navigation;
- Login-derived purple emphasis;
- warm neutral surfaces;
- compact page composition;
- compact forms;
- compact tables;
- restrained cards;
- subtle borders;
- limited shadows;
- precise typography;
- clear active states;
- polished status badges;
- predictable modal CRUD;
- strong RTL/LTR;
- complete three-language localization;
- keyboard-friendly operation;
- real browser-validated interactivity.

Consistency is more important than decorative novelty.

---

# 64. Permanent CRUD, Theme, Identity, and Login Rules

These rules are mandatory for all current and future Backoffice work:

- every entity must implement every applicable real CRUD action; every visible form, modal, toolbar, grid, card, and child-collection action must function or present a localized business/security reason;
- a successful Save persists the mutation, reloads authoritative data, refreshes the owning UI, and closes or exits the approved modal/editor workflow; a failed Save keeps the editor open with a localized error;
- Cancel must reliably close or exit Edit and discard unsaved values according to the dirty-state confirmation rules; duplicate modal actions with identical meaning are prohibited;
- hard delete is allowed only when referential integrity, protected history, and business dependencies remain safe; otherwise reject it server-side with a localized reason and offer or use Deactivate where applicable; never cascade-delete business history;
- all dates use the centralized locale-aware date service and shared picker: `fa` is real Jalali, while `en` and `ar` are Gregorian; persistence is canonical and locale-independent, and runtime language switching converts the same date without drift or digit reinterpretation;
- the shared date picker must provide direct month and year selectors in addition to day selection and month navigation;
- critical CRUD, calendar conversion, theme, CompanyContext, and authenticated-identity behaviors require strict real-browser E2E against the deployed application;
- the Login page's actual purple/deep-purple, light-lavender, surface, border, ink, and secondary accent palette is the authoritative color/theme reference for the entire Backoffice; internal layouts remain compact enterprise layouts and consume this palette through shared tokens rather than page-specific colors;
- the sidebar identity for a linked human user shows the authoritative linked Person full name on line one and the active CompanyContext display name on line two; it must update after company selection and must never show static development labels or internal identifiers. A safe technical fallback is allowed only for a valid non-human/system principal without a Person;
- all visible Login text inherits one common Login font-family token. Size, weight, color, line-height, and emphasis may retain their hierarchy;
- User Account Edit and Person/master-profile Edit remain separate workflows; account settings must not become a back door for changing Person master data;
- sortable grid headers remain label-like with small sort indicators, desktop grids avoid horizontal scrolling whenever reasonably possible, and all system action icons follow the shared `BarangIconButton` visual/semantic/accessibility contract with no page-specific icon styling.

# 65. Permanent Reference-Data and Form Compliance Rules

- Every configurable Reference Data catalog must have a documented, real business consumer. Decorative or unreachable CRUD catalogs are not permitted.
- Shared reference concepts must use the canonical Shared Basic Data catalog in every consuming module; module-specific catalogs are permitted only when their ownership and consumer are explicit.
- Delete, Activate, and Deactivate actions require a localized confirmation dialog with Confirm and Cancel. A blocked action must show the exact localized business/security reason and must never fail silently.
- Modal size must adapt to content density and viewport. Undersized ordinary editors must not create unnecessary internal scrolling, and horizontal modal/form scrolling is prohibited.
- Available viewport space must be used before introducing internal scrolling: ordinary modal/tab content must size naturally and must not be constrained by fixed or undersized nested containers. One intentional vertical scroll owner is allowed only when the complete content genuinely exceeds usable viewport space; dropdowns, trees, and search overlays must receive sufficient viewport-aware space and remain usable without horizontal overflow. This rule does not require blanket `overflow: hidden` or `overflow: visible`; inaccessible content and clipped actions are prohibited.
- Integrated configuration workspaces such as Shared Basic Data and Pricing use one canonical workspace contract: the page header is followed by a two-region workspace containing the shared internal navigation panel and the selected content region. The selected content uses the shared CRUD card, section header, toolbar, data-grid, action-icon, and pagination components. Business modules may change captions, columns, filters, and data sources, but must not create boxed navigation buttons, standalone search cards, or page-specific grid shells.
- Boolean filters in grid/toolbars use the compact inline `checkbox-field toolbar-boolean-filter` pattern beside their localized label. They must not inherit the full-width form-control/select geometry used by ordinary editor fields.
- Action icons must be semantic, distinct, colored, appropriately sized, accessible, and localized in tooltip/title/aria-label for `fa`, `ar`, and `en`.
- All forms and editors must support `fa`/`ar`/`en`, correct RTL/LTR direction and language font, and centralized Jalali/Gregorian date handling with canonical locale-independent persistence.
