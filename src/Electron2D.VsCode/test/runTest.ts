/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import path from "node:path";
import { cp, mkdir, rm, writeFile } from "node:fs/promises";
import { runTests } from "@vscode/test-electron";

async function main(): Promise<void> {
  const extensionDevelopmentPath = path.resolve(__dirname, "../..");
  const extensionTestsPath = path.resolve(__dirname, "suite/index.js");
  const workspacePath = path.resolve(extensionDevelopmentPath, ".vscode-test/workspaces/taskboard-extension");
  const expectedPrefix = path.resolve(extensionDevelopmentPath, ".vscode-test") + path.sep;
  if (!workspacePath.startsWith(expectedPrefix)) {
    throw new Error("Extension Host fixture path escaped .vscode-test.");
  }

  await rm(workspacePath, { recursive: true, force: true });
  await mkdir(workspacePath, { recursive: true });
  await cp(
    path.resolve(extensionDevelopmentPath, "../../data/templates/electron2d-empty/.taskboard"),
    path.join(workspacePath, ".taskboard"),
    { recursive: true });
  await writeFile(path.join(workspacePath, "README.md"), "# Extension Host fixture\n", "utf8");
  const portableDataPath = path.resolve(extensionDevelopmentPath, ".vscode-test/portable-data");
  const cliName = process.platform === "win32" ? "e2d.exe" : "e2d";
  const cliPath = path.resolve(extensionDevelopmentPath, `../Electron2D.Cli/bin/Debug/net10.0/${cliName}`);
  const workspaceSettingsPath = path.join(workspacePath, ".vscode", "settings.json");
  await mkdir(path.dirname(workspaceSettingsPath), { recursive: true });
  await writeFile(
    workspaceSettingsPath,
    JSON.stringify({ "electron2d.taskboard.cliPath": cliPath }, undefined, 2),
    "utf8");

  const requestedVersion = process.env.E2D_VSCODE_TEST_VERSION;
  await runTests({
    ...(requestedVersion ? { version: requestedVersion } : {}),
    extensionDevelopmentPath,
    extensionTestsPath,
    launchArgs: [workspacePath, "--disable-extensions", "--disable-workspace-trust", "--disable-updates"],
    extensionTestsEnv: { E2D_TEST_CLI: cliPath, E2D_TEST_WORKSPACE: workspacePath, VSCODE_PORTABLE: portableDataPath }
  });
}

void main().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
