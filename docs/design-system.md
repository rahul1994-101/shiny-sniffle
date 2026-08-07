# WebApp design system

One UI–inspired, **no external UI library**. Tokens and shared components live in `src/WebApp/wwwroot/app.css`. Theme is **dark-first** with `[data-theme="light"]` overrides; user choice is persisted via `webAppTheme` in `wwwroot/js/webapp.js`.

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
| Toggle / persist | Navbar, Settings → Appearance; `getAppTheme` / `setAppTheme` |

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

Page width: `--app-page-max`, horizontal padding `--app-page-pad-x`.

---

## Layout shell

| Piece | Behavior |
|-------|----------|
| `#app-shell.app-shell-sidebar-inset` | Desktop: padded shell, inset sidebar + main stack panel |
| `.main-stack` | Navbar + `main`; `gap: var(--ui-space-2)`; panel glass on ≥641px |
| Mobile ≤640px | Sidebar drawer + backdrop; shell padding 0 |
| Chat | `Chat.razor` + `ChatMessageList`; composer tokens `--app-composer-*` |

**Standard content pages** (dashboard, settings, automations): `app-page` → `app-page-header` + `app-page-body` → `app-page-body-inner`.

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

Modifiers: `ui-btn-icon`, `ui-btn-block`, `ui-btn-lg`.

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

### Segmented control (tabs / Dark–Light)

```html
<div class="ui-segmented" role="tablist">
  <NavLink class="ui-segmented-btn" ActiveClass="ui-segmented-btn-active" ...>A</NavLink>
  <button type="button" class="ui-segmented-btn ui-segmented-btn-active">B</button>
</div>
```

Email **Accounts | Providers** uses `SettingsEmailNav.razor` inside `ui-segmented settings-section-nav`.

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

`ui-list-row-rich`, `ui-list-row-rich-body`, `ui-list-row-rich-actions`, `ui-list-row-meta`, `ui-list-row-badge`.

### Settings-only (scoped CSS in `SettingsShell.razor.css`)

`settings-card`, `settings-section-title`, `settings-section-lead`, `settings-input`, `settings-label`, etc. Keep settings form styling in the shell; use `ui-btn` for actions.

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
- [ ] Lists: `ui-group` rows  
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
| `Components/Pages/Settings/Shared/General.razor` | Appearance control |
