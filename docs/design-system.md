# WebApp design system

One UI–inspired, **no external UI library**. Tokens and shared components live in `src/WebApp/wwwroot/app.css`. Theme is **dark-first** with `[data-theme="light"]` overrides; user choice is persisted **per device** via `webAppTheme` in `wwwroot/js/webapp.js` (`localStorage` key `app-theme`).

**Deferred:** Server-side theme persistence (cross-device sync) is intentionally out of scope — cosmetic, instant client toggle, and first paint depends on inline script in `App.razor`. Revisit only if product requires the same theme after sign-in on every device.

**Cursor rule (short):** [`.cursor/rules/design-system.mdc`](../.cursor/rules/design-system.mdc)

---

## Principles

1. **Tokens first** — color, spacing, radius, motion, elevation, and glass come from CSS variables. Do not hardcode `#hex` / `rgba` in component CSS unless mapping a one-off local layout concern.
2. **Reuse primitives** — prefer `ui-*` and `app-page-*` classes over new button/card patterns.
3. **Glass + gradient** — translucent panels use `backdrop-filter` and `--app-main-gradient` on shell/content so blur has depth. Pair with `--app-glass-scrim` / `color-mix` for readable text.
4. **Elevation hierarchy** — borders define shape; shadows define stack order. Three levels only (see below).
5. **Contrast** — `--app-fg` for primary text, `--app-muted` for supporting copy, `--app-fg-subtle` for tertiary (chevrons, breadcrumbs, row descriptions). Links use `--app-link`.
6. **Motion** — use `--ui-duration*` and `--ui-ease*`. Respect `prefers-reduced-motion` (animations off; transitions stay usable).

---

## Theme

| Mechanism | Location |
|-----------|----------|
| Default dark tokens | `:root`, `[data-theme="dark"]` in `app.css` |
| Light overrides | `[data-theme="light"]` in `app.css` |
| Toggle / persist (device-local) | Navbar, Settings → Appearance; `getAppTheme` / `setAppTheme` in `webapp.js` |
| Blazor sync | `app-theme-changed` DOM event; `subscribeAppThemeChanged` / `unsubscribeAppThemeChanged` (see `Appearance.razor`) |

When any code calls `webAppTheme.set()` (navbar toggle or Settings → Appearance), the document dispatches **`app-theme-changed`** with `{ theme: "dark" | "light" }`. Components that mirror theme UI should subscribe on first render and unsubscribe on dispose.

When adding a new semantic color, define it in **both** dark and light blocks.

---

## Tokens (reference)

### Motion & layout (`:root`, theme-agnostic)

| Token | Use |
|-------|-----|
| `--ui-duration-fast` / `--ui-duration` / `--ui-duration-slow` / `--ui-duration-enter` | Transitions vs enter animations |
| `--ui-ease`, `--ui-ease-out`, `--ui-ease-emphasized` | Timing curves |
| `--ui-space-1` … `--ui-space-6` | Padding, gaps, margins |
| `--ui-radius-xs` … `--ui-radius-xl` | Corners |
| `--ui-type-page` / `-section` / `-body` / `-caption` | Typography scale |

### Glass

| Token / class | Use |
|---------------|-----|
| `--app-glass-blur-chrome` | Navbar, page header, sidebar |
| `--app-glass-blur-panel` | Cards, `ui-group`, main stack panel, composer |
| `--app-glass-blur-float` | Dropdowns, menus |
| `--app-dialog-backdrop-blur` | Native `<dialog>` backdrop (theme-agnostic, `:root`) |
| `--app-glass-saturate`, `--app-glass-saturate-soft` | Backdrop saturation |
| `--app-glass-highlight` | Inset top edge on glass surfaces |
| `--app-glass-scrim` | Mixed into `background` for legibility |
| `.app-glass-chrome` / `.app-glass-panel` / `.app-glass-float` | Utility classes (blur + saturate) |

Do **not** use `::deep` in global `app.css` (Blazor scoped-only).

### Elevation

| Token | Use |
|-------|-----|
| `--app-elev-1` | Resting chrome: groups, headers, navbar, cards |
| `--app-elev-2` | Floats: dropdowns, focused composer |
| `--app-elev-3` | Modals, login/status cards, mobile sidebar drawer |
| `--app-shadow-inset-sm` | Recessed tracks (`ui-segmented`) |

Shadow **containers**, not every list row.

### Surfaces & text (theme-specific)

| Token | Use |
|-------|-----|
| `--app-main-bg`, `--app-main-gradient` | Page backdrop |
| `--app-surface`, `--app-surface-elevated`, `--app-surface-muted`, `--app-surface-subtle` | Panels and tracks |
| `--app-fg`, `--app-muted`, `--app-fg-subtle` | Text hierarchy |
| `--app-border`, `--app-border-strong` | Dividers and outlines |
| `--app-accent`, `--app-accent-hover`, `--app-on-accent` | Primary actions |
| `--app-focus-ring`, `--app-focus-ring-shadow` | Focus (always 2px ring) |

