#!/usr/bin/env node
"use strict";

// Local static host for the sample's generated manifest and AssetBundles.
const fs = require("node:fs");
const http = require("node:http");
const path = require("node:path");

const root = path.resolve(process.env.AFTER_HOURS_CONTENT_ROOT || path.join(process.cwd(), "ContentBuilds", "Current"));
const host = "127.0.0.1";
const port = Number(process.env.AFTER_HOURS_CONTENT_PORT || 27832);
let corruptFirstBundle = process.env.AFTER_HOURS_CORRUPT_FIRST_BUNDLE === "1";

http.createServer((request, response) => {
  if (request.method !== "GET") {
    response.writeHead(405, { Allow: "GET" }).end("GET only");
    return;
  }
  let relative;
  try {
    relative = decodeURIComponent(new URL(request.url, "http://localhost").pathname).replace(/^\/+/, "");
  } catch {
    response.writeHead(400).end("Invalid path");
    return;
  }
  if (!relative || relative.includes("\\") || relative.split("/").some((part) => part === ".." || part === ".")) {
    response.writeHead(400).end("Invalid path");
    return;
  }
  const file = path.resolve(root, relative);
  if (!file.startsWith(root + path.sep)) {
    response.writeHead(403).end("Outside content root");
    return;
  }
  fs.stat(file, (statError, stat) => {
    if (statError || !stat.isFile()) {
      response.writeHead(404).end("Not found");
      return;
    }
    response.writeHead(200, {
      "Content-Length": stat.size,
      "Content-Type": relative.endsWith(".json") ? "application/json; charset=utf-8" : "application/octet-stream",
      "Cache-Control": "no-store",
    });
    if (corruptFirstBundle && relative !== "manifest.json") {
      corruptFirstBundle = false;
      fs.readFile(file, (readError, bytes) => {
        if (readError) {
          response.writeHead(500).end("Could not read bundle");
          return;
        }
        if (bytes.length > 0) bytes[0] ^= 0xff;
        console.log(`CORRUPT ONCE / ${relative} / updater should reject the checksum`);
        response.end(bytes);
      });
      return;
    }
    fs.createReadStream(file).pipe(response);
  });
}).listen(port, host, () => {
  console.log(`After Hours content host listening on http://${host}:${port}/`);
  console.log(`Serving ${root}; run Build Local Bundle and Update Manifest before connecting.`);
  if (corruptFirstBundle) console.log("Failure injection armed: the first bundle response will have a bad checksum.");
  console.log("Loopback only. Press Ctrl+C to stop.");
}).on("error", (error) => {
  console.error(error.message);
  process.exitCode = 1;
});
