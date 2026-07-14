/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { randomBytes } from "node:crypto";
import { realpath } from "node:fs/promises";
import * as vscode from "vscode";
import {
  archiveTaskArguments,
  boardReadArguments,
  InFlightRequestBroker,
  isConfirmedDialogResult,
  isCompactBoardRead,
  isTaskRevisionConflict,
  reopenTaskArguments,
  resolveCliExecutable,
  runHumanAcceptanceWorkflow,
  runHumanDecisionCli,
  runHumanMessageCli,
  runAgentContextCli,
  runAgentMessageCli,
  runTaskCli,
  runWithBoardRevisionConflictRetry,
  runWithTaskRevisionConflictRetry,
  runWithTransientTaskboardLockRetry,
  taskReadArguments,
  taskRevisionFromMutation,
  TrailingRefreshCoordinator,
  unarchiveTaskArguments,
  WorkspaceCommandScheduler
} from "./cli.js";
import { sanitizedAgentDiagnostic, type AgentPresentationEvent } from "./agentChat.js";
import { OpenCodeBackend } from "./opencodeBackend.js";
import {
  buildBoardView,
  buildSidebarRows,
  normalizeTaskBoardTagColor,
  renderTaskSelection,
  TaskNavigationHistory,
  type BoardTask,
  type BoardView,
  type CardPreviewSnapshot,
  type SidebarTaskRow,
  type TaskBoardSnapshot,
  type TaskBoardTagColor,
  type TaskSnapshot,
  type TaskStatus
} from "./model.js";
import { parseWebviewMessage } from "./security.js";
import { buildWebviewHtml } from "./webviewHtml.js";
import { isPathInsideWorkspace, resolveWorkspaceFilePath, selectTaskboardCandidates } from "./workspace.js";
import { createLocalizer } from "./localization.js";

const viewType = "electron2d.taskBoard";
const launcherViewId = "electron2d.taskBoardLauncher";
const openSidebarTaskCommand = "electron2d.openTaskFromSidebar";
const boardReadBroker = new InFlightRequestBroker();
const workspaceCommandScheduler = new WorkspaceCommandScheduler();
const l10n = createLocalizer(vscode.env.language);
let workspaceSelectionInFlight: Promise<vscode.WorkspaceFolder | undefined> | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<unknown> {
  context.subscriptions.push(vscode.commands.registerCommand("electron2d.openTaskBoard", async () => {
    if (!vscode.workspace.isTrusted) {
      await vscode.window.showWarningMessage(l10n.t("extension.trustedRequired"));
      return;
    }

    const folder = await selectWorkspaceFolder(context);
    if (!folder) {
      return;
    }

    await TaskBoardPanel.open(context, folder);
  }));

  const sidebarProvider = new TaskBoardSidebarProvider(context);
  context.subscriptions.push(
    sidebarProvider,
    vscode.commands.registerCommand(
      openSidebarTaskCommand,
      async (folderUri: string, taskId: string) => {
        const folder = vscode.workspace.workspaceFolders?.find(candidate => candidate.uri.toString() === folderUri);
        if (!folder) {
          await vscode.window.showWarningMessage(l10n.t("extension.workspaceClosed", taskId));
          return;
        }

        await TaskBoardPanel.open(context, folder, taskId);
      }));

  const launcherView = vscode.window.createTreeView(launcherViewId, {
    treeDataProvider: sidebarProvider,
    showCollapseAll: false
  });
  const openFromLauncher = (): void => {
    void vscode.commands.executeCommand("electron2d.openTaskBoard");
  };
  context.subscriptions.push(
    launcherView,
    launcherView.onDidChangeVisibility(event => {
      if (event.visible) {
        openFromLauncher();
      }
    }));
  if (launcherView.visible) {
    openFromLauncher();
  }

  await openStartupTaskBoard(context);

  if (context.extensionMode === vscode.ExtensionMode.Test) {
    return {
      invoke: async (message: unknown) => await TaskBoardPanel.invokeForTest(message),
      getState: () => TaskBoardPanel.stateForTest(),
      getLastError: () => TaskBoardPanel.errorForTest(),
      getSidebarRows: () => sidebarProvider.rowsForTest(),
      getSelectedTaskId: () => TaskBoardPanel.selectedTaskForTest(),
      getSelectedTaskRevision: () => TaskBoardPanel.selectedTaskRevisionForTest(),
      getNavigationState: () => TaskBoardPanel.navigationStateForTest(),
      getLocale: () => l10n.locale
    };
  }

  return undefined;
}

class TaskBoardSidebarProvider implements vscode.TreeDataProvider<vscode.TreeItem>, vscode.Disposable {
  private readonly changed = new vscode.EventEmitter<vscode.TreeItem | undefined>();
  private readonly watchers: vscode.FileSystemWatcher[] = [];
  private readonly refreshCoordinator: TrailingRefreshCoordinator;
  private rows: SidebarTaskRow[] = [];
  private selectedFolder: vscode.WorkspaceFolder | undefined;
  private watchedFolder: string | undefined;
  private refreshTimer: NodeJS.Timeout | undefined;

  public constructor(private readonly context: vscode.ExtensionContext) {
    this.refreshCoordinator = new TrailingRefreshCoordinator(async () => this.refreshRows());
  }

  public readonly onDidChangeTreeData = this.changed.event;

  public getTreeItem(element: vscode.TreeItem): vscode.TreeItem {
    return element;
  }

  public async getChildren(element?: vscode.TreeItem): Promise<vscode.TreeItem[]> {
    if (element) {
      return [];
    }

    if (!vscode.workspace.isTrusted) {
      return [this.messageItem(l10n.t("extension.taskListTrusted"), "shield")];
    }

    try {
      const folder = this.selectedFolder ?? await selectWorkspaceFolder(this.context);
      if (!folder) {
        this.rows = [];
        return [this.messageItem(l10n.t("extension.noBoard"), "info")];
      }

      this.selectedFolder = folder;
      this.ensureWatchers(folder);
      await this.refreshCoordinator.request();
      if (this.rows.length === 0) {
        return [this.messageItem(l10n.t("extension.noActiveTasks"), "check")];
      }

      return this.rows.map(row => new SidebarTaskTreeItem(folder, row));
    } catch (error) {
      this.rows = [];
      const message = error instanceof Error ? error.message : String(error);
      return [this.messageItem(l10n.t("extension.taskListUnavailable", message), "error")];
    }
  }

