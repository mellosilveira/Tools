/**
 * Export .drawio diagrams to .svg and high-DPI .png using the locally installed
 * VS Code Draw.io (diagrams.net) offline engine via headless Chrome/Edge.
 *
 * Usage:
 *   node paper/scripts/export_drawio.mjs [file1.drawio file2.drawio ...]
 *   (Defaults to all .drawio files in paper/figures/)
 */

import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const FIGURES_DIR = path.resolve(REPO_ROOT, "paper", "figures");

function findDrawioWebapp() {
  const extRoot = path.join(os.homedir(), ".vscode", "extensions");
  if (fs.existsSync(extRoot)) {
    const dirs = fs
      .readdirSync(extRoot)
      .filter((d) => d.startsWith("hediet.vscode-drawio-"))
      .sort()
      .reverse();
    for (const d of dirs) {
      const candidate = path.join(extRoot, d, "drawio", "src", "main", "webapp");
      if (fs.existsSync(path.join(candidate, "index.html"))) {
        return candidate;
      }
    }
  }
  throw new Error("Offline Draw.io webapp not found in ~/.vscode/extensions/hediet.vscode-drawio-*");
}

function findBrowser() {
  const candidates = [
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  throw new Error("Neither Chrome nor Edge found for headless Draw.io export.");
}

const MIME_TYPES = {
  ".html": "text/html; charset=utf-8",
  ".js": "application/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".gif": "image/gif",
  ".json": "application/json; charset=utf-8",
  ".txt": "text/plain; charset=utf-8",
  ".xml": "application/xml; charset=utf-8",
  ".woff": "font/woff",
  ".woff2": "font/woff2",
  ".ttf": "font/ttf",
};

function writeFileWithRetry(filePath, data) {
  const tmp = `${filePath}.${process.pid}.${Date.now()}.tmp`;
  fs.writeFileSync(tmp, data);
  for (let attempt = 0; attempt < 8; attempt++) {
    try {
      fs.renameSync(tmp, filePath);
      return;
    } catch (err) {
      if (attempt === 7) {
        try { fs.unlinkSync(tmp); } catch {}
        throw err;
      }
      Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 150);
    }
  }
}

