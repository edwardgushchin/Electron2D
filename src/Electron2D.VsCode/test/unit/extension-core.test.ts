/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import path from "node:path";
import test from "node:test";
import { selectTaskboardCandidates } from "../../src/workspace.js";
import { buildWebviewHtml } from "../../src/webviewHtml.js";

test("workspace candidate selection keeps only roots with canonical board.e2tasks", () => {
  const candidates = selectTaskboardCandidates([
    { name: "first", path: "C:/first", hasBoard: false },
    { name: "second", path: "C:/second", hasBoard: true },
    { name: "third", path: "C:/third", hasBoard: true }
  ]);

  assert.deepEqual(candidates.map(candidate => candidate.path), ["C:/second", "C:/third"]);
});

test("webview HTML uses external local resources and contains no task data", () => {
  const html = buildWebviewHtml({
    cspSource: "vscode-webview://fixture",
    nonce: "abc123",
    scriptUri: "vscode-webview://fixture/webview.js",
    styleUri: "vscode-webview://fixture/webview.css",
    locale: "ru"
  });

  assert.match(html, /default-src 'none'/);
  assert.match(html, /nonce-abc123/);
  assert.match(html, /vscode-webview:\/\/fixture\/webview\.js/);
  assert.doesNotMatch(html, /<script(?![^>]+src=)[^>]*>/);
  assert.doesNotMatch(html, /TASKS\.md|\.electron2d\/tasks/);
  assert.match(html, /<aside[^>]+id="details"[^>]+aria-hidden="true"[^>]+inert[^>]+hidden/);
});

test("webview resources are cache-busted for every panel instance", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /const resourceQuery = `panel=\$\{nonce\}`/);
  assert.match(source, /vscode\.Uri\.joinPath\(distRoot, "webview\.js"\)\.with\(\{ query: resourceQuery \}\)/);
  assert.match(source, /vscode\.Uri\.joinPath\(distRoot, "webview\.css"\)\.with\(\{ query: resourceQuery \}\)/);
});

test("webview header matches the compact task board reference", () => {
  const html = buildWebviewHtml({
    cspSource: "vscode-webview://fixture",
    nonce: "abc123",
    scriptUri: "vscode-webview://fixture/webview.js",
    styleUri: "vscode-webview://fixture/webview.css",
    locale: "ru"
  });

  assert.match(html, /<html lang="ru">/);
  assert.match(html, /class="project-breadcrumb"[\s\S]*class="brand-mark"[\s\S]*<h1 id="project"><\/h1>[\s\S]*class="breadcrumb-separator">\/<[\s\S]*class="board-name">Доска задач</);
  assert.doesNotMatch(html, /<h1>Electron2D Task Board<\/h1>|taskboard \(main\)/);
  assert.match(html, /class="search-icon"[^>]+viewBox="0 0 16 16"/);
  assert.match(html, /id="filter"[^>]+placeholder="Поиск задач…"[^>]+aria-keyshortcuts="Control\+F"/);
  assert.match(html, /class="search-shortcut">Ctrl\+F<\/kbd>/);
  assert.match(html, /id="filter-toggle" class="secondary-control filter-button"[^>]+aria-controls="filter-panel"[^>]+aria-expanded="false"[\s\S]*class="filter-label">Фильтры<[\s\S]*id="filter-count"/);
  assert.match(html, /id="priority-filter"[\s\S]*<option value="">Все приоритеты<\/option>/);
  assert.match(html, /<label for="tag-filter-trigger">Теги<\/label>[\s\S]*id="tag-filter"[\s\S]*id="tag-filter-trigger"[^>]+role="combobox"[\s\S]*id="tag-filter-value">Все теги<\/span>/);
  assert.match(html, /id="tag-filter-popup"[^>]+role="listbox"[^>]+aria-multiselectable="true"/);
  assert.match(html, /class="archive-toggle secondary-control"[\s\S]*id="show-archived"[\s\S]*>Архив</);
  assert.match(html, /id="refresh"[^>]+aria-label="Обновить доску"[\s\S]*class="refresh-icon"[^>]+viewBox="0 0 16 16"/);
  assert.doesNotMatch(html, /<span class="refresh-icon"[^>]*>↻<\/span>/);
  assert.match(html, /<button id="create" class="primary-button" type="button">Создать задачу<\/button>/);
  assert.doesNotMatch(html, /create-menu-toggle|id="create-menu"|create-secondary|refresh-secondary/);
});

