/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { createLocalizer, type Localizer } from "./localization.js";

export const taskStatuses = [
  "Ready",
  "InProgress",
  "Blocked",
  "Review",
  "Done",
  "Cancelled"
] as const;

export type TaskStatus = typeof taskStatuses[number];

export class TaskNavigationHistory {
  private readonly previousTaskIds: string[] = [];
  private current: string | undefined;

  public get currentTaskId(): string | undefined {
    return this.current;
  }

  public get canGoBack(): boolean {
    return this.previousTaskIds.length > 0;
  }

  public openDirect(taskId: string): void {
    this.previousTaskIds.length = 0;
    this.current = taskId;
  }

  public openInternal(taskId: string): void {
    if (taskId === this.current) {
      return;
    }

    if (this.current) {
      this.previousTaskIds.push(this.current);
    }
    this.current = taskId;
  }

  public back(): string | undefined {
    const previous = this.previousTaskIds.pop();
    if (previous) {
      this.current = previous;
    }
    return previous;
  }

  public clear(): void {
    this.previousTaskIds.length = 0;
    this.current = undefined;
  }
}

export interface TaskStatusPresentation {
  readonly label: string;
  readonly description: string;
}

export function taskStatusPresentationFor(l10n: Localizer): Readonly<Record<TaskStatus, TaskStatusPresentation>> {
  return {
  Ready: {
    label: l10n.t("status.ready.label"),
    description: l10n.t("status.ready.description")
  },
  InProgress: {
    label: l10n.t("status.inProgress.label"),
    description: l10n.t("status.inProgress.description")
  },
  Blocked: {
    label: l10n.t("status.blocked.label"),
    description: l10n.t("status.blocked.description")
  },
  Review: {
    label: l10n.t("status.review.label"),
    description: l10n.t("status.review.description")
  },
  Done: {
    label: l10n.t("status.done.label"),
    description: l10n.t("status.done.description")
  },
  Cancelled: {
    label: l10n.t("status.cancelled.label"),
    description: l10n.t("status.cancelled.description")
  }
  };
}

export const taskStatusPresentation = taskStatusPresentationFor(createLocalizer("ru"));

export interface TaskSnapshot {
  readonly taskId: string;
  readonly taskUid?: string;
  readonly revision: number;
  readonly title: string;
  readonly status: TaskStatus;
  readonly boardStatus?: TaskStatus;
  readonly priority: string;
  readonly labels: readonly string[];
  readonly deadline?: string | null;
  readonly acceptanceCriteriaProgress?: AcceptanceCriteriaProgressSnapshot;
  readonly attachmentCount?: number;
  readonly cardPreview?: CardPreviewSnapshot;
  readonly previewAttachmentId?: string | null;
  readonly dependencies: readonly string[];
  readonly parentTaskId?: string | null;
  readonly assignee?: string | null;
  readonly description?: string;
  readonly createdAt?: string;
  readonly updatedAt?: string;
  readonly readiness?: string;
  readonly acceptanceState?: string;
  readonly acceptanceCriteria?: readonly AcceptanceCriterionSnapshot[];
  readonly executionContract?: ExecutionContractSnapshot;
  readonly linkedArtifacts?: readonly string[];
  readonly activity?: readonly ActivitySnapshot[];
  readonly conversation?: ConversationSnapshot;
  readonly contextSnapshot?: unknown;
  readonly attachments?: readonly AttachmentSnapshot[];
  readonly archivedAt?: string | null;
}

export interface ConversationSnapshot {
  readonly lastMessageSequence: number;
  readonly messages: readonly ConversationMessageSnapshot[];
}

export interface ConversationMessageSnapshot {
  readonly messageId: string;
  readonly sequence: number;
  readonly author: {
    readonly actorId: string;
    readonly actorKind: string;
    readonly role: string;
  };
  readonly createdAt: string;
  readonly agentRunId?: string | null;
  readonly content: readonly ConversationContentSnapshot[];
}

export type ConversationContentSnapshot =
  | { readonly kind: "Markdown"; readonly markdown: string }
  | { readonly kind: "Attachment"; readonly attachmentId: string };