async function exportSingleDrawio(webappDir, browserPath, drawioPath) {
  const absPath = path.resolve(drawioPath);
  const xmlContent = fs.readFileSync(absPath, "utf8");
  const stem = absPath.replace(/\.drawio$/i, "");
  const outSvg = `${stem}.svg`;
  const outPng = `${stem}.png`;

  const harnessHtml = `<!doctype html>
<html>
<head><meta charset="utf-8"></head>
<body style="margin:0;overflow:hidden;">
<iframe id="drawio" style="width:1600px;height:1200px;border:0;"
  src="/index.html?embed=1&proto=json&offline=1&spin=0&ui=min&noSaveBtn=1&noExitBtn=1"></iframe>
<script>
(async function () {
  const iframe = document.getElementById("drawio");
  const xml = ${JSON.stringify(xmlContent)};
  let stage = "init";

  window.addEventListener("message", async (evt) => {
    if (typeof evt.data !== "string") return;
    let msg;
    try { msg = JSON.parse(evt.data); } catch { return; }

    if (msg.event === "init") {
      iframe.contentWindow.postMessage(JSON.stringify({
        action: "load",
        xml: xml,
        autosave: 0
      }), "*");
    } else if (msg.event === "load") {
      stage = "export_svg";
      iframe.contentWindow.postMessage(JSON.stringify({
        action: "export",
        format: "svg",
        border: 14,
        background: "#ffffff",
        embedImages: true,
        shadows: false
      }), "*");
    } else if (msg.event === "export" && stage === "export_svg") {
      await fetch("/save?fmt=svg", { method: "POST", body: msg.data });
      stage = "export_png";
      iframe.contentWindow.postMessage(JSON.stringify({
        action: "export",
        format: "png",
        scale: 3,
        border: 14,
        background: "#ffffff",
        shadows: false
      }), "*");
    } else if (msg.event === "export" && stage === "export_png") {
      await fetch("/save?fmt=png", { method: "POST", body: msg.data });
      await fetch("/done", { method: "POST", body: "ok" });
    }
  });
})();
</script>
</body>
</html>`;

  await new Promise((resolve, reject) => {
    let settled = false;
    let browserProc = null;
    const tmpProfile = fs.mkdtempSync(path.join(os.tmpdir(), "drawio-export-"));

    const finish = (err) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      if (browserProc && !browserProc.killed) {
        try { browserProc.kill(); } catch {}
      }
      server.close(() => {
        try { fs.rmSync(tmpProfile, { recursive: true, force: true }); } catch {}
        if (err) reject(err);
        else resolve();
      });
    };

    const timer = setTimeout(() => finish(new Error(`Timeout exporting ${path.basename(absPath)}`)), 45000);

    const server = http.createServer((req, res) => {
      const urlObj = new URL(req.url, "http://127.0.0.1");
      if (urlObj.pathname === "/__harness.html") {
        res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
        res.end(harnessHtml);
        return;
      }

      if (urlObj.pathname === "/save" && req.method === "POST") {
        const fmt = urlObj.searchParams.get("fmt");
        const chunks = [];
        req.on("data", (c) => chunks.push(c));
        req.on("end", () => {
          const payload = Buffer.concat(chunks).toString("utf8");
          try {
            if (fmt === "svg") {
              const b64Prefix = "data:image/svg+xml;base64,";
              const utfPrefix = "data:image/svg+xml,";
              let svgText = payload;
              if (payload.startsWith(b64Prefix)) {
                svgText = Buffer.from(payload.slice(b64Prefix.length), "base64").toString("utf8");
              } else if (payload.startsWith(utfPrefix)) {
                svgText = decodeURIComponent(payload.slice(utfPrefix.length));
              }
              writeFileWithRetry(outSvg, svgText);
            } else if (fmt === "png") {
              const prefix = "data:image/png;base64,";
              const b64 = payload.startsWith(prefix) ? payload.slice(prefix.length) : payload;
              writeFileWithRetry(outPng, Buffer.from(b64, "base64"));
            }
            res.writeHead(200);
            res.end("saved");
          } catch (e) {
            res.writeHead(500);
            res.end(String(e));
            finish(e);
          }
        });
        return;
      }

      if (urlObj.pathname === "/done" && req.method === "POST") {
        res.writeHead(200);
        res.end("ok");
        finish(null);
        return;
      }

      let relPath = decodeURIComponent(urlObj.pathname).replace(/^\/+/, "");
      if (!relPath) relPath = "index.html";
      const target = path.resolve(webappDir, relPath);
      if (!target.startsWith(webappDir) || !fs.existsSync(target) || fs.statSync(target).isDirectory()) {
        res.writeHead(404);
        res.end("Not found");
        return;
      }
      const ext = path.extname(target).toLowerCase();
      res.writeHead(200, { "Content-Type": MIME_TYPES[ext] || "application/octet-stream" });
      fs.createReadStream(target).pipe(res);
    });

    server.listen(0, "127.0.0.1", () => {
      const { port } = server.address();
      const url = `http://127.0.0.1:${port}/__harness.html`;
      browserProc = spawn(
        browserPath,
        [
          "--headless=new",
          "--disable-gpu",
          "--no-sandbox",
          `--user-data-dir=${tmpProfile}`,
          url,
        ],
        { stdio: "ignore" }
      );
      browserProc.on("error", (err) => finish(err));
    });
  });

  console.log(`Exported: ${path.relative(REPO_ROOT, outSvg)} & ${path.relative(REPO_ROOT, outPng)}`);
}

async function main() {
  const args = process.argv.slice(2);
  const files = args.length
    ? args
    : fs
        .readdirSync(FIGURES_DIR)
        .filter((f) => f.endsWith(".drawio"))
        .sort()
        .map((f) => path.join(FIGURES_DIR, f));

  if (files.length === 0) {
    console.log("No .drawio files found.");
    return;
  }

  const webappDir = findDrawioWebapp();
  const browserPath = findBrowser();

  for (const file of files) {
    await exportSingleDrawio(webappDir, browserPath, file);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