test("extension contributes an Activity Bar launcher with a packaged safe SVG icon", async () => {
  const manifest = JSON.parse(await readFile(path.join(process.cwd(), "package.json"), "utf8")) as {
    activationEvents?: string[];
    contributes?: {
      viewsContainers?: { activitybar?: Array<{ id: string; title: string; icon: string }> };
      views?: Record<string, Array<{ id: string; name: string }>>;
    };
  };

  assert.ok(manifest.activationEvents?.includes("onView:electron2d.taskBoardLauncher"));
  assert.ok(manifest.activationEvents?.includes("workspaceContains:.taskboard/board.e2tasks"));
  assert.deepEqual(manifest.contributes?.viewsContainers?.activitybar, [{
    id: "electron2d-taskboard",
    title: "%viewContainer.title%",
    icon: "media/taskboard.svg"
  }]);
  assert.deepEqual(manifest.contributes?.views?.["electron2d-taskboard"], [{
    id: "electron2d.taskBoardLauncher",
    name: "%view.name%"
  }]);

  const icon = await readFile(path.join(process.cwd(), "media", "taskboard.svg"), "utf8");
  assert.match(icon, /^<svg[^>]+viewBox="0 0 24 24"/);
  assert.doesNotMatch(icon, /<script|<foreignObject|(?:xlink:)?href=|data:/i);
});

test("trusted canonical workspace opens the task board silently during activation", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /export async function activate\(context: vscode\.ExtensionContext\): Promise<unknown>/);
  assert.match(source, /await openStartupTaskBoard\(context\)/);

  const helper = source.match(/async function openStartupTaskBoard\([\s\S]+?\n\}/);
  assert.ok(helper, "a dedicated startup path must be present");
  assert.match(helper[0], /if \(!vscode\.workspace\.isTrusted\) \{\s*return;/);
  assert.match(helper[0], /vscode\.workspace\.workspaceFolders \?\? \[\]/);
  assert.match(helper[0], /vscode\.Uri\.joinPath\(folder\.uri, "\.taskboard", "board\.e2tasks"\)/);
  assert.match(helper[0], /await TaskBoardPanel\.open\(context, folder\);\s*return;/);
  assert.doesNotMatch(helper[0], /selectWorkspaceFolder|showWarningMessage|showQuickPick/);
  assert.doesNotMatch(source, /workbench\.action\.closeActiveEditor|closeWelcome/);
});

test("sidebar selection on an existing panel skips the full board refresh", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const openMethod = source.match(/public static async open[\s\S]+?const distRoot/);
  assert.ok(openMethod, "TaskBoardPanel.open must be present");
  const existingBranch = openMethod[0].match(/if \(existing\) \{([\s\S]+?)return;/);
  assert.ok(existingBranch, "TaskBoardPanel.open must handle an existing panel");
  assert.doesNotMatch(existingBranch[1]!, /existing\.refresh\(/);
  assert.match(existingBranch[1]!, /existing\.showTask\(taskId, "direct"\)/);
  assert.match(existingBranch[1]!, /existing\.panel\.reveal\(vscode\.ViewColumn\.Active,\s*false\)/);
});

test("task details use a panel-scoped hydrated cache and explicit loading messages", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /private readonly taskDetailsCache = new Map<string, BoardTask>\(\)/);
  assert.match(source, /const cached = this\.taskDetailsCache\.get\(taskId\)/);
  assert.match(source, /compact: card,\s*cached,/);
  assert.match(source, /loading: phase === "loading"/);
  assert.match(source, /remember: task => this\.taskDetailsCache\.set\(taskId, task\)/);
  assert.match(source, /type: "taskError"/);
});

test("task details navigation distinguishes direct internal back and close intents", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /TaskNavigationHistory/);
  assert.match(source, /private readonly taskNavigation = new TaskNavigationHistory\(\)/);
  assert.match(source, /existing\.showTask\(taskId, "direct"\)/);
  assert.match(source, /created\.showTask\(taskId, "direct"\)/);
  assert.match(source, /message\.type === "openTask"[\s\S]+this\.showTask\(message\.taskId, message\.navigation\)/);
  assert.match(source, /message\.type === "navigateBack"[\s\S]+this\.navigateBack\(\)/);
  assert.match(source, /message\.type === "closeTaskDetails"[\s\S]+this\.closeTaskDetails\(\)/);
  assert.match(source, /canGoBack: this\.taskNavigation\.canGoBack/);
  assert.match(source, /isCurrent: \(\) => this\.taskNavigation\.currentTaskId === taskId/);
});