export interface AcceptanceCriteriaProgressSnapshot {
  readonly passed: number;
  readonly total: number;
}

export const legacyTaskBoardTagColors = ["Gray", "Blue", "Green", "Yellow", "Orange", "Red", "Purple"] as const;
export type LegacyTaskBoardTagColor = typeof legacyTaskBoardTagColors[number];
export type TaskBoardTagColor = LegacyTaskBoardTagColor | `#${string}`;

export function normalizeTaskBoardTagColor(value: string): TaskBoardTagColor | undefined {
  const legacy = legacyTaskBoardTagColors.find(color => color.toLocaleLowerCase() === value.toLocaleLowerCase());
  if (legacy) {
    return legacy;
  }

  return /^#[0-9a-f]{6}$/i.test(value) ? value.toUpperCase() as TaskBoardTagColor : undefined;
}

export interface TaskBoardTagSnapshot {
  readonly tagId: string;
  readonly name: string;
  readonly color: TaskBoardTagColor;
}

export interface ExecutionContractSnapshot {
  readonly taskType: string;
  readonly readyToStart: readonly string[];
  readonly stopConditions: readonly string[];
  readonly allowedChanges: readonly string[];
  readonly forbiddenChanges: readonly string[];
  readonly requiredOutputs: readonly string[];
  readonly requiredCommands: readonly string[];
  readonly externalAudit?: string;
}

export interface AcceptanceCriterionSnapshot {
  readonly criterionId: string;
  readonly description: string;
  readonly state: string;
}

export interface ActivitySnapshot {
  readonly activityEntryId: string;
  readonly actorId: string;
  readonly actorKind?: string;
  readonly createdAt: string;
  readonly kind: string;
  readonly payload: string;
}

export interface StatusChangePayload {
  readonly previous: TaskStatus;
  readonly next: TaskStatus;
  readonly reason: string;
}

export function parseStatusChangePayload(payload: string): StatusChangePayload | undefined {
  let document: unknown;
  try {
    document = JSON.parse(payload) as unknown;
  } catch {
    return undefined;
  }

  if (!isJsonRecord(document) ||
    Object.keys(document).length !== 3 ||
    !Object.hasOwn(document, "previous") ||
    !Object.hasOwn(document, "next") ||
    !Object.hasOwn(document, "reason") ||
    !isTaskStatus(document.previous) ||
    !isTaskStatus(document.next) ||
    typeof document.reason !== "string" ||
    document.reason.trim().length === 0) {
    return undefined;
  }

  return {
    previous: document.previous,
    next: document.next,
    reason: document.reason
  };
}

export function taskPatchTopLevelFields(payload: string): readonly string[] | undefined {
  let document: unknown;
  try {
    document = JSON.parse(payload) as unknown;
  } catch {
    return undefined;
  }

  if (!isJsonRecord(document) || !Array.isArray(document.patch)) {
    return undefined;
  }

  const fields: string[] = [];
  const seen = new Set<string>();
  for (const operation of document.patch) {
    if (!isJsonRecord(operation) || typeof operation.path !== "string") {
      return undefined;
    }

    const segments = operation.path.split("/");
    if (segments[0] !== "" || !segments[1]) {
      return undefined;
    }

    const field = decodeJsonPointerToken(segments[1]);
    if (field === undefined) {
      return undefined;
    }

    if (!seen.has(field)) {
      seen.add(field);
      fields.push(field);
    }
  }

  return fields;
}

function isJsonRecord(value: unknown): value is Readonly<Record<string, unknown>> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isTaskStatus(value: unknown): value is TaskStatus {
  return typeof value === "string" && (taskStatuses as readonly string[]).includes(value);
}

function decodeJsonPointerToken(token: string): string | undefined {
  if (/~(?![01])/u.test(token)) {
    return undefined;
  }

  return token.replaceAll("~1", "/").replaceAll("~0", "~");
}