  private async refreshRows(): Promise<void> {
    const folder = this.selectedFolder;
    if (!folder) {
      this.rows = [];
      return;
    }

    const data = await runTaskCliForFolder(folder, boardReadArguments());
    this.rows = buildSidebarRows(asTaskBoardSnapshot(data), l10n);
  }

  public rowsForTest(): readonly SidebarTaskRow[] {
    return this.rows;
  }

  public dispose(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }
    this.changed.dispose();
    for (const watcher of this.watchers.splice(0)) {
      watcher.dispose();
    }
  }

  private ensureWatchers(folder: vscode.WorkspaceFolder): void {
    const folderKey = folder.uri.toString();
    if (this.watchedFolder === folderKey) {
      return;
    }

    for (const watcher of this.watchers.splice(0)) {
      watcher.dispose();
    }
    this.watchedFolder = folderKey;
    for (const pattern of [
      ".taskboard/board.e2tasks",
      ".taskboard/tasks/*.e2task",
      ".taskboard/completed/*.e2task"
    ]) {
      const watcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(folder, pattern));
      watcher.onDidCreate(() => this.scheduleRefresh());
      watcher.onDidChange(() => this.scheduleRefresh());
      watcher.onDidDelete(() => this.scheduleRefresh());
      this.watchers.push(watcher);
    }
  }

  private scheduleRefresh(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }
    this.refreshTimer = setTimeout(() => {
      this.refreshTimer = undefined;
      this.changed.fire(undefined);
    }, 150);
  }

  private messageItem(label: string, icon: string): vscode.TreeItem {
    const item = new vscode.TreeItem(label, vscode.TreeItemCollapsibleState.None);
    item.iconPath = new vscode.ThemeIcon(icon);
    item.tooltip = label;
    return item;
  }
}

class SidebarTaskTreeItem extends vscode.TreeItem {
  public constructor(folder: vscode.WorkspaceFolder, row: SidebarTaskRow) {
    super(row.label, vscode.TreeItemCollapsibleState.None);
    this.id = `${folder.uri.toString()}#${row.taskId}`;
    this.description = row.description;
    this.tooltip = row.tooltip;
    this.iconPath = new vscode.ThemeIcon(statusIcon(row.status));
    this.contextValue = "electron2dTask";
    this.command = {
      command: openSidebarTaskCommand,
      title: l10n.t("extension.openTask"),
      arguments: [folder.uri.toString(), row.taskId]
    };
  }
}

function statusIcon(status: TaskStatus): string {
  switch (status) {
    case "Ready": return "play-circle";
    case "InProgress": return "sync";
    case "Blocked": return "lock";
    case "Review": return "eye";
    case "Done": return "pass-filled";
    case "Cancelled": return "circle-slash";
    default: return "circle-outline";
  }
}

export function deactivate(): void {
  TaskBoardPanel.disposeAll();
}

async function openStartupTaskBoard(context: vscode.ExtensionContext): Promise<void> {
  if (!vscode.workspace.isTrusted) {
    return;
  }

  const folders = vscode.workspace.workspaceFolders ?? [];
  for (const folder of folders) {
    const boardUri = vscode.Uri.joinPath(folder.uri, ".taskboard", "board.e2tasks");
    if (!await pathExists(boardUri)) {
      continue;
    }

    await TaskBoardPanel.open(context, folder);
    return;
  }
}

async function selectWorkspaceFolder(context: vscode.ExtensionContext): Promise<vscode.WorkspaceFolder | undefined> {
  workspaceSelectionInFlight ??= resolveWorkspaceFolder(context);
  try {
    return await workspaceSelectionInFlight;
  } finally {
    workspaceSelectionInFlight = undefined;
  }
}

async function resolveWorkspaceFolder(context: vscode.ExtensionContext): Promise<vscode.WorkspaceFolder | undefined> {
  const folders = vscode.workspace.workspaceFolders ?? [];
  const inspected = await Promise.all(folders.map(async folder => ({
    folder,
    name: folder.name,
    path: folder.uri.fsPath,
    hasBoard: await pathExists(vscode.Uri.joinPath(folder.uri, ".taskboard", "board.e2tasks"))
  })));
  const candidates = selectTaskboardCandidates(inspected).map(candidate =>
    inspected.find(item => item.path === candidate.path)!).filter(item => item !== undefined);

  if (candidates.length === 0) {
    await vscode.window.showWarningMessage(l10n.t("extension.noTaskboard"));
    return undefined;
  }

  if (candidates.length === 1) {
    return candidates[0]!.folder;
  }

  const previous = context.workspaceState.get<string>("taskboard.workspace");
  const picked = await vscode.window.showQuickPick(
    candidates.map(candidate => ({
      label: candidate.name,
      description: candidate.path,
      folder: candidate.folder,
      picked: candidate.path === previous
    })),
    { placeHolder: l10n.t("extension.selectWorkspace") });
  if (picked) {
    await context.workspaceState.update("taskboard.workspace", picked.folder.uri.fsPath);
  }

  return picked?.folder;
}

async function runTaskCliForFolder(folder: vscode.WorkspaceFolder, taskArguments: readonly string[]): Promise<unknown> {
  const configuredPath = vscode.workspace.getConfiguration("electron2d.taskboard", folder.uri).get<string>("cliPath");
  const executable = resolveCliExecutable(configuredPath, process.platform);
  const key = `${folder.uri.toString()}\u0000${executable}`;
  const start = async (): Promise<unknown> => await runSerializedTaskCommand(
    folder,
    executable,
    async () => await runTaskCli(executable, folder.uri.fsPath, taskArguments));
  return isCompactBoardRead(taskArguments)
    ? await boardReadBroker.run(key, start)
    : await start();
}

