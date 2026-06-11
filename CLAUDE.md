# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Paces** is an ASP.NET Core Razor Pages admin dashboard template (v1.3.0) built with Tailwind CSS v4. It ships as a UI kit with many pre-built page variants, layout options, and integrated third-party JS plugins.

## Commands

### ASP.NET Core (backend)
```
dotnet run        # Start development server (http://localhost:5266 / https://localhost:7298)
dotnet build      # Build the project
dotnet publish    # Publish for production
```

### Frontend assets
```
npm run dev       # Start Gulp watcher (Tailwind CSS compilation + plugin copying)
npm run build     # Production build (minified CSS/JS)
```

Both `npm run dev` and `dotnet run` must be running together during development — Gulp watches and rebuilds CSS/JS while ASP.NET Core serves the app.

## Architecture

### Layout System

Pages use a two-tier layout hierarchy:

1. **Base layout** (`Pages/Shared/_BaseLayout.cshtml`): Wraps the outer HTML shell. Reads `ViewBag` properties for per-page CSS/JS customization.
2. **Layout variants**: Pages declare their layout in `@{ Layout = "..."; }`:
   - `_VerticalLayout` — sidebar + topbar
   - `_HorizontalLayout` — top-nav only
   - Variant pages under `Pages/Layouts/` showcase boxed, compact, gradient, and other configurations.

Partials in `Pages/Shared/Partials/` compose each layout:
- `_TitleMeta.cshtml` / `_HeadCss.cshtml` — `<head>` contents
- `_Topbar.cshtml` / `_Sidenav.cshtml` — navigation
- `_FooterScripts.cshtml` — JS initialization
- `_Customizer.cshtml` — floating theme-switcher panel

### Asset Pipeline

`gulpfile.js` drives the frontend build:
- **CSS**: `wwwroot/css/app.css` is the PostCSS entry point (imports Tailwind + all partial CSS files). Output goes to `wwwroot/css/app.min.css`.
- **JS**: `wwwroot/js/app.js` (main App class) + `vendor.js` (simplebar, flatpickr, choices, prismjs, preline). Output minified to `wwwroot/js/app.min.js`.
- **Plugins**: `plugins.config.js` defines which node_modules assets (ApexCharts, DataTables, FilePond, Quill, etc.) get copied to `wwwroot/plugins/`. Add new third-party libs here rather than importing them directly.

### CSS Organization

`wwwroot/css/` is split by concern:
- `config/` — CSS variables, fonts, theme tokens
- `structure/` — layout, sidenav, topbar, footer
- `custom/` — buttons, forms, tables, cards, badges, etc.
- `pages/` — page-specific overrides
- `plugins/` — styles scoped to each third-party plugin

### JavaScript App Class

`wwwroot/js/app.js` exposes a global `App` object initialized on `DOMContentLoaded`. It handles portlet card controls (close/collapse), modal/offcanvas/drawer wiring, and form validation bootstrap. Page-specific JS lives in `wwwroot/js/pages/`.

### Internationalization

Seven locale JSON files in `wwwroot/data/translations/` (en, es, de, ar, hi, it, ru). Translation loading is handled client-side.

### Key Decisions

- **No database or API layer** — this is a pure UI template; pages contain static HTML with dummy data.
- **Razor Pages only** — no MVC controllers. Each `.cshtml` file has a paired `.cshtml.cs` code-behind only where needed.
- **Tailwind v4** — uses the new CSS-first config (no `tailwind.config.js`); theme tokens are in `wwwroot/css/config/_root.css`.