Page width: **`--app-page-max`** (fluid `clamp(56rem, 42vw + 14rem, 80rem)` capped at 100%), horizontal padding **`--app-page-pad-x`**. Header and body use the same max so chrome aligns with content.

Reference breakpoints (document only; prefer tokens over ad-hoc px):

| Token / name | Value | Use |
|--------------|-------|-----|
| Shell (desktop) | **641px** (`--app-bp-shell`) | Drawer vs inset sidebar + glass main stack |
| sm | **576px** (`--app-bp-sm`) | Large phone tweaks (e.g. login card padding) |
| lg | **992px** (`--app-bp-lg`) | Settings split editor two-column + resize gutter |

Component-level `@media` should be rare; use local flex/grid or container queries when a panel—not the viewport—should drive layout.

---

## Layout shell

| Piece | Behavior |
|-------|----------|
| `#app-shell.app-shell-sidebar-inset` | Desktop: padded shell, inset sidebar + main stack panel |
| `.main-stack` | Navbar + `main`; `gap: var(--ui-space-2)`; panel glass on ≥641px |
| Mobile ≤640px | Sidebar drawer + backdrop; shell padding 0 |
| Chat | `Chat.razor` + `ChatMessageList`; composer tokens `--app-composer-*` (may use a wider local max) |

**Standard content pages** (dashboard, settings, workflows): `app-page` → `app-page-header` + `app-page-body` → `app-page-body-inner`.

**Settings**: `SettingsShell.razor` wraps title, breadcrumbs, optional `Nav` fragment, and body.

---

## Components (markup)

Use these before inventing new patterns.

### Buttons

```html
<button type="button" class="ui-btn ui-btn-primary">Save</button>
<button type="button" class="ui-btn ui-btn-secondary">Cancel</button>
<button type="button" class="ui-btn ui-btn-danger">Delete</button>
<a class="ui-btn ui-btn-primary" href="...">Link as button</a>
```

Modifiers: `ui-btn-icon`, `ui-btn-block`, `ui-btn-lg`, `ui-btn-sm` (compact row actions in rich lists).

### Grouped lists (One UI rows)

```html
<nav class="ui-group" aria-label="...">
  <NavLink class="ui-group-row" href="...">
    <span>
      <span class="ui-group-row-title">Title</span>
      <span class="ui-group-row-desc">Description</span>
    </span>
    <span class="ui-group-row-chevron" aria-hidden="true">›</span>
  </NavLink>
</nav>
```

Static row: add `ui-group-row-static`. Danger row: `ui-group-row-danger`.

#### List row interaction (hover & active)

| Row type | Classes | Hover | Active / current |
|----------|---------|--------|------------------|
| **Navigation** | `ui-group-row` + `NavLink` (+ chevron) | `--app-overlay-hover` + whole row slides 2px (motion on) | `.active` → `--app-overlay-active` (no slide) |
| **Rich** (metadata + buttons) | `ui-group-row` + `ui-group-row-static` + `ui-list-row-rich` | Row background hover; **only** `.ui-list-row-rich-body` slides | `settings-editor-row-active` when editing (dialog/split) |
| **Static** (placeholders, non-nav) | `ui-group-row-static` only | No background slide | — |

Sign-out and other **button** rows styled as `ui-group-row-static` do not slide. Respect `prefers-reduced-motion` for transforms.

### Segmented control (tabs / Dark–Light)

```html
<div class="ui-segmented" role="tablist">
  <NavLink class="ui-segmented-btn" ActiveClass="ui-segmented-btn-active" ...>A</NavLink>
  <button type="button" class="ui-segmented-btn ui-segmented-btn-active">B</button>
</div>
```

Settings → **Email** is **Providers** only (no sub-nav). Connected mailboxes are under **Workspace → Email accounts**; the account editor links to Providers when needed.

### Status, empty state

- `ui-status-pill` (+ `-success`, `-warning`, `-error`, `-fading`)
- `ui-empty-state`, `ui-empty-state-title`, `ui-empty-state-lead`

### Dropdowns

```html
<div class="ui-dropdown-menu" role="listbox">
  <button type="button" class="ui-dropdown-option ui-dropdown-option-active">...</button>
</div>
```

Used by provider picker and `ChatAgentSelector`.

### Rich rows inside groups

Use for provider/account catalog rows (actions on the right, no chevron):

```html
<div class="ui-group-row ui-group-row-static ui-list-row-rich">
  <div class="ui-list-row-rich-body">...</div>
  <div class="ui-list-row-rich-actions">...</div>
</div>
```

Classes: `ui-list-row-rich-body`, `ui-list-row-rich-actions`, `ui-list-row-meta`, `ui-list-row-badge`. See **List row interaction** above for motion rules.

### Settings-only (scoped CSS in `SettingsShell.razor.css`)

`settings-card`, `settings-section-title`, `settings-section-lead`, `settings-input`, `settings-label`, etc. Keep settings form styling in the shell; use `ui-btn` for actions.

