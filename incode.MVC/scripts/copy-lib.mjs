// 把後台用到的前端套件檔案從 node_modules 複製到 wwwroot/lib（npm run lib；dotnet build 前由 incode.MVC.csproj 自動執行）。
// wwwroot/lib 不提交進 git，每次都整個重建：新增套件時在 package.json 加相依（固定版本），再把要用的檔案加進下面的清單。
import { cpSync, existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const modules = join(root, "node_modules");
const lib = join(root, "wwwroot", "lib");

// [node_modules 內的路徑, wwwroot/lib 內的路徑]
const files = [
  ["jquery/dist/jquery.min.js", "jquery/dist/jquery.min.js"],
  ["jquery/dist/jquery.min.map", "jquery/dist/jquery.min.map"],
  ["jquery/LICENSE.txt", "jquery/LICENSE.txt"],

  ["bootstrap/dist/css/bootstrap.min.css", "bootstrap/dist/css/bootstrap.min.css"],
  ["bootstrap/dist/css/bootstrap.min.css.map", "bootstrap/dist/css/bootstrap.min.css.map"],
  ["bootstrap/dist/js/bootstrap.bundle.min.js", "bootstrap/dist/js/bootstrap.bundle.min.js"],
  ["bootstrap/dist/js/bootstrap.bundle.min.js.map", "bootstrap/dist/js/bootstrap.bundle.min.js.map"],
  ["bootstrap/LICENSE", "bootstrap/LICENSE"],

  ["jquery-validation/dist/jquery.validate.min.js", "jquery-validation/dist/jquery.validate.min.js"],
  ["jquery-validation/LICENSE.md", "jquery-validation/LICENSE.md"],
  ["jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js", "jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"],
  ["jquery-validation-unobtrusive/LICENSE.txt", "jquery-validation-unobtrusive/LICENSE.txt"],

  ["@fortawesome/fontawesome-free/css/all.min.css", "font-awesome/css/all.min.css"],
  ["@fortawesome/fontawesome-free/webfonts", "font-awesome/webfonts"],
  ["@fortawesome/fontawesome-free/LICENSE.txt", "font-awesome/LICENSE.txt"],

  ["chart.js/dist/chart.umd.min.js", "chart.js/chart.umd.min.js"],
  ["chart.js/LICENSE.md", "chart.js/LICENSE.md"],

  ["dompurify/dist/purify.min.js", "dompurify/dist/purify.min.js"],
  ["dompurify/LICENSE", "dompurify/LICENSE"],

  ["datatables.net/js/jquery.dataTables.min.js", "datatables/jquery.dataTables.min.js"],
  ["datatables.net-bs5/js/dataTables.bootstrap5.min.js", "datatables/dataTables.bootstrap5.min.js"],
  ["datatables.net-bs5/css/dataTables.bootstrap5.min.css", "datatables/dataTables.bootstrap5.min.css"],
  ["datatables.net-fixedheader/js/dataTables.fixedHeader.min.js", "datatables/dataTables.fixedHeader.min.js"],
  ["datatables.net-plugins/i18n/zh-HANT.json", "datatables/i18n/zh-HANT.json"],
  ["datatables.net/License.txt", "datatables/License.txt"],

  ["select2/dist/js/select2.min.js", "select2/select2.min.js"],
  ["select2/dist/css/select2.min.css", "select2/select2.min.css"],
  ["select2/LICENSE.md", "select2/LICENSE.md"],

  ["@microsoft/signalr/dist/browser/signalr.min.js", "signalr/signalr.min.js"],

  ["marked/lib/marked.umd.js", "marked/marked.umd.js"],
  ["marked/LICENSE", "marked/LICENSE"],

  ["@fontsource/nunito/LICENSE", "nunito/LICENSE"],
];

// Nunito（登入頁）：woff2 字型檔；各字重的 CSS 合併成 nunito/nunito.css（依 unicode-range 只會下載需要的字集）
const nunito = join(modules, "@fontsource", "nunito");
const nunitoCss = existsSync(nunito)
  ? readdirSync(nunito).filter((f) => /^\d{3}(-italic)?\.css$/.test(f)).sort()
  : [];
if (existsSync(nunito)) {
  for (const name of readdirSync(join(nunito, "files")).filter((f) => f.endsWith(".woff2")))
    files.push([`@fontsource/nunito/files/${name}`, `nunito/files/${name}`]);
}

const missing = files.filter(([from]) => !existsSync(join(modules, from)));
if (missing.length > 0) {
  console.error("找不到以下檔案（先執行 npm ci；套件升級後路徑可能改變）：");
  for (const [from] of missing) console.error("  node_modules/" + from);
  process.exit(1);
}

rmSync(lib, { recursive: true, force: true });
for (const [from, to] of files) {
  const target = join(lib, to);
  mkdirSync(dirname(target), { recursive: true });
  cpSync(join(modules, from), target, { recursive: true });
}
if (nunitoCss.length === 0) {
  console.error("找不到 @fontsource/nunito 的 CSS（先執行 npm ci）");
  process.exit(1);
}
// 只保留 woff2（目前的瀏覽器都支援），移除指向 .woff 的備援來源
const css = nunitoCss
  .map((name) => readFileSync(join(nunito, name), "utf8").replace(/, url\([^)]*\.woff\) format\('woff'\)/g, ""))
  .join("\n");
writeFileSync(join(lib, "nunito", "nunito.css"), css);

console.log(`已複製 ${files.length} 個項目到 wwwroot/lib`);
