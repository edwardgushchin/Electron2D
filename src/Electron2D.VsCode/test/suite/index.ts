/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import { execFile } from "node:child_process";
import { readFile, rm } from "node:fs/promises";
import path from "node:path";
import { promisify } from "node:util";
import * as vscode from "vscode";

const execFileAsync = promisify(execFile);

interface TestApi {
  invoke(message: unknown): Promise<void>;
  getState(): { boardRevision: number; columns: Array<{ tasks: Array<{ taskId: string; revision: number; title: string; status: string; rank: string }> }> } | undefined;
  getLastError(): string | undefined;
  getSidebarRows(): Array<{ taskId: string; label: string; description: string }>;
  getSelectedTaskId(): string | undefined;
  getSelectedTaskRevision(): number | undefined;
  getNavigationState(): { currentTaskId: string | undefined; canGoBack: boolean };
  getLocale(): "en" | "ru";
}

export async function run(): Promise<void> {
  const extension = vscode.extensions.getExtension("electron2d.electron2d-taskboard");
  assert.ok(extension, "Electron2D Task Board extension must be discoverable.");

  const cliPath = process.env.E2D_TEST_CLI;
  assert.ok(cliPath, "E2D_TEST_CLI must name the built CLI apphost.");
  await vscode.workspace.getConfiguration("electron2d.taskboard").update(
    "cliPath",
    cliPath,
    vscode.ConfigurationTarget.Workspace);

  await extension.activate();
  await waitFor(() => vscode.window.tabGroups.all
    .flatMap(group => group.tabs)
    .some(tab => tab.input instanceof vscode.TabInputWebview && tab.input.viewType.endsWith("electron2d.taskBoard")));
  const startupTabs = vscode.window.tabGroups.all
    .flatMap(group => group.tabs)
    .filter(tab => tab.input instanceof vscode.TabInputWebview && tab.input.viewType.endsWith("electron2d.taskBoard"));
  const expectedPanelTitle = /^ru(?:-|$)/i.test(vscode.env.language) ? "Доска задач" : "Task Board";
  assert.equal(startupTabs.length, 1, "a canonical trusted workspace must open one task board during activation");
  assert.equal(startupTabs[0]?.label, expectedPanelTitle, "the startup editor title must follow the VS Code locale");
  assert.equal(
    vscode.window.tabGroups.activeTabGroup.activeTab,
    startupTabs[0],
    "the startup task board must become the active editor instead of Welcome");

  await vscode.commands.executeCommand("workbench.view.extension.electron2d-taskboard");
  await vscode.commands.executeCommand("workbench.view.extension.electron2d-taskboard");
  assert.equal(vscode.window.tabGroups.all
    .flatMap(group => group.tabs)
    .filter(tab => tab.input instanceof vscode.TabInputWebview && tab.input.viewType.endsWith("electron2d.taskBoard"))
    .length, 1, "Activity Bar launcher must reveal the existing task board without creating a duplicate.");

  const api = extension.exports as TestApi;
  assert.equal(typeof api.invoke, "function", "Extension test mode must expose its test API.");
  assert.equal(
    typeof api.getSelectedTaskRevision,
    "function",
    "Extension test mode must expose the hydrated revision used by task actions.");
  const expectedLocale = /^ru(?:-|$)/i.test(vscode.env.language) ? "ru" : "en";
  assert.equal(api.getLocale(), expectedLocale, "Extension locale must follow vscode.env.language.");
  const workspacePath = process.env.E2D_TEST_WORKSPACE;
  assert.ok(workspacePath);
  await waitFor(() => api.getState() !== undefined);

  await api.invoke({ type: "create", title: "Host-created task", description: "Created by Extension Host test", priority: "P1" });
  const taskPath = path.join(workspacePath, ".taskboard", "tasks", "T-0001.e2task");
  await waitFor(async () => await fileExists(taskPath));
  await waitFor(() => api.getSidebarRows().some(row => row.taskId === "T-0001"));
  const sidebarRow = api.getSidebarRows().find(row => row.taskId === "T-0001");
  assert.equal(sidebarRow?.label, "T-0001 · Host-created task");
  assert.equal(sidebarRow?.description, `${expectedLocale === "ru" ? "Готово к работе" : "Ready"} · P1`);

  await Promise.all([
    api.invoke({ type: "create", title: "Concurrent task A", description: "FIFO A", priority: "P2" }),
    api.invoke({ type: "create", title: "Concurrent task B", description: "FIFO B", priority: "P3" })
  ]);
  await waitFor(() => {
    const titles = api.getState()?.columns.flatMap(column => column.tasks).map(candidate => candidate.title) ?? [];
    return titles.includes("Concurrent task A") && titles.includes("Concurrent task B");
  });
  assert.equal(api.getLastError(), undefined, "concurrent extension mutations must be serialized without lock alerts");
  assert.equal(await fileExists(path.join(workspacePath, ".taskboard", "tasks", "T-0002.e2task")), true);
  assert.equal(await fileExists(path.join(workspacePath, ".taskboard", "tasks", "T-0003.e2task")), true);

  if (process.platform === "win32") {
    const lockPath = path.join(workspacePath, ".taskboard", ".lock");
    const readyPath = path.join(workspacePath, "taskboard-lock-ready.tmp");
    const holder = execFileAsync("powershell.exe", [
      "-NoProfile",
      "-NonInteractive",
      "-Command",
      "$stream=[System.IO.File]::Open($env:E2D_LOCK_PATH,[System.IO.FileMode]::OpenOrCreate,[System.IO.FileAccess]::ReadWrite,[System.IO.FileShare]::None); try {[System.IO.File]::WriteAllText($env:E2D_LOCK_READY,'ready'); Start-Sleep -Milliseconds 350} finally {$stream.Dispose()}"
    ], {
      windowsHide: true,
      env: { ...process.env, E2D_LOCK_PATH: lockPath, E2D_LOCK_READY: readyPath }
    });
    await waitFor(async () => await fileExists(readyPath));
    await api.invoke({ type: "create", title: "Retried after lock", description: "Transient recovery", priority: "P1" });
    await holder;
    await rm(readyPath, { force: true });
    await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
      .some(candidate => candidate.title === "Retried after lock") ?? false);
    assert.equal(api.getLastError(), undefined, "a short external lock must recover without a VS Code error alert");
  }

  const folderUri = vscode.workspace.workspaceFolders?.[0]?.uri.toString();
  assert.ok(folderUri);
  await vscode.commands.executeCommand("electron2d.openTaskFromSidebar", folderUri, "T-0001");
  assert.equal(api.getSelectedTaskId(), "T-0001", "Sidebar command must select the compact task before details hydration completes.");
  await waitFor(() => api.getSelectedTaskRevision() === 1);
  assert.deepEqual(api.getNavigationState(), { currentTaskId: "T-0001", canGoBack: false });
  await api.invoke({ type: "openTask", taskId: "T-0002", navigation: "internal" });
  await api.invoke({ type: "openTask", taskId: "T-0003", navigation: "internal" });
  assert.deepEqual(api.getNavigationState(), { currentTaskId: "T-0003", canGoBack: true });
  await api.invoke({ type: "navigateBack" });
  assert.equal(api.getSelectedTaskId(), "T-0002");
  await api.invoke({ type: "navigateBack" });
  assert.deepEqual(api.getNavigationState(), { currentTaskId: "T-0001", canGoBack: false });
  await api.invoke({ type: "openTask", taskId: "T-0001", navigation: "internal" });
  assert.equal(api.getNavigationState().canGoBack, false, "opening the current task must not add history");
  await api.invoke({ type: "closeTaskDetails" });
  assert.deepEqual(api.getNavigationState(), { currentTaskId: undefined, canGoBack: false });
  await vscode.commands.executeCommand("electron2d.openTaskFromSidebar", folderUri, "T-0001");
  await api.invoke({ type: "openFile", path: "README.md" });
  const activeFilePath = path.normalize(vscode.window.activeTextEditor?.document.uri.fsPath ?? "");
  const expectedFilePath = path.normalize(path.join(workspacePath, "README.md"));
  assert.equal(
    process.platform === "win32" ? activeFilePath.toLocaleLowerCase() : activeFilePath,
    process.platform === "win32" ? expectedFilePath.toLocaleLowerCase() : expectedFilePath,
    "a linked project file must open in the active VS Code editor group");
  let task = JSON.parse(await readFile(taskPath, "utf8")) as { revision: number; title: string; status: string; activity: unknown[] };
  assert.equal(task.revision, 1);

  await api.invoke({ type: "comment", taskId: "T-0001", text: "Host comment", expectedTaskRevision: 1 });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.revision === 2) ?? false);
  await new Promise(resolve => setTimeout(resolve, 250));
  await api.invoke({ type: "edit", taskId: "T-0001", title: "Host-edited task", description: "Edited", priority: "P2", expectedTaskRevision: 2 });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.revision === 3 && candidate.title === "Host-edited task") ?? false);
  await new Promise(resolve => setTimeout(resolve, 250));
  const board = JSON.parse(await readFile(path.join(workspacePath, ".taskboard", "board.e2tasks"), "utf8")) as { revision: number };
  await api.invoke({
    type: "move", taskId: "T-0001", groupId: null, rank: "00000500",
    expectedBoardRevision: board.revision
  });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.rank === "000000000500") ?? false);
  task = JSON.parse(await readFile(taskPath, "utf8")) as typeof task;
  assert.equal(task.title, "Host-edited task");
  assert.equal(task.status, "Ready");
  assert.equal(task.revision, 3);
  assert.equal(task.activity.length, 1);

  await new Promise(resolve => setTimeout(resolve, 500));
  await execFileAsync(cliPath, [
    "tasks", "comment", "add", "T-0001", "--text", "External watcher comment",
    "--expected-revision", "3", "--project", workspacePath, "--format", "json"
  ], { windowsHide: true });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.revision === 4) ?? false);
  await waitFor(() => api.getSelectedTaskRevision() === 4);

  const boardBeforeExternalMove = JSON.parse(
    await readFile(path.join(workspacePath, ".taskboard", "board.e2tasks"), "utf8")) as { revision: number };
  await execFileAsync(cliPath, [
    "tasks", "move", "T-0001", "--rank", "00000750",
    "--expected-board-revision", String(boardBeforeExternalMove.revision),
    "--project", workspacePath, "--format", "json"
  ], { windowsHide: true });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.rank === "000000000750") ?? false);

  await api.invoke({
    type: "move", taskId: "T-0001", groupId: null, rank: "00000850",
    expectedBoardRevision: boardBeforeExternalMove.revision
  });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.rank === "000000000850") ?? false);
  assert.equal(api.getLastError(), undefined, "a recovered board revision conflict must not surface as an alert");

  await execFileAsync(cliPath, [
    "tasks", "set-status", "T-0001", "--status", "InProgress",
    "--expected-revision", "4", "--project", workspacePath, "--format", "json"
  ], { windowsHide: true });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.revision === 5 && candidate.status === "InProgress") ?? false);
  await waitFor(() => api.getSelectedTaskRevision() === 5);

  await api.invoke({ type: "comment", taskId: "T-0001", text: "Stale", expectedTaskRevision: 4 });
  assert.match(api.getLastError() ?? "", /revision conflict/i);
  assert.equal(api.getSelectedTaskRevision(), 5, "a stale action must keep canonical hydrated details without retrying intent");
  task = JSON.parse(await readFile(taskPath, "utf8")) as typeof task;
  assert.equal(task.revision, 5, "task revision conflict recovery must not retry the stale comment");

  await execFileAsync(cliPath, [
    "tasks", "cancel", "T-0001", "--reason", "Extension Host reopen fixture",
    "--expected-revision", "5", "--project", workspacePath, "--format", "json"
  ], { windowsHide: true });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.revision === 6 && candidate.status === "Cancelled") ?? false);
  await api.invoke({
    type: "reopen", taskId: "T-0001", reason: "Reopened by Extension Host test", expectedTaskRevision: 6
  });
  await waitFor(() => api.getState()?.columns.flatMap(column => column.tasks)
    .some(candidate => candidate.taskId === "T-0001" && candidate.revision === 7 && candidate.status === "Ready") ?? false);
  assert.equal(api.getLastError(), undefined, "reopen must refresh canonical Ready state without a local error");

  await vscode.workspace.getConfiguration("electron2d.taskboard").update(
    "cliPath",
    path.join(workspacePath, "missing-e2d"),
    vscode.ConfigurationTarget.Workspace);
  await api.invoke({ type: "refresh" });
  assert.match(api.getLastError() ?? "", /ENOENT|not found/i);
  await vscode.workspace.getConfiguration("electron2d.taskboard").update(
    "cliPath",
    cliPath,
    vscode.ConfigurationTarget.Workspace);
}

async function waitFor(predicate: () => boolean | Promise<boolean>): Promise<void> {
  const deadline = Date.now() + 10_000;
  while (!await predicate()) {
    if (Date.now() >= deadline) {
      const tabs = vscode.window.tabGroups.all.flatMap(group => group.tabs).map(tab => ({
        label: tab.label,
        inputType: typeof tab.input === "object" && tab.input !== null ? tab.input.constructor.name : typeof tab.input,
        viewType: tab.input instanceof vscode.TabInputWebview ? tab.input.viewType : null
      }));
      console.error("Open tab inventory:", JSON.stringify(tabs));
      throw new Error("Timed out waiting for Electron2D task board webview.");
    }

    await new Promise(resolve => setTimeout(resolve, 50));
  }
}

async function fileExists(filePath: string): Promise<boolean> {
  try {
    await readFile(filePath);
    return true;
  } catch {
    return false;
  }
}
