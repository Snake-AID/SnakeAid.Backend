# Docsify Documentation Site

This folder contains the SnakeAid Backend documentation powered by [Docsify](https://docsify.js.org/).

## Quick Start

### Option 1: Using Python (Recommended)

```bash
# Navigate to docs folder
cd docs

# Start a simple HTTP server
python -m http.server 3000

# Open browser to http://localhost:3000
```

### Option 2: Using Node.js

```bash
# Install docsify-cli globally
npm i docsify-cli -g

# Navigate to docs folder
cd docs

# Serve the documentation
docsify serve

# Open browser to http://localhost:3000
```

### Option 3: Using Live Server (VS Code)

1. Install "Live Server" extension in VS Code
2. Right-click on `docs/index.html`
3. Select "Open with Live Server"

## File Structure

```
docs/
├── index.html              # Docsify configuration
├── _sidebar.md             # Sidebar navigation
├── HOME.md                 # Home page content
├── README.md               # Documentation organization guide
│
├── ASP Identity/           # ASP.NET Identity documentation
│   ├── aspnet-identity.introduction.md
│   ├── aspnet-identity.plan.md
│   ├── aspnet-identity.prompt.md
│   ├── aspnet-identity.sourcecode.md
│   └── aspnet-identity.usageguilde.md
│
└── NuGet/                  # NuGet documentation
    └── NuGet_Upgrade_Doc.md
```

## Features

- ✅ Full-text search
- ✅ Syntax highlighting (C#, JSON, SQL, JS, TS)
- ✅ Mermaid diagram support
- ✅ Copy code button
- ✅ Responsive design
- ✅ Pagination
- ✅ Zoom images

## Adding New Documentation

1. Create a new folder for your feature/technology
2. Create 5 documentation files:
   - `*.introduction.md`
   - `*.plan.md`
   - `*.prompt.md`
   - `*.sourcecode.md`
   - `*.usageguilde.md`
3. Update `_sidebar.md` to add navigation links
4. Refresh the browser to see changes

## Customization

### Changing Theme

Edit `index.html` and change the CSS link:

```html
<!-- Available themes: vue, buble, dark, pure -->
<link rel="stylesheet" href="//cdn.jsdelivr.net/npm/docsify@4/lib/themes/vue.css">
```

### Changing Colors

Edit the CSS variables in `index.html`:

```css
:root {
  --theme-color: #4caf50;  /* Primary color */
  --theme-color-dark: #388e3c;  /* Dark variant */
}
```

## Deployment

### GitHub Pages

1. Push `docs` folder to GitHub
2. Go to repository Settings → Pages
3. Select branch and `/docs` folder
4. Save and wait for deployment

### Netlify

1. Create `netlify.toml` in project root:
   ```toml
   [build]
     publish = "docs"
   ```
2. Connect repository to Netlify
3. Deploy

## Troubleshooting

### Mermaid diagrams not rendering

- Make sure you're using 4 backticks for code blocks containing mermaid
- Check browser console for errors

### Search not working

- Clear browser cache
- Make sure `search.min.js` plugin is loaded

### Sidebar not showing

- Check `_sidebar.md` file exists
- Verify `loadSidebar: true` in `index.html`

## Resources

- [Docsify Documentation](https://docsify.js.org/)
- [Docsify Plugins](https://docsify.js.org/#/plugins)
- [Mermaid Documentation](https://mermaid.js.org/)

---

**Last Updated**: 2026-01-24