**Settings list editors:** `SettingsEditorHost.razor` composes list + editor (full page, dialog, split). The sticky **layout picker** is preview UX (`ShowLayoutPicker` default true, per-page `LayoutPreferenceKey` in `sessionStorage`); remove when asked, not a standing cleanup item.

Section list header: `ListTitle`, `ListLead` or `ListLeadContent`, and **`ListHeaderActions`** (toolbar on the right — Add, later search/filter). Implemented via **`SettingsSectionHead.razor`** inside `SettingsEditorHost`; primary add actions use **`SettingsHeadActionButton.razor`** (+ icon + label). At `max-width: 640px`, section head stacks **title → lead → actions**; Add uses full width (`settings-head-action`).

**Settings editor form** (`settings-editor-form` — `EmailProviders.razor`, `EmailAccounts.razor`; layout in `SettingsEditorHost.razor.css`):

**Provider**

| Block | Classes | Notes |
|--------|---------|--------|
| Identity | `settings-field` | Name, slug + hint |
| Mail | `settings-mail-server` | Top border; each protocol is `settings-endpoint-row` |
| Endpoint row | `settings-endpoint-port-ssl` | **Mobile:** host 100%; port **80%** + SSL **20%** (`4fr 1fr`). **≥641px:** **host 60% \| port 30% \| SSL 10%** (`6fr 3fr 1fr`). SSL: `settings-select` **SSL / Plain** |
| Extras | `settings-provider-extras` | Help URL first; **mobile** `4fr 1fr`; **desktop** `9fr 1fr` |

**Account**

| Block | Classes | Notes |
|--------|---------|--------|
| Provider | `settings-field` | Full width (top) |
| Sign-in + credentials | `settings-mail-server` | Divider; **email \| username** (`settings-endpoint-row-duo`: `1fr 1fr` at all breakpoints); then **password \| default** (`settings-extras-row` **`settings-password-default-row`**: `4fr 1fr` mobile, `9fr 1fr` ≥641px) |
| Agent reference | `settings-provider-extras` | Divider; alias + context (same pattern as workspace contacts) |

**Dialog footer actions** (`settings-editor-dialog-footer`): Save + Cancel stay **side-by-side** (`1fr` + `auto`) at all widths used by the editor dialog; stack full-width only at `max-width: 360px`. Dialog header uses `ui-dialog-header` only (no `settings-editor-header` margin).

**Optional fields:** When user input is optional (nullable DB column or blank-allowed with auto-generate), append `<span class="settings-label-optional">@FormFieldCopy.OptionalMarker</span>` on the **field label** — not in hints. Use `FormFieldCopy.OptionalMarker` (`"(optional)"`) everywhere; do not hardcode the string. Hints explain behavior (auto-generate, format); never repeat “optional” in hint copy.

**Shared ER editor:** Tags and Buckets use `WorkspaceErEditorFields.razor` (name, color, alias, context). Contacts and Email accounts inline the same Agent reference block with the same optional pattern.

**Color picker:** `ColorPicker.razor` — presets row + hex/custom/clear row; styles in `app.css` under `.ui-color-picker-*`.

---

## Where to put CSS

| Change | Location |
|--------|----------|
| New token or shared component | `wwwroot/app.css` |
| Page-specific layout | `{Page}.razor.css` |
| Settings form/card layout | `SettingsShell.razor.css` (`::deep` for child content) |
| Shell / sidebar / navbar | `MainLayout.razor.css`, `Sidebar.razor.css`, `Navbar.razor.css` |

Promote to `app.css` when a pattern appears twice across areas.

Bootstrap is loaded for reboot/forms only; **do not** use Bootstrap button/card classes for product UI.

---

## Checklist for new UI

- [ ] Uses `app-page` or existing shell (chat/settings)  
- [ ] Colors/spacing from tokens  
- [ ] Primary actions: `ui-btn-primary`  
- [ ] Lists: `ui-group` rows — correct **nav / rich / static** hover (see design-system.md)  
- [ ] Editor lists: `settings-editor-row-active` when row is being edited (dialog/split)  
- [ ] Floating panels: `ui-dropdown-menu` + elev-2  
- [ ] Focus: `--app-focus-ring-shadow`  
- [ ] Glass: tiered blur tokens, not one-off `blur(12px)`  
- [ ] Light + dark verified  
- [ ] Motion: `prefers-reduced-motion` considered  

---

## Related files

| File | Role |
|------|------|
| `wwwroot/app.css` | Tokens + all `ui-*` / `app-page-*` |
| `Components/Layout/MainLayout.razor` | Inset shell |
| `Components/Shared/Navbar.razor` | Theme toggle |
| `Components/Pages/Settings/Shared/SettingsShell.razor` | Settings chrome |
| `Components/Pages/Settings/Shared/Appearance.razor` | Theme control |
| `Components/Pages/Settings/Shared/General.razor` | Profile and password |
| `Components/Pages/Settings/Shared/SettingsEditorHost.razor` | List + editor layouts + layout preview footer |
| `wwwroot/js/webapp.js` | Theme + settings editor JS interop |
