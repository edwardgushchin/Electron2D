/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { build } from "esbuild";
import { copyFile, mkdir } from "node:fs/promises";

await Promise.all([
  build({
    entryPoints: ["src/extension.ts"],
    outfile: "dist/extension.js",
    bundle: true,
    platform: "node",
    target: "node24",
    format: "cjs",
    external: ["vscode"],
    sourcemap: true,
    logLevel: "info"
  }),
  build({
    entryPoints: ["src/webview.ts"],
    outfile: "dist/webview.js",
    bundle: true,
    platform: "browser",
    target: "es2024",
    format: "iife",
    minify: true,
    sourcemap: true,
    logLevel: "info"
  }),
  build({
    entryPoints: ["test/runTest.ts"],
    outfile: "dist/test/runTest.js",
    bundle: true,
    platform: "node",
    target: "node24",
    format: "cjs",
    external: ["vscode"],
    sourcemap: true,
    logLevel: "info"
  }),
  build({
    entryPoints: ["test/suite/index.ts"],
    outfile: "dist/test/suite/index.js",
    bundle: true,
    platform: "node",
    target: "node24",
    format: "cjs",
    external: ["vscode"],
    sourcemap: true,
    logLevel: "info"
  })
]);

await mkdir("dist", { recursive: true });
await copyFile("src/webview.css", "dist/webview.css");