async function runSerializedTaskCommand<T>(
  folder: vscode.WorkspaceFolder,
  executable: string,
  start: () => Promise<T>,
  retryWholeCommand = true): Promise<T> {
  const key = `${folder.uri.toString()}\u0000${executable}`;
  return await workspaceCommandScheduler.run(key, async () => retryWholeCommand
    ? await retryTaskboardLock(start)
    : await start());
}

async function retryTaskboardLock<T>(start: () => Promise<T>): Promise<T> {
  return await runWithTransientTaskboardLockRetry(
    start,
    async failedAttempt => {
      const delayMs = Math.min(75 * (2 ** (failedAttempt - 1)), 600);
      await new Promise(resolve => setTimeout(resolve, delayMs));
    },
    5);
}

async function pathExists(uri: vscode.Uri): Promise<boolean> {
  try {
    await vscode.workspace.fs.stat(uri);
    return true;
  } catch {
    return false;
  }
}

class TaskBoardPanel implements vscode.Disposable {
  private static readonly panels = new Map<string, TaskBoardPanel>();
  private readonly disposables: vscode.Disposable[] = [];
  private refreshTimer: NodeJS.Timeout | undefined;
  private disposed = false;
  private currentView: BoardView | undefined;
  private lastError: string | undefined;
  private selectedTaskId: string | undefined;
  private readonly taskDetailsCache = new Map<string, BoardTask>();
  private readonly taskNavigation = new TaskNavigationHistory();
  private selectionToken = 0;
  private readonly refreshCoordinator: TrailingRefreshCoordinator;
  private readonly agentBackend: OpenCodeBackend;
  private readonly activeAgentRuns = new Map<string, { readonly taskUid: string; readonly runId: string }>();
  private readonly cancelledAgentRuns = new Set<string>();

  private constructor(
    private readonly context: vscode.ExtensionContext,
    private readonly folder: vscode.WorkspaceFolder,
    private readonly panel: vscode.WebviewPanel) {
    this.refreshCoordinator = new TrailingRefreshCoordinator(async () => this.refreshOnce());
    this.agentBackend = new OpenCodeBackend(folder.uri.fsPath, folder.uri.toString());
    const distRoot = vscode.Uri.joinPath(context.extensionUri, "dist");
    const nonce = randomBytes(18).toString("base64url");
    const resourceQuery = `panel=${nonce}`;
    panel.webview.html = buildWebviewHtml({
      cspSource: panel.webview.cspSource,
      nonce,
      scriptUri: panel.webview.asWebviewUri(
        vscode.Uri.joinPath(distRoot, "webview.js").with({ query: resourceQuery })).toString(),
      styleUri: panel.webview.asWebviewUri(
        vscode.Uri.joinPath(distRoot, "webview.css").with({ query: resourceQuery })).toString(),
      locale: l10n.locale
    });
    this.disposables.push(
      panel.onDidDispose(() => this.dispose()),
      panel.webview.onDidReceiveMessage(message => this.handleMessage(message)),
      ...this.createWatchers());
  }