export interface AttachmentSnapshot {
  readonly attachmentId: string;
  readonly displayName: string;
  readonly relativePath: string;
  readonly mediaType: string;
  readonly byteLength: number;
  readonly sha256: string;
  readonly previewUri?: string;
}

export interface CardPreviewSnapshot {
  readonly attachmentId: string;
  readonly displayName: string;
  readonly relativePath: string;
  readonly mediaType: string;
  readonly previewUri?: string;
}

export interface BoardGroup {
  readonly groupId: string;
  readonly kind: "Epoch" | "Milestone";
  readonly title: string;
  readonly rank: string;
  readonly parentGroupId: string | null;
}

export interface BoardPlacement {
  readonly taskId: string;
  readonly groupId: string | null;
  readonly rank: string;
}

export interface TaskBoardSnapshot {
  readonly board: {
    readonly revision: number;
    readonly tags?: readonly TaskBoardTagSnapshot[];
    readonly groups: readonly BoardGroup[];
    readonly placements: readonly BoardPlacement[];
  };
  readonly tasks: readonly TaskSnapshot[];
}

export interface BoardTask extends TaskSnapshot {
  readonly groupId: string | null;
  readonly rank: string;
}

export interface BoardColumn {
  readonly status: TaskStatus;
  readonly tasks: readonly BoardTask[];
}

export interface BoardView {
  readonly boardRevision: number;
  readonly tags: readonly TaskBoardTagSnapshot[];
  readonly groups: readonly BoardGroup[];
  readonly columns: readonly BoardColumn[];
}

export interface SidebarTaskRow {
  readonly taskId: string;
  readonly label: string;
  readonly description: string;
  readonly tooltip: string;
  readonly status: TaskStatus;
  readonly priority: string;
}

export interface RenderTaskSelectionOptions<T> {
  readonly compact: T;
  readonly cached: T | undefined;
  readonly render: (task: T, phase: "loading" | "ready") => Promise<void>;
  readonly load: () => Promise<T>;
  readonly isCurrent: () => boolean;
  readonly remember: (task: T) => void;
}

export interface MoveTaskIntent {
  readonly command: "move";
  readonly taskId: string;
  readonly groupId: string | null;
  readonly rank: string;
  readonly expectedBoardRevision: number;
}

export function buildBoardView(snapshot: TaskBoardSnapshot): BoardView {
  const placementByTask = new Map(snapshot.board.placements.map(placement => [placement.taskId, placement]));
  const columns = taskStatuses.map(status => {
    const tasks = snapshot.tasks
      .filter(task => boardStatusOf(task) === status)
      .map(task => {
        const placement = placementByTask.get(task.taskId);
        return {
          ...task,
          groupId: placement?.groupId ?? null,
          rank: placement?.rank ?? "99999999"
        };
      })
      .sort((left, right) => left.rank.localeCompare(right.rank) || left.taskId.localeCompare(right.taskId));
    return { status, tasks };
  });

  return {
    boardRevision: snapshot.board.revision,
    tags: snapshot.board.tags ?? [],
    groups: orderGroups(snapshot.board.groups),
    columns
  };
}

export function resolveTaskTags(
  task: Pick<TaskSnapshot, "labels">,
  tags: readonly TaskBoardTagSnapshot[]): readonly TaskBoardTagSnapshot[] {
  const tagsById = new Map(tags.map(tag => [tag.tagId, tag]));
  return task.labels.flatMap(tagId => {
    const tag = tagsById.get(tagId);
    return tag ? [tag] : [];
  });
}

