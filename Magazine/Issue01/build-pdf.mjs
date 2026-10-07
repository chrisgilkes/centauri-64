import fs from "fs";
import path from "path";
import { fileURLToPath, pathToFileURL } from "url";
import puppeteer from "puppeteer";
import { execSync } from "child_process";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const outDir = path.join(__dirname, "output");
const pagesDir = path.join(outDir, "pages");

fs.mkdirSync(pagesDir, { recursive: true });

execSync("node build-html.mjs", { cwd: __dirname, stdio: "inherit" });

const htmlPath = path.join(__dirname, "issue01.html");
const pdfPath = path.join(
  outDir,
  "Centauri64_Magazine_Issue_01_January_1986_v4.pdf"
);

const browser = await puppeteer.launch({
  headless: true,
  args: ["--allow-file-access-from-files"],
});

const page = await browser.newPage();
await page.goto(pathToFileURL(htmlPath).href, {
  waitUntil: "networkidle0",
});

await page.pdf({
  path: pdfPath,
  format: "A4",
  printBackground: true,
  preferCSSPageSize: true,
  margin: { top: "0", right: "0", bottom: "0", left: "0" },
});

console.log("Wrote", pdfPath);

// Render each page to PNG for visual QA
const sections = await page.$$(".page");
console.log(`Rendering ${sections.length} page images…`);

for (let i = 0; i < sections.length; i++) {
  const el = sections[i];
  const file = path.join(pagesDir, `page-${String(i + 1).padStart(2, "0")}.png`);
  await el.screenshot({ path: file, type: "png" });
  console.log(" ", path.basename(file));
}

await browser.close();

// Also copy PDF to Magazine root for easy find
fs.copyFileSync(
  pdfPath,
  path.join(__dirname, "Centauri64_Magazine_Issue_01_January_1986_v4.pdf")
);

console.log("Done.");
