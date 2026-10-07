# Centauri64 Magazine — Issue 1 (January 1986)

Editorial PDF production for Issue 1.

## Source

- `source/Centauri64_Magazine_Issue_01_January_1986_v3.pdf` — editorial content/structure source (do not overwrite)
- `assets/issue01-cover.jpg` — approved cover (kept as page 1)

## Build

```bash
cd Magazine/Issue01
npm install
node build-pdf.mjs
```

Outputs:

- `Centauri64_Magazine_Issue_01_January_1986_v4.pdf`
- `output/Centauri64_Magazine_Issue_01_January_1986_v4.pdf`
- `output/pages/page-01.png` … `page-20.png` (visual QA)

Rebuild HTML only:

```bash
node build-html.mjs
```

## Files

| File | Role |
|------|------|
| `styles.css` | Dense 1985–86 print typography / columns / furniture |
| `build-html.mjs` | Issue 1 page content + layout |
| `build-pdf.mjs` | Puppeteer A4 PDF + page PNG render |
| `issue01.html` | Generated intermediate |

Keep Issue 1 specific. Extract helpers only when Issue 2 needs them.