test("editor panel title leaves the workspace name to VS Code", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /createWebviewPanel\(\s*viewType,\s*l10n\.t\("extension\.panelTitle"\),/s);
  assert.doesNotMatch(source, /Tasks — \$\{folder\.name\}/);
});

test("editor board and sidebar serialize watcher refreshes with a trailing read", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /import[\s\S]+TrailingRefreshCoordinator[\s\S]+from "\.\/cli\.js"/);
  assert.equal(
    (source.match(/new TrailingRefreshCoordinator\(/g) ?? []).length,
    2,
    "sidebar and editor panel must each serialize their refresh lifecycle");
  assert.match(source, /getChildren[\s\S]+await this\.refreshCoordinator\.request\(\)/);
  assert.match(source, /private async refresh\(\): Promise<void> \{\s*await this\.refreshCoordinator\.request\(\);\s*\}/);
  assert.match(source, /private async refreshOnce\(\): Promise<void>/);
});

test("all extension CLI paths share the workspace scheduler and transient lock retry", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /import[\s\S]+WorkspaceCommandScheduler[\s\S]+from "\.\/cli\.js"/);
  assert.match(source, /const workspaceCommandScheduler = new WorkspaceCommandScheduler\(\)/);
  assert.match(
    source,
    /async function runSerializedTaskCommand[\s\S]+workspaceCommandScheduler\.run[\s\S]+runWithTransientTaskboardLockRetry/);
  assert.match(
    source,
    /async function runTaskCliForFolder[\s\S]+runSerializedTaskCommand\([\s\S]+runTaskCli\(/);
  assert.match(
    source,
    /runSerializedTaskCommand\([\s\S]+runHumanDecisionCli\(/,
    "trusted human decisions must not bypass serialization or retry");
});

test("agent chat persists trusted human and final agent messages around one canonical context checkpoint", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const sendBranch = source.match(/private async sendAgentMessage[\s\S]+?private async cancelAgentRun/);
  assert.ok(sendBranch, "the task-scoped agent chat controller must be present");
  assert.match(sendBranch[0], /runHumanMessageCli\([\s\S]+runAgentContextCli\([\s\S]+agentBackend\.run\([\s\S]+runAgentMessageCli\(/);
  assert.match(sendBranch[0], /current\.task\.revision !== prepared\.checkpointRevision/);
  assert.match(sendBranch[0], /agentRunId: runId/);
});

test("agent chat starts a fresh run and safely refreshes only a stale human append", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const sendBranch = source.match(/private async sendAgentMessage[\s\S]+?private async cancelAgentRun/);
  assert.ok(sendBranch, "the task-scoped agent chat controller must be present");
  assert.match(source, /import[\s\S]+runWithTaskRevisionConflictRetry[\s\S]+from "\.\/cli\.js"/);
  assert.match(
    sendBranch[0],
    /postAgentEvent\(taskId, \{\s*kind: "status",\s*runId,\s*status: "connecting"[\s\S]+runWithTaskRevisionConflictRetry\([\s\S]+runHumanMessageCli\(/);
  assert.match(sendBranch[0], /const taskUid = task\.taskUid[\s\S]+taskReadArguments\(taskId\)/);
});

test("accept action completes internal review through the serialized human workflow", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /import[\s\S]+runHumanAcceptanceWorkflow[\s\S]+from "\.\/cli\.js"/);
  assert.match(source, /message\.type === "accept"[\s\S]+this\.taskDetailsCache\.get\(message\.taskId\)/);
  assert.match(source, /message\.type === "accept"[\s\S]+runSerializedTaskCommand\([\s\S]+runHumanAcceptanceWorkflow\(/);
  assert.match(source, /runHumanAcceptanceWorkflow\([\s\S]+"submit", message\.taskId/);
  assert.match(source, /runHumanAcceptanceWorkflow\([\s\S]+runHumanDecisionCli\(/);
});

test("cancelled lifecycle handlers confirm archive and pass canonical revisions to CLI", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const reopenBranch = source.match(/if \(message\.type === "reopen"\) \{([\s\S]+?)\n\s*return;\n\s*\}/);
  assert.ok(reopenBranch, "reopen handler must be present");
  assert.match(reopenBranch[1]!, /mutateTaskAndRefreshDetails\(message\.taskId, reopenTaskArguments\(/);

  const archiveBranch = source.match(/if \(message\.type === "archive"\) \{([\s\S]+?)\n\s*return;\n\s*\}/);
  assert.ok(archiveBranch, "archive handler must be present");
  assert.match(archiveBranch[1]!, /vscode\.window\.showWarningMessage\(/);
  assert.match(archiveBranch[1]!, /if \(!isConfirmedDialogResult\(confirmed, label\)\) \{\s*return;/);
  assert.match(archiveBranch[1]!, /mutateTaskAndRefreshDetails\(message\.taskId, archiveTaskArguments\(/);

  const unarchiveBranch = source.match(/if \(message\.type === "unarchive"\) \{([\s\S]+?)\n\s*return;\n\s*\}/);
  assert.ok(unarchiveBranch, "unarchive handler must be present");
  assert.match(unarchiveBranch[1]!, /mutateTaskAndRefreshDetails\(message\.taskId, unarchiveTaskArguments\(/);
});

test("task board routes operation errors through standard VS Code alerts", async () => {
  const extensionSource = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const webviewSource = await readFile(path.join(process.cwd(), "src", "webview.ts"), "utf8");
  const webviewCss = await readFile(path.join(process.cwd(), "src", "webview.css"), "utf8");
  const html = buildWebviewHtml({
    cspSource: "vscode-webview://fixture",
    nonce: "abc123",
    scriptUri: "vscode-webview://fixture/webview.js",
    styleUri: "vscode-webview://fixture/webview.css"
  });

  assert.match(
    extensionSource,
    /private async postError\(message: string\): Promise<void> \{\s*this\.lastError = message;\s*void vscode\.window\.showErrorMessage\(message\);\s*\}/);
  assert.doesNotMatch(extensionSource, /postMessage\(\{ type: "error"/);
  assert.doesNotMatch(html, /id="notice"/);
  assert.doesNotMatch(webviewSource, /message\.type === "error"/);
  assert.doesNotMatch(webviewCss, /#notice/);
  assert.match(webviewSource, /message\.type === "taskError"/);
});

test("card placement invokes only tasks move and never changes task status", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const moveBranch = source.match(/if \(message\.type === "move"\) \{([\s\S]+?)\n\s*return;\n\s*\}/);
  assert.ok(moveBranch, "move message handler must be present");
  assert.match(moveBranch[1]!, /moveWithRevisionRecovery\(message\)/);
  const recovery = source.match(/private async moveWithRevisionRecovery\([\s\S]+?\n\s*private isMoveApplicable/);
  assert.ok(recovery, "move recovery must be present");
  assert.match(recovery[0], /runSerializedTaskCommand\(/);
  assert.match(recovery[0], /runWithBoardRevisionConflictRetry\(/);
  assert.match(recovery[0], /boardReadArguments\(\)/);
  assert.match(recovery[0], /publishBoardSnapshot\(snapshot\)/);
  assert.match(recovery[0], /"move", message\.taskId/);
  assert.doesNotMatch(recovery[0], /set-status|message\.status|expectedTaskRevision/);
});

test("task create requires a loaded board snapshot and passes its revision to CLI CAS", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const createStart = source.indexOf('if (message.type === "create") {');
  const editStart = source.indexOf('if (message.type === "edit") {', createStart);
  assert.notEqual(createStart, -1, "create message handler must be present");
  assert.notEqual(editStart, -1, "edit message handler must follow create");
  const createBranch = source.slice(createStart, editStart);
  assert.match(createBranch, /if \(!this\.currentView\) \{/);
  assert.match(createBranch, /createWithRevisionRecovery\(message, this\.currentView\.boardRevision\)/);

  const recovery = source.match(/private async createWithRevisionRecovery\([\s\S]+?\n\s*private async mutate/);
  assert.ok(recovery, "create revision recovery must be present");
  assert.match(recovery[0], /runSerializedTaskCommand\(/);
  assert.match(recovery[0], /runWithBoardRevisionConflictRetry\(/);
  assert.match(recovery[0], /boardReadArguments\(\)/);
  assert.match(recovery[0], /publishBoardSnapshot\(snapshot\)/);
  assert.match(
    recovery[0],
    /"--expected-board-revision", String\(expectedBoardRevision\)/);
  assert.match(createBranch, /await this\.panel\.webview\.postMessage\(\{ type: "createResult", ok: true \}\)/);
  assert.match(createBranch, /type: "createResult", ok: false, message: diagnostic/);
  assert.match(createBranch, /this\.lastError = diagnostic/);
});

test("task edit does not send assignment metadata to the CLI", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  const editBranch = source.match(/if \(message\.type === "edit"\) \{([\s\S]+?)\n\s*return;\n\s*\}/);
  assert.ok(editBranch, "edit message handler must be present");
  assert.doesNotMatch(editBranch[1]!, /--assignee|message\.assignee/);
  assert.match(editBranch[1]!, /--deadline|--clear-deadline/);
});

test("global tag actions are serialized through the CLI and never write taskboard files directly", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /message\.type === "manageTags"[\s\S]+manageTags\(\)/);
  assert.match(source, /message\.type === "addTaskTag"[\s\S]+addTaskTag\(message\)/);
  assert.match(source, /"tag", "create"[\s\S]+"--assign-to"/);
  assert.match(source, /"tag", "assign"[\s\S]+"--expected-board-revision"/);
  assert.doesNotMatch(source, /writeFile[^\n]+board\.e2tasks/);
});

test("tag settings validate strict custom hex colors before invoking the CLI", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /normalizeTaskBoardTagColor/);
  assert.match(source, /showInputBox\(\{[\s\S]+value:\s*current[\s\S]+validateInput:[\s\S]+normalizeTaskBoardTagColor/s);
  assert.doesNotMatch(source, /const colors:\s*readonly TaskBoardTagColor\[\]/);
});

test("attachment cover selection uses the CLI and only trusted raster paths become webview URIs", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /message\.type === "setAttachmentPreview"[\s\S]+"attachment", "set-preview"/);
  assert.match(source, /message\.type === "clearAttachmentPreview"[\s\S]+"attachment", "clear-preview"/);
  assert.match(source, /task\.cardPreview/);
  assert.match(source, /const attachmentOwner = task\.taskUid \?\? task\.taskId/);
  assert.match(source, /this\.previewForWebview\(attachmentOwner, task\.cardPreview\)/);
  assert.match(source, /isRasterAttachment\(preview\.mediaType\)/);
  assert.match(source, /isSafeAttachmentPath\(attachmentOwner, preview\.relativePath\)/);
  assert.match(source, /asWebviewUri\(uri\)/);
});

test("overview does not expose a workspace reveal host action", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.doesNotMatch(source, /message\.type === "revealWorkspace"|revealFileInOS/);
});

test("linked and non-raster attached files are resolved inside the workspace and opened in the standard editor", async () => {
  const source = await readFile(path.join(process.cwd(), "src", "extension.ts"), "utf8");
  assert.match(source, /if \(message\.type === "openFile"\) \{\s*await this\.openWorkspaceFile\(message\.path\);\s*return;\s*\}/);
  assert.match(source, /resolveWorkspaceFilePath\(this\.folder\.uri\.fsPath, relativePath\)/);
  assert.match(source, /realpath\(candidatePath\)/);
  assert.match(source, /isPathInsideWorkspace\(realWorkspacePath, realFilePath\)/);
  assert.match(source, /fileType[^\n]+vscode\.FileType\.File/);
  assert.match(source, /vscode\.commands\.executeCommand\("vscode\.open", fileUri, \{[\s\S]+viewColumn: vscode\.ViewColumn\.Active/);
  assert.doesNotMatch(source, /vscode\.workspace\.openTextDocument\(fileUri\)/);
});

test("workspace file resolver rejects traversal and absolute paths", async () => {
  const workspaceModule = await import("../../src/workspace.js") as unknown as Record<string, unknown>;
  assert.equal(typeof workspaceModule.resolveWorkspaceFilePath, "function");
  assert.equal(typeof workspaceModule.isPathInsideWorkspace, "function");
  const resolveFile = workspaceModule.resolveWorkspaceFilePath as (root: string, candidate: string) => string | undefined;
  const isInside = workspaceModule.isPathInsideWorkspace as (root: string, candidate: string) => boolean;
  const root = path.resolve("C:/project");
  assert.equal(resolveFile(root, "docs/readme.md"), path.join(root, "docs", "readme.md"));
  assert.equal(resolveFile(root, "../outside.md"), undefined);
  assert.equal(resolveFile(root, "docs/../outside.md"), undefined);
  assert.equal(resolveFile(root, "C:/outside.md"), undefined);
  assert.equal(isInside(root, path.join(root, "docs", "readme.md")), true);
  assert.equal(isInside(root, path.resolve(root, "..", "outside.md")), false);
});