  public static async open(context: vscode.ExtensionContext, folder: vscode.WorkspaceFolder, taskId?: string): Promise<void> {
    const key = folder.uri.toString();
    const existing = this.panels.get(key);
    if (existing) {
      existing.panel.reveal(vscode.ViewColumn.Active, false);
      if (taskId) {
        void existing.showTask(taskId, "direct");
      }
      return;
    }

    const distRoot = vscode.Uri.joinPath(context.extensionUri, "dist");
    const panel = vscode.window.createWebviewPanel(
      viewType,
      l10n.t("extension.panelTitle"),
      vscode.ViewColumn.Active,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [distRoot, vscode.Uri.joinPath(folder.uri, ".taskboard", "attachments")]
      });
    const created = new TaskBoardPanel(context, folder, panel);
    this.panels.set(key, created);
    await created.refresh();
    if (taskId) {
      await created.showTask(taskId, "direct");
    }
  }

  public static disposeAll(): void {
    for (const panel of [...this.panels.values()]) {
      panel.dispose();
    }
  }

  public static async invokeForTest(message: unknown): Promise<void> {
    const panel = this.panels.values().next().value as TaskBoardPanel | undefined;
    if (!panel) {
      throw new Error(l10n.t("extension.panelNotOpen"));
    }

    await panel.handleMessage(message);
  }

  public static stateForTest(): BoardView | undefined {
    return (this.panels.values().next().value as TaskBoardPanel | undefined)?.currentView;
  }

  public static errorForTest(): string | undefined {
    return (this.panels.values().next().value as TaskBoardPanel | undefined)?.lastError;
  }

  public static selectedTaskForTest(): string | undefined {
    return (this.panels.values().next().value as TaskBoardPanel | undefined)?.selectedTaskId;
  }

  public static selectedTaskRevisionForTest(): number | undefined {
    const panel = this.panels.values().next().value as TaskBoardPanel | undefined;
    return panel?.selectedTaskId ? panel.taskDetailsCache.get(panel.selectedTaskId)?.revision : undefined;
  }

  public static navigationStateForTest(): { currentTaskId: string | undefined; canGoBack: boolean } {
    const navigation = (this.panels.values().next().value as TaskBoardPanel | undefined)?.taskNavigation;
    return {
      currentTaskId: navigation?.currentTaskId,
      canGoBack: navigation?.canGoBack ?? false
    };
  }

  public dispose(): void {
    if (this.disposed) {
      return;
    }

    this.disposed = true;
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }

    TaskBoardPanel.panels.delete(this.folder.uri.toString());
    this.agentBackend.dispose();
    for (const disposable of this.disposables.splice(0)) {
      disposable.dispose();
    }
  }

  private createWatchers(): vscode.FileSystemWatcher[] {
    return [
      ".taskboard/board.e2tasks",
      ".taskboard/tasks/*.e2task",
      ".taskboard/completed/*.e2task"
    ].map(pattern => {
      const watcher = vscode.workspace.createFileSystemWatcher(new vscode.RelativePattern(this.folder, pattern));
      watcher.onDidCreate(() => this.scheduleRefresh());
      watcher.onDidChange(() => this.scheduleRefresh());
      watcher.onDidDelete(() => this.scheduleRefresh());
      return watcher;
    });
  }

  private scheduleRefresh(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }

    this.refreshTimer = setTimeout(() => {
      this.refreshTimer = undefined;
      void this.refresh();
    }, 150);
  }

  private async handleMessage(rawMessage: unknown): Promise<void> {
    const message = parseWebviewMessage(rawMessage);
    if (!message) {
      await this.postError(l10n.t("extension.invalidMessage"));
      return;
    }

    if (message.type === "refresh") {
      await this.refresh();
      return;
    }

    if (message.type === "openFile") {
      await this.openWorkspaceFile(message.path);
      return;
    }

    if (message.type === "manageTags") {
      await this.manageTags();
      return;
    }

    if (message.type === "addTaskTag") {
      await this.addTaskTag(message);
      return;
    }

    if (message.type === "openTask") {
      await this.showTask(message.taskId, message.navigation);
      return;
    }

    if (message.type === "navigateBack") {
      await this.navigateBack();
      return;
    }

    if (message.type === "closeTaskDetails") {
      await this.closeTaskDetails();
      return;
    }

    if (message.type === "comment") {
      await this.mutateTaskAndRefreshDetails(message.taskId, [
        "comment", "add", message.taskId,
        "--text", message.text,
        "--expected-revision", String(message.expectedTaskRevision)
      ]);
      return;
    }

    if (message.type === "sendAgentMessage") {
      void this.sendAgentMessage(message.taskId, message.text, message.expectedTaskRevision);
      return;
    }

    if (message.type === "cancelAgentRun") {
      await this.cancelAgentRun(message.taskId);
      return;
    }

    if (message.type === "respondAgentPermission") {
      await this.respondAgentPermission(message.taskId, message.permissionId, message.response);
      return;
    }

    if (message.type === "create") {
      if (!this.currentView) {
        const diagnostic = l10n.t("extension.invalidMessage");
        this.lastError = diagnostic;
        await this.panel.webview.postMessage({ type: "createResult", ok: false, message: diagnostic });
        return;
      }

      try {
        await this.createWithRevisionRecovery(message, this.currentView.boardRevision);
        await this.panel.webview.postMessage({ type: "createResult", ok: true });
      } catch (error) {
        const diagnostic = error instanceof Error ? error.message : String(error);
        this.lastError = diagnostic;
        void vscode.window.showErrorMessage(diagnostic);
        await this.panel.webview.postMessage({ type: "createResult", ok: false, message: diagnostic });
      }
      return;
    }

    if (message.type === "edit") {
      await this.mutateTaskAndRefreshDetails(message.taskId, [
        "update", message.taskId,
        "--title", message.title,
        "--description", message.description,
        "--priority", message.priority,
        ...(message.deadline ? ["--deadline", message.deadline] : []),
        ...(message.deadline === null ? ["--clear-deadline", "true"] : []),
        "--expected-revision", String(message.expectedTaskRevision)
      ]);
      return;
    }

    if (message.type === "move") {
      try {
        await this.moveWithRevisionRecovery(message);
      } catch (error) {
        await this.postError(error instanceof Error ? error.message : String(error));
      }
      return;
    }

    if (message.type === "attach") {
      const selected = await vscode.window.showOpenDialog({
        canSelectFiles: true,
        canSelectFolders: false,
        canSelectMany: false,
        title: l10n.t("extension.attachTitle", message.taskId)
      });
      if (selected?.[0]) {
        await this.mutateTaskAndRefreshDetails(message.taskId, [
          "attachment", "add", message.taskId,
          "--file", selected[0].fsPath,
          "--expected-revision", String(message.expectedTaskRevision)
        ]);
      }
      return;
    }

    if (message.type === "reopen") {
      await this.mutateTaskAndRefreshDetails(message.taskId, reopenTaskArguments(
        message.taskId,
        message.expectedTaskRevision,
        message.reason));
      return;
    }

    if (message.type === "archive") {
      const label = l10n.t("task.archive");
      const confirmed = await vscode.window.showWarningMessage(
        l10n.t("extension.confirmDecision", label, message.taskId),
        { modal: true },
        label);
      if (!isConfirmedDialogResult(confirmed, label)) { return; }

      await this.mutateTaskAndRefreshDetails(message.taskId, archiveTaskArguments(
        message.taskId,
        message.expectedTaskRevision,
        message.expectedBoardRevision));
      return;
    }

    if (message.type === "unarchive") {
      await this.mutateTaskAndRefreshDetails(message.taskId, unarchiveTaskArguments(
        message.taskId,
        message.expectedTaskRevision,
        message.expectedBoardRevision));
      return;
    }

    if (message.type === "removeAttachment") {
      await this.mutateTaskAndRefreshDetails(message.taskId, [
        "attachment", "remove", message.taskId,
        "--attachment", message.attachmentId,
        "--expected-revision", String(message.expectedTaskRevision)
      ]);
      return;
    }

    if (message.type === "setAttachmentPreview") {
      await this.mutateTaskAndRefreshDetails(message.taskId, [
        "attachment", "set-preview", message.taskId,
        "--attachment", message.attachmentId,
        "--expected-revision", String(message.expectedTaskRevision)
      ]);
      return;
    }

    if (message.type === "clearAttachmentPreview") {
      await this.mutateTaskAndRefreshDetails(message.taskId, [
        "attachment", "clear-preview", message.taskId,
        "--expected-revision", String(message.expectedTaskRevision)
      ]);
      return;
    }

    if (message.type === "submit") {
      await this.mutateTaskAndRefreshDetails(message.taskId, [
        "submit", message.taskId,
        "--reason", message.reason,
        "--expected-revision", String(message.expectedTaskRevision)
      ]);
      return;
    }

    if (message.type === "accept") {
      const label = l10n.t("extension.acceptTask");
      const confirmed = await vscode.window.showWarningMessage(
        l10n.t("extension.confirmDecision", label, message.taskId),
        { modal: true, detail: message.reason },
        label);
      if (confirmed !== label) {
        return;
      }

      try {
        const task = this.taskDetailsCache.get(message.taskId);
        if (!task || task.status !== "Review") {
          throw new Error(l10n.t("task.acceptUnavailable"));
        }
        const configuredPath = vscode.workspace.getConfiguration("electron2d.taskboard", this.folder.uri)
          .get<string>("cliPath");
        const executable = resolveCliExecutable(configuredPath, process.platform);
        await runSerializedTaskCommand(
          this.folder,
          executable,
          async () => await runHumanAcceptanceWorkflow(
            message.taskId,
            task.acceptanceState,
            message.expectedTaskRevision,
            async revision => await retryTaskboardLock(async () => await runTaskCli(
              executable,
              this.folder.uri.fsPath,
              ["submit", message.taskId, "--reason", message.reason, "--expected-revision", String(revision)])),
            async revision => await retryTaskboardLock(async () => await runHumanDecisionCli(
              executable,
              this.folder.uri.fsPath,
              message.taskId,
              revision,
              "accept",
              message.reason))),
          false);
        await this.refresh();
      } catch (error) {
        await this.handleTaskMutationError(message.taskId, error);
      }
      return;
    }

    if (message.type === "requestChanges") {
      const label = l10n.t("extension.requestChanges");
      const confirmed = await vscode.window.showWarningMessage(
        l10n.t("extension.confirmDecision", label, message.taskId),
        { modal: true, detail: message.reason },
        label);
      if (confirmed !== label) {
        return;
      }

      try {
        const configuredPath = vscode.workspace.getConfiguration("electron2d.taskboard", this.folder.uri)
          .get<string>("cliPath");
        const executable = resolveCliExecutable(configuredPath, process.platform);
        await runSerializedTaskCommand(
          this.folder,
          executable,
          async () => await runHumanDecisionCli(
            executable,
            this.folder.uri.fsPath,
            message.taskId,
            message.expectedTaskRevision,
            "request-changes",
            message.reason));
        await this.refresh();
      } catch (error) {
        await this.handleTaskMutationError(message.taskId, error);
      }
    }
  }

  private async createWithRevisionRecovery(
    message: {
      readonly title: string;
      readonly description: string;
      readonly priority: string;
      readonly deadline?: string;
    },
    initialBoardRevision: number): Promise<void> {
    const configuredPath = vscode.workspace.getConfiguration("electron2d.taskboard", this.folder.uri)
      .get<string>("cliPath");
    const executable = resolveCliExecutable(configuredPath, process.platform);
    await runSerializedTaskCommand(
      this.folder,
      executable,
      async () => await runWithBoardRevisionConflictRetry(
        initialBoardRevision,
        async expectedBoardRevision => await runTaskCli(
          executable,
          this.folder.uri.fsPath,
          [
            "create",
            "--title", message.title,
            "--description", message.description,
            "--priority", message.priority,
            ...(message.deadline ? ["--deadline", message.deadline] : []),
            "--expected-board-revision", String(expectedBoardRevision)
          ]),
        async () => {
          const data = await runTaskCli(
            executable,
            this.folder.uri.fsPath,
            boardReadArguments());
          const snapshot = asTaskBoardSnapshot(data);
          await this.publishBoardSnapshot(snapshot);
          return snapshot.board.revision;
        }));
    await this.refresh();
  }

  private async mutate(argumentsWithoutEnvelope: readonly string[]): Promise<void> {
    try {
      await this.runCli(argumentsWithoutEnvelope);
      await this.refresh();
    } catch (error) {
      await this.postError(error instanceof Error ? error.message : String(error));
    }
  }

  private async handleTaskMutationError(taskId: string, error: unknown): Promise<void> {
    const diagnostic = error instanceof Error ? error.message : String(error);
    if (!isTaskRevisionConflict(error)) {
      await this.postError(diagnostic);
      return;
    }

    this.taskDetailsCache.delete(taskId);
    await this.refresh();
    if (this.taskNavigation.currentTaskId === taskId) {
      await this.showTask(taskId, "refresh");
    }
    await this.postError(l10n.t("errors.taskRevisionConflictRefreshed", diagnostic));
  }

  private async mutateTaskAndRefreshDetails(
    taskId: string,
    argumentsWithoutEnvelope: readonly string[]): Promise<void> {
    try {
      await this.runCli(argumentsWithoutEnvelope);
      this.taskDetailsCache.delete(taskId);
      await this.refresh();
      if (this.taskNavigation.currentTaskId === taskId) {
        await this.showTask(taskId, "refresh");
      }
    } catch (error) {
      await this.postError(error instanceof Error ? error.message : String(error));
    }
  }

  private async manageTags(): Promise<void> {
    if (!this.currentView) {
      return;
    }

    const createId = "__create__";
    const picked = await vscode.window.showQuickPick([
      { label: `$(add) ${l10n.t("tags.create")}`, id: createId },
      ...this.currentView.tags.map(tag => ({ label: tag.name, description: tag.color, id: tag.tagId }))
    ], { placeHolder: l10n.t("board.tagSettings") });
    if (!picked) {
      return;
    }

    if (picked.id === createId) {
      await this.createGlobalTag(this.currentView.boardRevision);
      return;
    }

    const tag = this.currentView.tags.find(candidate => candidate.tagId === picked.id);
    if (!tag) {
      return;
    }

    const action = await vscode.window.showQuickPick([
      { label: `$(edit) ${l10n.t("tags.edit")}`, id: "edit" },
      { label: `$(trash) ${l10n.t("tags.delete")}`, id: "delete" }
    ], { placeHolder: l10n.t("tags.selectAction", tag.name) });
    if (action?.id === "edit") {
      const name = await vscode.window.showInputBox({ prompt: l10n.t("tags.namePrompt"), value: tag.name });
      if (!name?.trim()) {
        return;
      }
      const color = await this.pickTagColor(tag.color);
      if (!color) {
        return;
      }
      await this.mutate([
        "tag", "update", tag.tagId,
        "--name", name.trim(),
        "--color", color,
        "--expected-board-revision", String(this.currentView.boardRevision)
      ]);
    } else if (action?.id === "delete") {
      const confirmed = await vscode.window.showWarningMessage(
        l10n.t("tags.delete"),
        { modal: true, detail: tag.name },
        l10n.t("tags.delete"));
      if (confirmed === l10n.t("tags.delete")) {
        await this.mutate([
          "tag", "delete", tag.tagId,
          "--expected-board-revision", String(this.currentView.boardRevision)
        ]);
      }
    }
  }

  private async addTaskTag(message: {
    readonly taskId: string;
    readonly expectedTaskRevision: number;
    readonly expectedBoardRevision: number;
  }): Promise<void> {
    const task = this.currentView?.columns.flatMap(column => column.tasks)
      .find(candidate => candidate.taskId === message.taskId);
    if (!task || !this.currentView) {
      return;
    }

    const createId = "__create__";
    const available = this.currentView.tags.filter(tag => !task.labels.includes(tag.tagId));
    const picked = await vscode.window.showQuickPick([
      { label: `$(add) ${l10n.t("tags.create")}`, id: createId },
      ...available.map(tag => ({ label: tag.name, description: tag.color, id: tag.tagId }))
    ], { placeHolder: available.length > 0 ? l10n.t("tags.selectForTask") : l10n.t("tags.noneAvailable") });
    if (!picked) {
      return;
    }

    if (picked.id === createId) {
      await this.createGlobalTag(message.expectedBoardRevision, message.taskId, message.expectedTaskRevision);
    } else {
      await this.mutate([
        "tag", "assign", message.taskId,
        "--tag", picked.id,
        "--expected-task-revision", String(message.expectedTaskRevision),
        "--expected-board-revision", String(message.expectedBoardRevision)
      ]);
    }

    await this.showTask(message.taskId, "internal");
  }

  private async createGlobalTag(
    expectedBoardRevision: number,
    assignToTaskId?: string,
    expectedTaskRevision?: number): Promise<void> {
    const name = await vscode.window.showInputBox({ prompt: l10n.t("tags.namePrompt") });
    if (!name?.trim()) {
      return;
    }

    const color = await this.pickTagColor("Gray");
    if (!color) {
      return;
    }

    await this.mutate([
      "tag", "create",
      "--name", name.trim(),
      "--color", color,
      ...(assignToTaskId && expectedTaskRevision
        ? ["--assign-to", assignToTaskId, "--expected-task-revision", String(expectedTaskRevision)]
        : []),
      "--expected-board-revision", String(expectedBoardRevision)
    ]);
  }

  private async pickTagColor(current: TaskBoardTagColor): Promise<TaskBoardTagColor | undefined> {
    const value = await vscode.window.showInputBox({
      prompt: l10n.t("tags.colorPrompt"),
      placeHolder: "#RRGGBB",
      value: current,
      validateInput: candidate => normalizeTaskBoardTagColor(candidate) ? undefined : l10n.t("tags.colorInvalid")
    });
    return value === undefined ? undefined : normalizeTaskBoardTagColor(value);
  }

  private async refresh(): Promise<void> {
    await this.refreshCoordinator.request();
  }

  private async refreshOnce(): Promise<void> {
    try {
      const data = await this.runCli(boardReadArguments());
      await this.publishBoardSnapshot(asTaskBoardSnapshot(data));
    } catch (error) {
      await this.postError(error instanceof Error ? error.message : String(error));
    }
  }

  private async moveWithRevisionRecovery(message: {
    readonly taskId: string;
    readonly groupId: string | null;
    readonly rank: string;
    readonly expectedBoardRevision: number;
  }): Promise<void> {
    const configuredPath = vscode.workspace.getConfiguration("electron2d.taskboard", this.folder.uri)
      .get<string>("cliPath");
    const executable = resolveCliExecutable(configuredPath, process.platform);
    const initialTask = this.currentView?.columns.flatMap(column => column.tasks)
      .find(task => task.taskId === message.taskId);
    const initialBoardStatus = initialTask?.boardStatus ?? initialTask?.status;
    await runSerializedTaskCommand(
      this.folder,
      executable,
      async () => await runWithBoardRevisionConflictRetry(
        message.expectedBoardRevision,
        async expectedBoardRevision => await runTaskCli(
          executable,
          this.folder.uri.fsPath,
          [
            "move", message.taskId,
            ...(message.groupId ? ["--group", message.groupId] : []),
            "--rank", message.rank,
            "--expected-board-revision", String(expectedBoardRevision)
          ]),
        async () => {
          const data = await runTaskCli(
            executable,
            this.folder.uri.fsPath,
            boardReadArguments());
          const snapshot = asTaskBoardSnapshot(data);
          await this.publishBoardSnapshot(snapshot);
          return this.isMoveApplicable(message.taskId, message.groupId, initialBoardStatus)
            ? snapshot.board.revision
            : undefined;
        },
        3,
        l10n.t("errors.moveNotApplicable")));
    await this.refresh();
  }

  private isMoveApplicable(
    taskId: string,
    groupId: string | null,
    initialBoardStatus: TaskStatus | undefined): boolean {
    const task = this.currentView?.columns.flatMap(column => column.tasks)
      .find(candidate => candidate.taskId === taskId);
    const currentBoardStatus = task?.boardStatus ?? task?.status;
    return task !== undefined &&
      currentBoardStatus === initialBoardStatus &&
      (groupId === null || this.currentView?.groups.some(group => group.groupId === groupId) === true);
  }

  private async publishBoardSnapshot(snapshot: TaskBoardSnapshot): Promise<void> {
    const safeSnapshot: TaskBoardSnapshot = {
      ...snapshot,
      tasks: snapshot.tasks.map(task => this.taskForWebview(task))
    };
    const selectedTaskId = this.taskNavigation.currentTaskId;
    const cachedTask = selectedTaskId ? this.taskDetailsCache.get(selectedTaskId) : undefined;
    const selectedCard = selectedTaskId
      ? safeSnapshot.tasks.find(task => task.taskId === selectedTaskId)
      : undefined;
    const refreshSelectedDetails = cachedTask !== undefined &&
      selectedCard !== undefined &&
      cachedTask.revision !== selectedCard.revision;
    this.currentView = buildBoardView(safeSnapshot);
    this.lastError = undefined;
    await this.panel.webview.postMessage({
      type: "state",
      project: this.folder.name,
      view: this.currentView
    });
    if (refreshSelectedDetails && selectedTaskId !== undefined && this.taskNavigation.currentTaskId === selectedTaskId) {
      this.taskDetailsCache.delete(selectedTaskId);
      await this.showTask(selectedTaskId, "refresh");
    }
  }

  private async showTask(taskId: string, navigation: "direct" | "internal" | "back" | "refresh"): Promise<void> {
    const card = this.currentView?.columns.flatMap(column => column.tasks)
      .find(candidate => candidate.taskId === taskId);
    if (!card) {
      await this.closeTaskDetails();
      return;
    }

    if (navigation === "direct") {
      this.taskNavigation.openDirect(taskId);
    } else if (navigation === "internal") {
      this.taskNavigation.openInternal(taskId);
    }
    this.selectedTaskId = this.taskNavigation.currentTaskId;
    const selectionToken = ++this.selectionToken;
    const cached = this.taskDetailsCache.get(taskId);
    try {
      await renderTaskSelection<BoardTask>({
        compact: card,
        cached,
        render: async (task, phase) => {
          await this.panel.webview.postMessage({
            type: "task",
            task: this.taskForWebview(task),
            loading: phase === "loading",
            canGoBack: this.taskNavigation.canGoBack
          });
        },
        load: async () => {
          const details = asTaskGetPayload(await this.runCli(taskReadArguments(taskId))).task;
          return { ...details, groupId: card.groupId, rank: card.rank };
        },
        isCurrent: () => this.taskNavigation.currentTaskId === taskId && this.selectionToken === selectionToken,
        remember: task => this.taskDetailsCache.set(taskId, task)
      });
    } catch (error) {
      if (this.taskNavigation.currentTaskId === taskId && this.selectionToken === selectionToken) {
        const message = error instanceof Error ? error.message : String(error);
        if (cached) {
          await this.postError(message);
        } else {
          this.lastError = message;
          await this.panel.webview.postMessage({
            type: "taskError",
            task: this.taskForWebview(card),
            message,
            canGoBack: this.taskNavigation.canGoBack
          });
        }
      }
    }
  }

  private async navigateBack(): Promise<void> {
    const previousTaskId = this.taskNavigation.back();
    if (previousTaskId) {
      await this.showTask(previousTaskId, "back");
    }
  }

  private async closeTaskDetails(): Promise<void> {
    this.selectionToken++;
    this.taskNavigation.clear();
    this.selectedTaskId = undefined;
    await this.panel.webview.postMessage({ type: "task", task: null, canGoBack: false });
  }

  private async openWorkspaceFile(relativePath: string): Promise<void> {
    try {
      const candidatePath = resolveWorkspaceFilePath(this.folder.uri.fsPath, relativePath);
      if (!candidatePath) {
        throw new Error(l10n.t("errors.unsafePath"));
      }

      const [realWorkspacePath, realFilePath] = await Promise.all([
        realpath(this.folder.uri.fsPath),
        realpath(candidatePath)
      ]);
      if (!isPathInsideWorkspace(realWorkspacePath, realFilePath)) {
        throw new Error(l10n.t("errors.outsideProject"));
      }

      const fileUri = vscode.Uri.file(realFilePath);
      const fileType = (await vscode.workspace.fs.stat(fileUri)).type;
      if ((fileType & vscode.FileType.File) !== vscode.FileType.File) {
        throw new Error(l10n.t("errors.notFile"));
      }

      await vscode.commands.executeCommand("vscode.open", fileUri, {
        viewColumn: vscode.ViewColumn.Active,
        preview: true,
        preserveFocus: false
      });
    } catch (error) {
      const reason = error instanceof Error ? error.message : String(error);
      await this.postError(l10n.t("errors.openFile", relativePath, reason));
    }
  }

  private async sendAgentMessage(taskId: string, text: string, expectedTaskRevision: number): Promise<void> {
    if (this.activeAgentRuns.has(taskId)) {
      await this.postError(l10n.t("chat.alreadyRunning"));
      return;
    }

    const task = this.taskDetailsCache.get(taskId);
    if (!task?.taskUid || task.revision !== expectedTaskRevision) {
      await this.postError(l10n.t("chat.staleTask"));
      await this.showTask(taskId, "refresh");
      return;
    }
    const taskUid = task.taskUid;

    const configuredPath = vscode.workspace.getConfiguration("electron2d.taskboard", this.folder.uri)
      .get<string>("cliPath");
    const executable = resolveCliExecutable(configuredPath, process.platform);
    const runId = `run-${randomBytes(16).toString("hex")}`;
    this.activeAgentRuns.set(taskId, { taskUid, runId });
    try {
      await this.postAgentEvent(taskId, {
        kind: "status",
        runId,
        status: "connecting",
        text: l10n.t("chat.connecting")
      });
      const prepared = await runSerializedTaskCommand(
        this.folder,
        executable,
        async () => {
          const human = await runWithTaskRevisionConflictRetry(
            expectedTaskRevision,
            taskUid,
            async revision => await retryTaskboardLock(async () => await runHumanMessageCli(
              executable,
              this.folder.uri.fsPath,
              taskId,
              revision,
              text)),
            async () => {
              const refreshed = asTaskGetPayload(await runTaskCli(
                executable,
                this.folder.uri.fsPath,
                taskReadArguments(taskId)));
              return refreshed.task.taskUid
                ? { taskUid: refreshed.task.taskUid, revision: refreshed.task.revision }
                : undefined;
            });
          const humanRevision = taskRevisionFromMutation(human, taskId);
          const checkpoint = await retryTaskboardLock(async () => await runAgentContextCli(
            executable,
            this.folder.uri.fsPath,
            taskId,
            humanRevision,
            runId));
          const checkpointRevision = taskRevisionFromMutation(checkpoint, taskId);
          const payload = asTaskGetPayload(await runTaskCli(
            executable,
            this.folder.uri.fsPath,
            taskReadArguments(taskId)));
          if (payload.task.revision !== checkpointRevision || payload.task.taskUid !== taskUid) {
            throw new Error(l10n.t("chat.contextChanged"));
          }
          return { payload, checkpointRevision };
        },
        false);

      if (this.taskNavigation.currentTaskId === taskId) {
        await this.showTask(taskId, "refresh");
      }
      const context = JSON.stringify({
        protocol: "Electron2D.TaskAgentContext/1",
        taskId,
        taskUid,
        taskRevision: prepared.checkpointRevision,
        agentRunId: runId,
        agentContext: prepared.payload.agentContext
      });
      const finalText = await this.agentBackend.run({
        taskId,
        taskUid,
        runId,
        context,
        prompt: text,
        emit: event => void this.postAgentEvent(taskId, event)
      });

      await runSerializedTaskCommand(
        this.folder,
        executable,
        async () => {
          const current = asTaskGetPayload(await runTaskCli(
            executable,
            this.folder.uri.fsPath,
            taskReadArguments(taskId)));
          if (current.task.revision !== prepared.checkpointRevision) {
            throw new Error(l10n.t("chat.contextChanged"));
          }
          await retryTaskboardLock(async () => await runAgentMessageCli(
            executable,
            this.folder.uri.fsPath,
            taskId,
            prepared.checkpointRevision,
            finalText,
            runId));
        },
        false);
      await this.postAgentEvent(taskId, { kind: "final", runId, text: finalText });
      await this.refresh();
      if (this.taskNavigation.currentTaskId === taskId) {
        await this.showTask(taskId, "refresh");
      }
    } catch (error) {
      if (this.cancelledAgentRuns.has(runId)) {
        return;
      }
      const diagnostic = sanitizedAgentDiagnostic(error instanceof Error ? error.message : String(error));
      await this.postAgentEvent(taskId, { kind: "error", runId, text: diagnostic });
      await this.postError(diagnostic);
    } finally {
      this.activeAgentRuns.delete(taskId);
      this.cancelledAgentRuns.delete(runId);
    }
  }

  private async cancelAgentRun(taskId: string): Promise<void> {
    const active = this.activeAgentRuns.get(taskId);
    if (!active) {
      return;
    }
    try {
      this.cancelledAgentRuns.add(active.runId);
      await this.agentBackend.cancel(active.taskUid);
      await this.postAgentEvent(taskId, {
        kind: "status",
        runId: active.runId,
        status: "cancelled",
        text: l10n.t("chat.cancelled")
      });
    } catch (error) {
      await this.postError(sanitizedAgentDiagnostic(error instanceof Error ? error.message : String(error)));
    }
  }

  private async respondAgentPermission(
    taskId: string,
    permissionId: string,
    response: "once" | "session" | "reject"): Promise<void> {
    const active = this.activeAgentRuns.get(taskId);
    if (!active) {
      await this.postError(l10n.t("chat.noActiveRun"));
      return;
    }
    try {
      await this.agentBackend.respondPermission(active.taskUid, permissionId, response);
    } catch (error) {
      await this.postError(sanitizedAgentDiagnostic(error instanceof Error ? error.message : String(error)));
    }
  }

  private async postAgentEvent(taskId: string, event: AgentPresentationEvent): Promise<void> {
    await this.panel.webview.postMessage({ type: "agentEvent", taskId, event });
  }

  private async runCli(taskArguments: readonly string[]): Promise<unknown> {
    return await runTaskCliForFolder(this.folder, taskArguments);
  }

  private taskForWebview(task: TaskSnapshot): TaskSnapshot {
    const attachmentOwner = task.taskUid ?? task.taskId;
    const cardPreview = this.previewForWebview(attachmentOwner, task.cardPreview);
    const { cardPreview: _untrustedCardPreview, attachments, ...taskWithoutResources } = task;
    return {
      ...taskWithoutResources,
      ...(cardPreview ? { cardPreview } : {}),
      ...(attachments ? { attachments: attachments.map(attachment => {
        if (!isRasterAttachment(attachment.mediaType) || !isSafeAttachmentPath(attachmentOwner, attachment.relativePath)) {
          return attachment;
        }

        const uri = vscode.Uri.joinPath(this.folder.uri, ...attachment.relativePath.split("/"));
        return { ...attachment, previewUri: this.panel.webview.asWebviewUri(uri).toString() };
      }) } : {})
    };
  }

  private previewForWebview(attachmentOwner: string, preview: CardPreviewSnapshot | undefined): CardPreviewSnapshot | undefined {
    if (!preview || !isRasterAttachment(preview.mediaType) || !isSafeAttachmentPath(attachmentOwner, preview.relativePath)) {
      return undefined;
    }

    const uri = vscode.Uri.joinPath(this.folder.uri, ...preview.relativePath.split("/"));
    return { ...preview, previewUri: this.panel.webview.asWebviewUri(uri).toString() };
  }

  private async postError(message: string): Promise<void> {
    this.lastError = message;
    void vscode.window.showErrorMessage(message);
  }
}