export function buildSidebarRows(
  snapshot: TaskBoardSnapshot,
  l10n: Localizer = createLocalizer("ru")): SidebarTaskRow[] {
  const placementByTask = new Map(snapshot.board.placements.map(placement => [placement.taskId, placement]));
  const statusIndex = new Map(taskStatuses.map((status, index) => [status, index]));
  return snapshot.tasks
    .filter(task => !task.archivedAt)
    .map(task => ({
      task,
      rank: placementByTask.get(task.taskId)?.rank ?? "99999999"
    }))
    .sort((left, right) =>
      (statusIndex.get(boardStatusOf(left.task)) ?? taskStatuses.length) -
        (statusIndex.get(boardStatusOf(right.task)) ?? taskStatuses.length) ||
      left.rank.localeCompare(right.rank) ||
      left.task.taskId.localeCompare(right.task.taskId))
    .map(({ task }) => {
      const boardStatus = boardStatusOf(task);
      const status = formatTaskStatus(boardStatus, l10n);
      const label = `${task.taskId} · ${task.title}`;
      const description = `${status} · ${task.priority}`;
      return {
        taskId: task.taskId,
        label,
        description,
        tooltip: `${label}\n${description}`,
        status: boardStatus,
        priority: task.priority
      };
    });
}

export function matchesTaskFilters(
  task: TaskSnapshot,
  query: string,
  priority: string,
  tagIds: ReadonlySet<string>): boolean {
  const normalizedQuery = query.trim().toLocaleLowerCase();
  const matchesText = normalizedQuery.length === 0 ||
    `${task.taskId} ${task.title} ${task.labels.join(" ")}`.toLocaleLowerCase().includes(normalizedQuery);
  const matchesPriority = priority.length === 0 || task.priority === priority;
  const matchesTags = [...tagIds].every(tagId => task.labels.includes(tagId));
  return matchesText && matchesPriority && matchesTags;
}

export async function renderTaskSelection<T>(options: RenderTaskSelectionOptions<T>): Promise<void> {
  await options.render(options.cached ?? options.compact, options.cached ? "ready" : "loading");
  const hydrated = await options.load();
  if (options.isCurrent()) {
    options.remember(hydrated);
    await options.render(hydrated, "ready");
  }
}

export function formatTaskStatus(status: TaskStatus, l10n: Localizer = createLocalizer("ru")): string {
  return taskStatusPresentationFor(l10n)[status].label;
}

export function boardStatusOf(task: Pick<TaskSnapshot, "status" | "boardStatus">): TaskStatus {
  return task.boardStatus ?? task.status;
}

function orderGroups(groups: readonly BoardGroup[]): BoardGroup[] {
  const byRank = (left: BoardGroup, right: BoardGroup): number =>
    left.rank.localeCompare(right.rank) || left.groupId.localeCompare(right.groupId);
  const epochs = groups.filter(group => group.kind === "Epoch").sort(byRank);
  const ordered: BoardGroup[] = [];
  const included = new Set<string>();
  for (const epoch of epochs) {
    ordered.push(epoch);
    included.add(epoch.groupId);
    for (const milestone of groups
      .filter(group => group.kind === "Milestone" && group.parentGroupId === epoch.groupId)
      .sort(byRank)) {
      ordered.push(milestone);
      included.add(milestone.groupId);
    }
  }

  ordered.push(...groups.filter(group => !included.has(group.groupId)).sort(byRank));
  return ordered;
}

export function moveTask(
  task: Pick<TaskSnapshot, "taskId">,
  groupId: string | null,
  rank: string,
  expectedBoardRevision: number): MoveTaskIntent {
  return {
    command: "move",
    taskId: task.taskId,
    groupId,
    rank,
    expectedBoardRevision
  };
}