function isRasterAttachment(mediaType: string): boolean {
  return new Set(["image/png", "image/jpeg", "image/gif", "image/webp", "image/bmp"]).has(mediaType.toLowerCase());
}

function isSafeAttachmentPath(taskId: string, relativePath: string): boolean {
  const prefix = `.taskboard/attachments/${taskId}/`;
  return relativePath.startsWith(prefix) &&
    relativePath.split("/").every(segment => segment.length > 0 && segment !== "." && segment !== "..");
}

function asTaskBoardSnapshot(value: unknown): TaskBoardSnapshot {
  if (typeof value !== "object" || value === null || !("board" in value) || !("tasks" in value) || !Array.isArray(value.tasks)) {
    throw new Error("e2d returned an invalid tasks.board payload.");
  }

  return value as TaskBoardSnapshot;
}

function asTaskGetSnapshot(value: unknown): TaskSnapshot {
  if (typeof value !== "object" || value === null || !("task" in value) ||
      typeof value.task !== "object" || value.task === null || !("taskId" in value.task)) {
    throw new Error("e2d returned an invalid tasks.get payload.");
  }

  return value.task as TaskSnapshot;
}

interface TaskGetPayload {
  readonly task: TaskSnapshot;
  readonly agentContext: unknown;
}

function asTaskGetPayload(value: unknown): TaskGetPayload {
  if (typeof value !== "object" || value === null || !("task" in value) || !("agentContext" in value) ||
      typeof value.task !== "object" || value.task === null || !("taskId" in value.task)) {
    throw new Error("e2d returned an invalid tasks.get context payload.");
  }
  return value as TaskGetPayload;
}