export function overviewGoalText(
  task: Pick<TaskSnapshot, "taskId" | "title" | "description">,
  l10n: Localizer = createLocalizer("ru")): string {
  const description = task.description?.trim();
  if (!description) {
    return l10n.t("model.noDescription");
  }

  const lines = description.replace(/\r\n/g, "\n").split("\n");
  const firstContent = lines.find(line => line.trim().length > 0)?.trim();
  if (!firstContent?.startsWith(`## ${task.taskId} `)) {
    return description;
  }

  const sectionStart = lines.findIndex(line => line.trim() === "### Самодостаточное описание");
  if (sectionStart < 0) {
    return description;
  }

  const nextSectionOffset = lines
    .slice(sectionStart + 1)
    .findIndex(line => /^###\s+/.test(line.trim()));
  const sectionEnd = nextSectionOffset < 0 ? lines.length : sectionStart + 1 + nextSectionOffset;
  const section = lines.slice(sectionStart + 1, sectionEnd).join("\n").trim();
  return section.length > 0 ? section : description;
}

export function linkedFileArtifacts(artifacts: readonly string[] | undefined): readonly string[] {
  return classifyLinkedArtifacts(artifacts).files;
}

export interface LinkedArtifactClassification {
  readonly files: readonly string[];
  readonly directories: readonly string[];
  readonly ignored: readonly string[];
}

export function classifyLinkedArtifacts(
  artifacts: readonly string[] | undefined): LinkedArtifactClassification {
  const files: string[] = [];
  const directories: string[] = [];
  const ignored: string[] = [];
  for (const value of new Set(artifacts ?? [])) {
    if (!isSafeRelativeArtifactPath(value)) {
      ignored.push(value);
    } else if (/[\\/]$/.test(value)) {
      directories.push(value);
    } else if (isFileArtifactPath(value)) {
      files.push(value);
    } else {
      ignored.push(value);
    }
  }
  return { files, directories, ignored };
}

function isSafeRelativeArtifactPath(value: string): boolean {
  const normalized = value.replaceAll("\\", "/");
  return value.trim() === value && value.length > 0 &&
    !/[\u0000-\u001f]/.test(value) &&
    !/^[a-z][a-z0-9+.-]*:\/\//i.test(value) &&
    !/^(?:[a-z]:|\/)/i.test(normalized) &&
    !/(?:^|\/)\.\.(?:\/|$)/.test(normalized) &&
    !/^(?:dotnet|git|e2d|npm|npx|pnpm|yarn|node|python3?|pwsh|powershell|bash|sh|cmake|msbuild|cargo|java|javac|xcodebuild|gradle|gradlew|\.\/gradlew)\s/i.test(value);
}

function isFileArtifactPath(value: string): boolean {
  const name = value.replaceAll("\\", "/").split("/").at(-1) ?? "";
  return /\.[A-Za-z0-9][A-Za-z0-9._-]*$/.test(name) ||
    /^(?:license|notice|copying|makefile|dockerfile)$/i.test(name) ||
    /^\.(?:gitignore|gitattributes|editorconfig)$/.test(name);
}

export type ArtifactIconKind =
  "folder" | "readme" | "markdown" | "json" | "csharp" | "typescript" | "css" | "project" | "image" | "archive" | "vsix" | "file";

export function artifactIconKind(artifact: string, mediaType?: string): ArtifactIconKind {
  const normalized = artifact.replaceAll("\\", "/").toLowerCase();
  const normalizedMediaType = mediaType?.toLowerCase() ?? "";
  if (normalized.endsWith("/")) {
    return "folder";
  }
  if (normalizedMediaType.startsWith("image/")) {
    return "image";
  }
  if (/^(?:application\/(?:zip|x-7z-compressed|x-rar-compressed|gzip)|application\/x-tar)$/.test(normalizedMediaType)) {
    return "archive";
  }

  const name = normalized.slice(normalized.lastIndexOf("/") + 1);
  if (/^readme(?:\.|$)/.test(name)) {
    return "readme";
  }
  if (/\.(?:md|mdx|markdown)$/.test(name)) {
    return "markdown";
  }
  if (/\.(?:json|jsonc|e2task|e2tasks)$/.test(name)) {
    return "json";
  }
  if (/\.cs$/.test(name)) {
    return "csharp";
  }
  if (/\.(?:ts|tsx|mts|cts)$/.test(name)) {
    return "typescript";
  }
  if (/\.css$/.test(name)) {
    return "css";
  }
  if (/\.(?:csproj|fsproj|vbproj|sln|props|targets)$/.test(name)) {
    return "project";
  }
  if (/\.(?:png|jpe?g|gif|webp|bmp|svg|ico)$/.test(name)) {
    return "image";
  }
  if (/\.(?:zip|7z|rar|tar|gz|tgz)$/.test(name)) {
    return "archive";
  }
  if (/\.vsix$/.test(name)) {
    return "vsix";
  }
  return "file";
}
