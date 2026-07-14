/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import {
  artifactIconKind,
  classifyLinkedArtifacts,
  matchesTaskFilters,
  overviewGoalText,
  parseStatusChangePayload,
  resolveTaskTags,
  taskPatchTopLevelFields,
  taskStatusPresentationFor,
  type AcceptanceCriterionSnapshot,
  type ActivitySnapshot,
  type ArtifactIconKind,
  type AttachmentSnapshot,
  type BoardTask,
  type BoardView,
  type TaskBoardTagColor,
  type TaskBoardTagSnapshot,
  type TaskStatus
} from "./model.js";
import {
  createAgentChatState,
  reduceAgentChatEvent,
  type AgentChatState,
  type AgentPresentationEvent
} from "./agentChat.js";
import {
  parseCommentMarkdown,
  type CommentMarkdownNode
} from "./commentMarkdown.js";
import { createLocalizer } from "./localization.js";
import { createTaskListExpansionState } from "./taskListExpansion.js";

declare function acquireVsCodeApi(): { postMessage(message: unknown): void };
const vscode = acquireVsCodeApi();
const l10n = createLocalizer(document.documentElement.lang);
const taskStatusPresentation = taskStatusPresentationFor(l10n);
const board = requiredElement("board");
const details = requiredElement("details");
const project = requiredElement("project");
const filter = requiredElement("filter") as HTMLInputElement;
const priorityFilter = requiredElement("priority-filter") as HTMLSelectElement;
const filterToggle = requiredElement("filter-toggle");
const filterPanel = requiredElement("filter-panel");
const tagFilterTrigger = requiredElement("tag-filter-trigger") as HTMLButtonElement;
const tagFilterValue = requiredElement("tag-filter-value");
const tagFilterPopup = requiredElement("tag-filter-popup");
const selectedTagIds = new Set<string>();
const filterCount = requiredElement("filter-count");
const resetFilters = requiredElement("reset-filters") as HTMLButtonElement;
const showArchived = requiredElement("show-archived") as HTMLInputElement;
const createTaskTrigger = requiredElement("create") as HTMLButtonElement;
const createTaskDialog = requiredElement("create-dialog");
const createTaskWindow = createTaskDialog.querySelector<HTMLElement>(".create-task-window")!;
const createTaskForm = requiredElement("create-task-form") as HTMLFormElement;
const createTaskTitle = requiredElement("create-task-title") as HTMLInputElement;
const createTaskDescription = requiredElement("create-task-description") as HTMLTextAreaElement;
const createTaskPriority = requiredElement("create-task-priority") as HTMLSelectElement;
const createTaskDeadline = requiredElement("create-task-deadline") as HTMLInputElement;
const createTaskDeadlineError = requiredElement("create-task-deadline-error");
const createTaskError = requiredElement("create-task-error");
const createTaskCancel = requiredElement("create-task-cancel") as HTMLButtonElement;
const createTaskClose = requiredElement("create-task-close") as HTMLButtonElement;
const createTaskSubmit = requiredElement("create-task-submit") as HTMLButtonElement;
const CARD_PREVIEW_LIMIT = 6;
const taskListExpansionState = createTaskListExpansionState();
const SVG_NAMESPACE = "http://www.w3.org/2000/svg";
let activityDropdownSequence = 0;
let currentView: BoardView | undefined;
let currentDetailsTask: BoardTask | undefined;
const agentChatStates = new Map<string, AgentChatState>();
let taskCardToRestoreFocus: HTMLButtonElement | null = null;
let tagFilterOptions: HTMLButtonElement[] = [];
let tagFilterActiveIndex = 0;
let tagFilterExpanded = false;
let createTaskPending = false;

requiredElement("refresh").addEventListener("click", () => vscode.postMessage({ type: "refresh" }));
requiredElement("tag-settings").addEventListener("click", () => vscode.postMessage({ type: "manageTags" }));
requiredElement("create").addEventListener("click", () => openCreateTaskForm());
createTaskDialog.addEventListener("click", event => {
  if (event.target === createTaskDialog) {
    closeCreateTaskForm();
  }
});
createTaskForm.addEventListener("submit", event => {
  event.preventDefault();
  submitCreateTask();
});
createTaskTitle.addEventListener("input", updateCreateTaskSubmit);
createTaskDeadline.addEventListener("input", () => {
  validateCreateTaskDeadline(true);
  updateCreateTaskSubmit();
});
createTaskCancel.addEventListener("click", () => closeCreateTaskForm());
createTaskClose.addEventListener("click", () => closeCreateTaskForm());
filterToggle.addEventListener("click", () => {
  closeTagFilter(false);
  togglePopover("filter-toggle", "filter-panel");
});
document.addEventListener("pointerdown", closeFilterOnOutsidePointer, true);
filter.addEventListener("input", () => renderBoard());
priorityFilter.addEventListener("change", () => {
  updateFilterCount();
  renderBoard();
});
resetFilters.addEventListener("click", resetPanelFilters);
showArchived.addEventListener("change", () => renderBoard());
updateFilterCount();
details.addEventListener("click", event => {
  if (event.target === details) {
    closeTaskDetails();
  }
});
window.addEventListener("keydown", event => {
  if (!createTaskDialog.hidden) {
    if (event.ctrlKey && event.key === "Enter") {
      event.preventDefault();
      submitCreateTask();
    } else if (event.key === "Escape") {
      event.preventDefault();
      closeCreateTaskForm();
    } else if (event.key === "Tab") {
      trapCreateTaskFocus(event);
    }
    return;
  }

  if (event.ctrlKey && event.key.toLocaleLowerCase() === "f") {
    event.preventDefault();
    filter.focus();
    filter.select();
    return;
  }

  if (event.key === "Escape" && !details.hidden) {
    event.preventDefault();
    closeTaskDetails();
  } else if (event.key === "Escape") {
    closePopover("filter-toggle", "filter-panel");
  }
});

window.addEventListener("message", event => {
  const message = event.data as Record<string, unknown>;
  if (message.type === "createResult") {
    if (message.ok === true) {
      createTaskPending = false;
      closeCreateTaskForm(true);
    } else {
      setCreateTaskPending(false);
      createTaskError.textContent = typeof message.message === "string"
        ? message.message
        : l10n.t("task.createFailed");
      createTaskError.hidden = false;
      createTaskTitle.focus();
    }
  } else if (message.type === "state") {
    currentView = message.view as BoardView;
    synchronizeTagFilter(currentView.tags);
    project.textContent = typeof message.project === "string" ? message.project : "";
    renderBoard();
  } else if (message.type === "task") {
    const task = (message.task as BoardTask | null) ?? null;
    if (task) {
      const state = agentChatStates.get(task.taskId);
      if (state?.runId && task.conversation?.messages.some(candidate => candidate.agentRunId === state.runId)) {
        agentChatStates.delete(task.taskId);
      }
    }
    renderDetails(
      task,
      message.loading === true,
      message.canGoBack === true);
  } else if (message.type === "taskError") {
    renderDetailsError(
      message.task as BoardTask,
      typeof message.message === "string" ? message.message : l10n.t("task.loadFailed", ""),
      message.canGoBack === true);
  } else if (message.type === "agentEvent" && typeof message.taskId === "string") {
    const event = message.event as AgentPresentationEvent;
    const previous = agentChatStates.get(message.taskId) ?? createAgentChatState(message.taskId);
    agentChatStates.set(message.taskId, reduceAgentChatEvent(previous, event));
    if (currentDetailsTask?.taskId === message.taskId) {
      document.querySelector<HTMLElement>(".agent-chat")?.replaceWith(createAgentChat(currentDetailsTask));
    }
  }
});

function renderBoard(): void {
  board.replaceChildren();
  const query = filter.value;
  for (const column of currentView?.columns ?? []) {
    const section = document.createElement("section");
    section.className = "column";
    section.dataset.status = column.status;
    const tasks = column.tasks.filter(task =>
      (showArchived.checked || !task.archivedAt) &&
      matchesTaskFilters(task, query, priorityFilter.value, selectedTagIds));

    const header = document.createElement("header");
    header.className = "column-header";
    const title = document.createElement("div");
    title.className = "column-title";
    const heading = document.createElement("h2");
    const presentation = taskStatusPresentation[column.status];
    heading.textContent = presentation.label;
    heading.title = `${presentation.label} — ${presentation.description}`;
    const count = document.createElement("span");
    count.className = "column-count";
    count.textContent = String(tasks.length);
    title.append(heading, count);
    const menu = document.createElement("button");
    menu.type = "button";
    menu.className = "column-action";
    menu.setAttribute("aria-label", l10n.t("board.showAllColumn", presentation.label));
    menu.title = l10n.t("board.showAll");
    menu.textContent = "⋮";
    menu.addEventListener("click", () => {
      for (const showMore of section.querySelectorAll<HTMLButtonElement>(".show-more")) {
        showMore.click();
      }
    });
    const collapse = document.createElement("button");
    collapse.type = "button";
    collapse.className = "column-action";
    collapse.setAttribute("aria-label", l10n.t("board.collapseColumn", presentation.label));
    collapse.setAttribute("aria-expanded", "true");
    collapse.title = l10n.t("board.collapse");
    collapse.append(createChevronIcon());
    const columnBody = document.createElement("div");
    columnBody.className = "column-body";
    collapse.addEventListener("click", () => {
      const expanded = collapse.getAttribute("aria-expanded") === "true";
      collapse.setAttribute("aria-expanded", String(!expanded));
      collapse.setAttribute("aria-label", l10n.t(expanded ? "board.expandColumn" : "board.collapseColumn", presentation.label));
      collapse.title = l10n.t(expanded ? "board.expand" : "board.collapse");
      section.classList.toggle("column-collapsed", expanded);
    });
    header.append(title, menu, collapse);
    section.append(header, columnBody);

    columnBody.append(createTaskList(column.status, undefined, tasks));
    board.append(section);
  }
}

function updateFilterCount(): void {
  const activeCount = Number(priorityFilter.value !== "") + Number(selectedTagIds.size > 0);
  filterCount.textContent = String(activeCount);
  filterCount.hidden = activeCount === 0;
  resetFilters.disabled = activeCount === 0;
}

function resetPanelFilters(): void {
  priorityFilter.value = "";
  selectedTagIds.clear();
  for (const option of tagFilterOptions) {
    option.setAttribute("aria-selected", "false");
  }
  updateTagFilterSummary(currentView?.tags ?? []);
  updateFilterCount();
  renderBoard();
}

function synchronizeTagFilter(tags: readonly TaskBoardTagSnapshot[]): void {
  const availableTagIds = new Set(tags.map(tag => tag.tagId));
  for (const tagId of [...selectedTagIds]) {
    if (!availableTagIds.has(tagId)) {
      selectedTagIds.delete(tagId);
    }
  }

  tagFilterOptions = [];
  tagFilterPopup.replaceChildren();
  for (const tag of tags) {
    const option = document.createElement("button");
    option.type = "button";
    option.className = "tag-filter-option";
    option.id = `tag-filter-option-${tagFilterOptions.length}`;
    option.dataset.tagId = tag.tagId;
    option.setAttribute("role", "option");
    option.setAttribute("aria-selected", String(selectedTagIds.has(tag.tagId)));
    option.tabIndex = -1;
    const check = document.createElement("span");
    check.className = "tag-filter-check";
    check.setAttribute("aria-hidden", "true");
    check.textContent = "✓";
    const optionLabel = document.createElement("span");
    optionLabel.className = "tag-filter-option-label";
    optionLabel.textContent = tag.name;
    option.append(check, optionLabel);
    const optionIndex = tagFilterOptions.length;
    option.addEventListener("pointerenter", () => setActiveTagFilterOption(optionIndex));
    option.addEventListener("click", () => {
      toggleTagFilterSelection(tag.tagId);
      tagFilterTrigger.focus({ preventScroll: true });
    });
    tagFilterOptions.push(option);
    tagFilterPopup.append(option);
  }

  const selectedIndex = tags.findIndex(tag => selectedTagIds.has(tag.tagId));
  tagFilterActiveIndex = selectedIndex >= 0 ? selectedIndex : 0;
  updateTagFilterSummary(tags);
  updateFilterCount();
}

function toggleTagFilterSelection(tagId: string): void {
  if (selectedTagIds.has(tagId)) {
    selectedTagIds.delete(tagId);
  } else {
    selectedTagIds.add(tagId);
  }

  for (const option of tagFilterOptions) {
    option.setAttribute("aria-selected", String(selectedTagIds.has(option.dataset.tagId ?? "")));
  }
  updateTagFilterSummary(currentView?.tags ?? []);
  updateFilterCount();
  renderBoard();
}

function updateTagFilterSummary(tags: readonly TaskBoardTagSnapshot[]): void {
  const selectedTags = tags.filter(tag => selectedTagIds.has(tag.tagId));
  const summary = selectedTags.length === 0
    ? l10n.t("board.allTags")
    : selectedTags.length === 1
      ? selectedTags[0]!.name
      : l10n.t("board.selectedTags", selectedTags.length);
  tagFilterValue.textContent = summary;
  tagFilterTrigger.title = summary;
}

function setActiveTagFilterOption(index: number): void {
  if (tagFilterOptions.length === 0) {
    return;
  }

  tagFilterActiveIndex = (index + tagFilterOptions.length) % tagFilterOptions.length;
  tagFilterOptions.forEach((option, optionIndex) => {
    option.classList.toggle("active", optionIndex === tagFilterActiveIndex);
  });
  const activeOption = tagFilterOptions[tagFilterActiveIndex]!;
  tagFilterTrigger.setAttribute("aria-activedescendant", activeOption.id);
  activeOption.scrollIntoView({ block: "nearest" });
}

function openTagFilter(): void {
  if (tagFilterExpanded || tagFilterOptions.length === 0) {
    return;
  }

  tagFilterExpanded = true;
  tagFilterPopup.hidden = false;
  tagFilterTrigger.setAttribute("aria-expanded", "true");
  setActiveTagFilterOption(tagFilterActiveIndex);
}

function closeTagFilter(restoreFocus: boolean): void {
  if (!tagFilterExpanded) {
    return;
  }

  tagFilterExpanded = false;
  tagFilterPopup.hidden = true;
  tagFilterTrigger.setAttribute("aria-expanded", "false");
  tagFilterTrigger.removeAttribute("aria-activedescendant");
  if (restoreFocus) {
    tagFilterTrigger.focus();
  }
}

function closeFilterOnOutsidePointer(event: PointerEvent): void {
  if (event.target instanceof Node &&
      !filterToggle.contains(event.target) && !filterPanel.contains(event.target)) {
    closeTagFilter(false);
    closePopover("filter-toggle", "filter-panel");
  }
}

tagFilterTrigger.addEventListener("click", () => {
  if (tagFilterExpanded) {
    closeTagFilter(false);
  } else {
    openTagFilter();
  }
});

tagFilterTrigger.addEventListener("keydown", event => {
  switch (event.key) {
    case "ArrowDown":
      event.preventDefault();
      event.stopPropagation();
      if (tagFilterExpanded) {
        setActiveTagFilterOption(tagFilterActiveIndex + 1);
      } else {
        openTagFilter();
      }
      break;
    case "ArrowUp":
      event.preventDefault();
      event.stopPropagation();
      if (tagFilterExpanded) {
        setActiveTagFilterOption(tagFilterActiveIndex - 1);
      } else {
        openTagFilter();
      }
      break;
    case "Home":
      event.preventDefault();
      event.stopPropagation();
      openTagFilter();
      setActiveTagFilterOption(0);
      break;
    case "End":
      event.preventDefault();
      event.stopPropagation();
      openTagFilter();
      setActiveTagFilterOption(tagFilterOptions.length - 1);
      break;
    case "Enter":
    case " ": {
      event.preventDefault();
      event.stopPropagation();
      if (!tagFilterExpanded) {
        openTagFilter();
        break;
      }

      const tagId = tagFilterOptions[tagFilterActiveIndex]?.dataset.tagId;
      if (tagId) {
        toggleTagFilterSelection(tagId);
      }
      break;
    }
    case "Escape":
      if (tagFilterExpanded) {
        event.preventDefault();
        event.stopPropagation();
        closeTagFilter(true);
      }
      break;
  }
});

function createChevronIcon(className = "chevron-icon"): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.setAttribute("class", className);
  icon.setAttribute("viewBox", "0 0 16 16");
  icon.setAttribute("aria-hidden", "true");
  icon.setAttribute("focusable", "false");
  const path = document.createElementNS(SVG_NAMESPACE, "path");
  path.setAttribute("d", "M4 6l4 4 4-4");
  icon.append(path);
  return icon;
}

function createTaskList(status: TaskStatus, groupId: string | null | undefined, tasks: readonly BoardTask[]): HTMLDivElement {
  const list = document.createElement("div");
  list.className = "card-list";
  list.dataset.status = status;
  list.addEventListener("dragover", event => event.preventDefault());
  list.addEventListener("drop", event => {
    event.preventDefault();
    const taskId = event.dataTransfer?.getData("text/plain");
    const task = currentView?.columns.flatMap(candidate => candidate.tasks).find(candidate => candidate.taskId === taskId);
    if (task) {
      if (status !== (task.boardStatus ?? task.status)) {
        return;
      }

      const targetGroupId = groupId === undefined ? task.groupId : groupId;
      const rankedTasks = groupId === undefined
        ? tasks.filter(candidate => candidate.groupId === targetGroupId)
        : tasks;
      sendMove(task, targetGroupId, nextRank(rankedTasks));
    }
  });
  const renderCards = (): void => {
    list.replaceChildren();
    const visible = taskListExpansionState.visibleTasks(status, tasks, CARD_PREVIEW_LIMIT);
    for (const task of visible) {
      list.append(createTaskCard(task));
    }

    if (visible.length < tasks.length) {
      const more = document.createElement("button");
      more.type = "button";
      more.className = "show-more";
      more.textContent = l10n.t("board.moreTasks", tasks.length - visible.length);
      more.addEventListener("click", () => {
        taskListExpansionState.expand(status);
        renderCards();
      });
      list.append(more);
    }
  };
  renderCards();

  return list;
}

function createTaskCard(task: BoardTask): HTMLButtonElement {
  const card = document.createElement("button");
  card.type = "button";
  card.className = "card";
  card.dataset.taskId = task.taskId;
  card.draggable = true;
  const id = document.createElement("span");
  id.className = "task-id";
  id.textContent = task.taskId;
  const tags = document.createElement("span");
  tags.className = "card-tags";
  for (const tag of resolveTaskTags(task, currentView?.tags ?? [])) {
    tags.append(createTaskTagChip(tag));
  }
  const title = document.createElement("span");
  title.className = "card-title";
  appendInlineStrong(title, task.title);
  const footer = document.createElement("span");
  footer.className = "card-footer";
  const priority = document.createElement("span");
  priority.className = "priority-badge";
  priority.textContent = task.priority;
  const identity = document.createElement("span");
  identity.className = "card-identity";
  identity.append(id, priority);
  const indicators = document.createElement("span");
  indicators.className = "card-indicators";
  const metadata = document.createElement("span");
  metadata.className = "card-metadata";
  const blocked = task.boardStatus === "Blocked" || task.status === "Blocked" || (task.readiness && task.readiness !== "Ready");
  if (blocked) {
    indicators.append(createCardIndicator(createBlockerIndicatorIcon(), task.dependencies.length || 1, "warning", l10n.t("indicator.blockers")));
  } else if (task.dependencies.length > 0) {
    indicators.append(createCardIndicator("↗", task.dependencies.length, "", l10n.t("indicator.dependencies")));
  }
  if (task.deadline) {
    metadata.append(createCardMetadata("calendar", formatCardDeadline(task.deadline), l10n.t("indicator.deadline")));
  }
  if ((task.acceptanceCriteriaProgress?.total ?? 0) > 0) {
    const progress = task.acceptanceCriteriaProgress!;
    metadata.append(createCardMetadata("checklist", `${progress.passed}/${progress.total}`, l10n.t("indicator.checklist")));
  }
  if ((task.attachmentCount ?? 0) > 0) {
    metadata.append(createCardMetadata("attachment", String(task.attachmentCount), l10n.t("indicator.attachments")));
  }
  if (task.status === "Review" && task.acceptanceState === "Submitted") {
    indicators.append(createCardIndicator("⌛", null, "success", l10n.t("indicator.awaitingDecision")));
  } else if (task.status === "Done") {
    indicators.append(createCardIndicator("✓", null, "success", l10n.t("indicator.done")));
  }
  footer.append(identity, metadata, indicators);
  if (task.cardPreview?.previewUri) {
    const cover = document.createElement("img");
    cover.className = "task-card-cover";
    cover.src = task.cardPreview.previewUri;
    cover.alt = task.cardPreview.displayName;
    cover.loading = "lazy";
    cover.decoding = "async";
    card.append(cover);
  }
  const body = document.createElement("span");
  body.className = "task-card-body";
  if (tags.childElementCount > 0) {
    body.append(tags);
  }
  body.append(title, footer);
  card.append(body);
  card.addEventListener("click", () => {
    taskCardToRestoreFocus = card;
    vscode.postMessage({ type: "openTask", taskId: task.taskId, navigation: "direct" });
  });
  card.addEventListener("dragstart", event => event.dataTransfer?.setData("text/plain", task.taskId));
  return card;
}

function formatCardDeadline(value: string): string {
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) {
    return value;
  }

  return new Intl.DateTimeFormat(document.documentElement.lang, { day: "2-digit", month: "short" })
    .format(new Date(year, month - 1, day));
}

function createCardMetadata(
  kind: "calendar" | "checklist" | "attachment",
  text: string,
  title: string): HTMLSpanElement {
  const item = document.createElement("span");
  item.className = "card-metadata-item";
  item.title = title;
  item.append(createCardMetadataIcon(kind), text);
  return item;
}

function createCardMetadataIcon(kind: "calendar" | "checklist" | "attachment"): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.setAttribute("viewBox", "0 0 16 16");
  icon.setAttribute("aria-hidden", "true");
  const path = document.createElementNS(SVG_NAMESPACE, "path");
  path.setAttribute("d", kind === "calendar"
    ? "M3.5 4.5h9v8h-9zM5.5 2.5v3M10.5 2.5v3M3.5 7h9"
    : kind === "checklist"
      ? "M2.5 4.5l1.5 1.5 2.5-3M8 5h5M2.5 10l1.5 1.5 2.5-3M8 10h5"
      : "M6 5.5v5a2 2 0 004 0v-6a3 3 0 00-6 0v6.5a4 4 0 008 0V6");
  icon.append(path);
  return icon;
}

function createBlockerIndicatorIcon(): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.setAttribute("viewBox", "0 0 16 16");
  icon.setAttribute("aria-hidden", "true");
  icon.setAttribute("focusable", "false");
  const outline = document.createElementNS(SVG_NAMESPACE, "path");
  outline.setAttribute("d", "M8 2.25 14 13.25H2L8 2.25Z");
  const mark = document.createElementNS(SVG_NAMESPACE, "path");
  mark.setAttribute("d", "M8 5.75v3.5M8 11.25h.01");
  icon.append(outline, mark);
  return icon;
}

function createCardIndicator(
  icon: string | SVGSVGElement,
  count: number | null,
  stateClass: string,
  title: string): HTMLSpanElement {
  const indicator = document.createElement("span");
  indicator.className = `card-indicator ${stateClass}`.trim();
  indicator.title = title;
  indicator.setAttribute("aria-label", title);
  if (typeof icon === "string") {
    indicator.textContent = count === null ? icon : `${icon} ${count}`;
  } else {
    indicator.append(icon);
    if (count !== null) {
      indicator.append(String(count));
    }
  }
  return indicator;
}

function renderDetails(task: BoardTask | null, loading = false, canGoBack = false): void {
  currentDetailsTask = task ?? undefined;
  details.replaceChildren();
  setDetailsVisibility(task !== null);
  if (!task) {
    return;
  }

  const detailsWindow = document.createElement("section");
  detailsWindow.className = "details-window";
  detailsWindow.tabIndex = -1;
  const titleBar = createDetailsTitleBar(task, canGoBack);
  if (loading) {
    const detailsContent = document.createElement("div");
    detailsContent.className = "details-content";
    detailsContent.classList.add("details-loading");
    detailsContent.setAttribute("aria-busy", "true");
    const loadingStatus = document.createElement("p");
    loadingStatus.className = "details-loading-status";
    loadingStatus.setAttribute("role", "status");
    loadingStatus.textContent = l10n.t("task.loading");
    detailsContent.append(loadingStatus, createDetailsSkeleton());
    detailsWindow.append(titleBar, detailsContent);
    details.append(detailsWindow);
    detailsWindow.focus();
    return;
  }

  const detailsHeader = document.createElement("div");
  detailsHeader.className = "details-header";
  const tabs = document.createElement("div");
  tabs.className = "details-tabs";
  tabs.setAttribute("role", "tablist");
  tabs.setAttribute("aria-label", l10n.t("task.sections"));
  const detailsContent = document.createElement("div");
  detailsContent.className = "details-content";
  const tabDefinitions = [
    { id: "overview", label: l10n.t("task.tab.overview"), create: () => createOverviewTabPanel(task) },
    { id: "details", label: l10n.t("task.tab.details"), create: () => createDetailsTabPanel(task) },
    { id: "dependencies", label: l10n.t("task.tab.dependencies"), create: () => createDependenciesTabPanel(task) },
    { id: "activity", label: l10n.t("task.tab.activity"), create: () => createActivityTabPanel(task) }
  ] as const;
  const defaultTaskTabId = "overview";
  const tabButtons = new Map<string, HTMLButtonElement>();
  const activateTab = (tabId: string, focus = false): void => {
    const definition = tabDefinitions.find(candidate => candidate.id === tabId)
      ?? tabDefinitions.find(candidate => candidate.id === defaultTaskTabId)!;
    for (const [id, button] of tabButtons) {
      const selected = id === definition.id;
      button.setAttribute("aria-selected", String(selected));
      button.tabIndex = selected ? 0 : -1;
    }
    const panel = definition.create();
    panel.id = `task-tabpanel-${definition.id}`;
    panel.setAttribute("role", "tabpanel");
    panel.setAttribute("aria-labelledby", `task-tab-${definition.id}`);
    detailsContent.replaceChildren(panel);
    if (focus) {
      tabButtons.get(definition.id)?.focus();
    }
  };
  for (const definition of tabDefinitions) {
    const tab = document.createElement("button");
    tab.type = "button";
    tab.id = `task-tab-${definition.id}`;
    tab.textContent = definition.label;
    tab.setAttribute("role", "tab");
    tab.setAttribute("aria-controls", `task-tabpanel-${definition.id}`);
    tab.setAttribute("aria-selected", "false");
    tab.tabIndex = -1;
    tab.addEventListener("click", () => activateTab(definition.id));
    tab.addEventListener("keydown", event => {
      if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") {
        return;
      }
      event.preventDefault();
      const index = tabDefinitions.findIndex(candidate => candidate.id === definition.id);
      const direction = event.key === "ArrowRight" ? 1 : -1;
      const next = tabDefinitions[(index + direction + tabDefinitions.length) % tabDefinitions.length]!;
      activateTab(next.id, true);
    });
    tabButtons.set(definition.id, tab);
    tabs.append(tab);
  }
  detailsHeader.append(titleBar, tabs);

  detailsWindow.append(detailsHeader, detailsContent);
  if (currentView && (task.archivedAt || task.status === "Review" || task.status === "Cancelled")) {
    const expectedBoardRevision = currentView.boardRevision;
    const footer = document.createElement("footer");
    footer.className = "details-footer";
    if (task.archivedAt) {
      const unarchive = actionButton(l10n.t("task.unarchive"), () => vscode.postMessage({
        type: "unarchive",
        taskId: task.taskId,
        expectedTaskRevision: task.revision,
        expectedBoardRevision
      }));
      unarchive.className = "details-secondary-action";
      footer.append(unarchive);
    } else if (task.status === "Review") {
      const allCriteriaPassed = task.acceptanceCriteria?.every(criterion => criterion.state === "Passed") ?? true;
      const accept = actionButton(l10n.t("task.accept"), () => vscode.postMessage({
        type: "accept",
        taskId: task.taskId,
        reason: l10n.t("task.acceptReason"),
        expectedTaskRevision: task.revision
      }));
      accept.className = "details-accept-action";
      accept.disabled = !allCriteriaPassed;
      if (!allCriteriaPassed) {
        accept.title = l10n.t("task.acceptUnavailable");
      }
      footer.append(accept);
    } else if (task.status === "Cancelled") {
      const archive = actionButton(l10n.t("task.archive"), () => vscode.postMessage({
        type: "archive",
        taskId: task.taskId,
        expectedTaskRevision: task.revision,
        expectedBoardRevision
      }));
      archive.className = "details-destructive-action";
      const reopen = actionButton(l10n.t("task.reopen"), () => vscode.postMessage({
        type: "reopen",
        taskId: task.taskId,
        reason: l10n.t("task.reopenReason"),
        expectedTaskRevision: task.revision
      }));
      reopen.className = "details-primary-action";
      footer.append(archive, reopen);
    }
    detailsWindow.append(footer);
  }
  details.append(detailsWindow);
  activateTab(defaultTaskTabId);
  detailsWindow.focus();
}

function createTaskTagChip(tag: TaskBoardTagSnapshot): HTMLSpanElement {
  const chip = document.createElement("span");
  chip.className = "card-tag";
  applyTagAccent(chip, tag.color);
  chip.textContent = tag.name;
  return chip;
}

function applyTagAccent(element: HTMLElement, color: TaskBoardTagColor): void {
  element.style.setProperty("--tag-accent", resolveTagAccent(color));
}

function resolveTagAccent(color: TaskBoardTagColor): string {
  switch (color) {
    case "Gray": return "var(--vscode-descriptionForeground)";
    case "Blue": return "var(--vscode-charts-blue)";
    case "Green": return "var(--vscode-charts-green)";
    case "Yellow": return "var(--vscode-charts-yellow)";
    case "Orange": return "var(--vscode-charts-orange)";
    case "Red": return "var(--vscode-charts-red)";
    case "Purple": return "var(--vscode-charts-purple)";
    default: return /^#[0-9A-F]{6}$/.test(color) ? color : "var(--vscode-descriptionForeground)";
  }
}

function createDetailsTitleBar(task: BoardTask, canGoBack: boolean): HTMLElement {
  const titleBar = document.createElement("header");
  titleBar.className = "details-titlebar";
  const heading = document.createElement("h2");
  heading.id = "task-details-title";
  appendInlineStrong(heading, task.title);
  const close = document.createElement("button");
  close.type = "button";
  close.className = "window-close";
  close.setAttribute("aria-label", l10n.t("task.closeDetails"));
  close.title = l10n.t("task.close");
  close.textContent = "×";
  close.addEventListener("click", closeTaskDetails);
  titleBar.append(createDetailsBackButton(canGoBack), heading, close);
  return titleBar;
}

function createDetailsBackButton(canGoBack: boolean): HTMLButtonElement {
  const back = document.createElement("button");
  back.type = "button";
  back.className = "details-back";
  back.setAttribute("aria-label", l10n.t("task.back"));
  back.title = l10n.t("task.back");
  back.disabled = !canGoBack;
  back.append(createBackIcon());
  back.addEventListener("click", () => vscode.postMessage({ type: "navigateBack" }));
  return back;
}

function createBackIcon(): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.setAttribute("class", "details-back-icon");
  icon.setAttribute("viewBox", "0 0 16 16");
  icon.setAttribute("aria-hidden", "true");
  icon.setAttribute("focusable", "false");
  const path = document.createElementNS(SVG_NAMESPACE, "path");
  path.setAttribute("d", "M9.5 3.5 5 8l4.5 4.5M5.25 8H13");
  icon.append(path);
  return icon;
}

function createOverviewTabPanel(task: BoardTask): HTMLElement {
  const panel = document.createElement("div");
  panel.className = "details-overview overview-grid";
  const main = document.createElement("div");
  main.className = "overview-main overview-main-panel";
  const sidebar = document.createElement("aside");
  sidebar.className = "overview-sidebar";

  const goal = detailSection(l10n.t("overview.goal"));
  const description = document.createElement("pre");
  description.className = "task-description";
  appendInlineStrong(description, overviewGoalText(task, l10n));
  goal.append(description);

  const steps = detailSection(l10n.t("overview.steps"));
  steps.append(numberedList(task.executionContract?.requiredOutputs));

  const context = detailSection(l10n.t("overview.context"));
  const contextIntro = document.createElement("p");
  appendInlineStrong(
    contextIntro,
    l10n.t("overview.contextIntro",
      task.executionContract?.taskType ?? l10n.t("overview.notSpecifiedLower"),
      task.readiness ?? l10n.t("overview.notSpecifiedLower")));
  const contextList = textList([
    ...(task.executionContract?.readyToStart ?? []),
    ...(task.executionContract?.stopConditions ?? []).map(item => l10n.t("overview.stopCondition", item))
  ]);
  context.append(contextIntro, contextList);

  const summary = detailSection(l10n.t("overview.summary"));
  summary.append(overviewSummary(task));

  const classifiedArtifacts = classifyLinkedArtifacts(task.linkedArtifacts);
  const linked = detailSection(l10n.t("overview.linkedFiles"));
  linked.append(linkedArtifactsList(classifiedArtifacts.files));

  const attachments = detailSection(l10n.t("overview.attachments"));
  attachments.append(attachmentArtifactsList(task));

  const directories = detailSection(l10n.t("overview.directories"));
  directories.append(directoryArtifactsList(classifiedArtifacts.directories));

  const chat = createAgentChat(task);

  main.append(goal, steps, context);
  sidebar.append(summary, linked, attachments, directories);
  panel.append(main, sidebar, chat);
  return panel;
}

function createAgentChat(task: BoardTask): HTMLElement {
  const section = document.createElement("section");
  section.className = "agent-chat";
  section.classList.add("detail-section");
  const heading = document.createElement("h3");
  heading.textContent = l10n.t("chat.title");
  const messages = document.createElement("div");
  messages.className = "agent-chat-messages";
  messages.setAttribute("role", "log");
  messages.setAttribute("aria-live", "polite");
  for (const message of task.conversation?.messages ?? []) {
    const item = document.createElement("article");
    item.className = `agent-chat-message agent-chat-message--${message.author.actorKind === "Agent" ? "agent" : "human"}`;
    const author = document.createElement("strong");
    author.textContent = message.author.actorKind === "Agent" ? l10n.t("chat.agent") : l10n.t("chat.user");
    const body = document.createElement("div");
    for (const content of message.content) {
      if (content.kind === "Markdown") {
        appendCommentMarkdown(body, content.markdown);
      }
    }
    item.append(author, body);
    messages.append(item);
  }
  const state = agentChatStates.get(task.taskId);
  if (state) {
    for (const transient of state.transient) {
      const row = document.createElement("div");
      row.className = `agent-chat-transient agent-chat-transient--${transient.kind}`;
      row.textContent = transient.kind === "tool"
        ? `${transient.tool}: ${transient.text}`
        : transient.text;
      messages.append(row);
    }
    if (state.finalText) {
      const final = document.createElement("article");
      final.className = "agent-chat-message agent-chat-message--agent agent-chat-message--streaming";
      const author = document.createElement("strong");
      author.textContent = l10n.t("chat.agent");
      const body = document.createElement("div");
      appendCommentMarkdown(body, state.finalText);
      final.append(author, body);
      messages.append(final);
    }
  }
  if (messages.childElementCount === 0) {
    messages.append(emptyState(l10n.t("chat.empty")));
  }

  const permission = state?.permission;
  if (permission) {
    const request = document.createElement("div");
    request.className = "agent-chat-permission";
    const text = document.createElement("p");
    text.textContent = permission.text;
    const actions = document.createElement("div");
    for (const [response, label] of [
      ["once", l10n.t("chat.permissionOnce")],
      ["session", l10n.t("chat.permissionSession")],
      ["reject", l10n.t("chat.permissionReject")]
    ] as const) {
      const button = document.createElement("button");
      button.type = "button";
      button.textContent = label;
      button.addEventListener("click", () => vscode.postMessage({
        type: "respondAgentPermission",
        taskId: task.taskId,
        permissionId: permission.permissionId,
        response
      }));
      actions.append(button);
    }
    request.append(text, actions);
    section.append(heading, messages, request);
  } else {
    section.append(heading, messages);
  }

  const composer = document.createElement("form");
  composer.className = "agent-chat-composer";
  const input = document.createElement("textarea");
  input.rows = 3;
  input.maxLength = 16_384;
  input.placeholder = l10n.t("chat.placeholder");
  input.setAttribute("aria-label", input.placeholder);
  input.disabled = state?.running === true;
  const action = document.createElement("button");
  action.type = state?.running ? "button" : "submit";
  action.textContent = state?.running ? l10n.t("chat.cancel") : l10n.t("chat.send");
  if (state?.running) {
    action.addEventListener("click", () => vscode.postMessage({ type: "cancelAgentRun", taskId: task.taskId }));
  }
  composer.addEventListener("submit", event => {
    event.preventDefault();
    const text = input.value.trim();
    if (!text) {
      return;
    }
    vscode.postMessage({
      type: "sendAgentMessage",
      taskId: task.taskId,
      text,
      expectedTaskRevision: task.revision
    });
    input.value = "";
  });
  composer.append(input, action);
  section.append(composer);
  return section;
}

function createDetailsTabPanel(task: BoardTask): HTMLElement {
  const panel = document.createElement("div");
  panel.className = "details-structured";
  const primaryGrid = document.createElement("div");
  primaryGrid.className = "details-primary-grid";
  const parameterGrid = document.createElement("div");
  parameterGrid.className = "details-parameter-grid";
  parameterGrid.append(
    referenceField(l10n.t("details.status"), taskStatusPresentation[task.boardStatus ?? task.status].label, "status"),
    referenceField(l10n.t("details.priority"), task.priority, "priority"),
    referenceField(l10n.t("details.readiness"), task.readiness ?? l10n.t("details.notSpecified")),
    referenceField(l10n.t("details.taskType"), task.executionContract?.taskType ?? l10n.t("details.notSpecified")),
    referenceField(l10n.t("details.acceptanceState"), task.acceptanceState ?? l10n.t("details.notSpecified")));
  primaryGrid.append(parameterGrid);

  const metadataStrip = document.createElement("div");
  metadataStrip.className = "details-metadata-strip";
  metadataStrip.append(
    referenceField(l10n.t("details.created"), formatTaskDate(task.createdAt)),
    referenceField(l10n.t("details.updated"), formatTaskDate(task.updatedAt)));

  const criteriaSection = detailSection(l10n.t("details.criteria"));
  appendCriteria(criteriaSection, task.acceptanceCriteria, false);

  const contractSection = detailSection(l10n.t("details.contract"));
  const contractGrid = document.createElement("div");
  contractGrid.className = "execution-contract-grid";
  contractGrid.append(
    contractCard(l10n.t("details.allowed"), task.executionContract?.allowedChanges, "allowed"),
    contractCard(l10n.t("details.forbidden"), task.executionContract?.forbiddenChanges, "forbidden"),
    contractCard(l10n.t("details.result"), task.executionContract?.requiredOutputs, "result"),
    contractCard(l10n.t("details.stopConditions"), task.executionContract?.stopConditions, "warning"));
  contractSection.append(contractGrid);

  const readySection = detailSection(l10n.t("details.ready"));
  readySection.append(checklist(task.executionContract?.readyToStart));

  const commandsSection = detailSection(l10n.t("details.commands"));
  commandsSection.append(commandList(task.executionContract?.requiredCommands));
  panel.append(primaryGrid, metadataStrip, criteriaSection, contractSection, readySection, commandsSection);
  return panel;
}

function createDependenciesTabPanel(task: BoardTask): HTMLElement {
  const panel = document.createElement("div");
  panel.className = "dependencies-workspace";
  const toolbar = document.createElement("div");
  toolbar.className = "dependencies-toolbar";
  const search = document.createElement("input");
  search.type = "search";
  search.placeholder = l10n.t("dependencies.search");
  search.setAttribute("aria-label", search.placeholder);
  const filters = document.createElement("div");
  filters.className = "dependency-filters";
  const addRelation = dependencyActionButton(l10n.t("dependencies.add"), "plus", "secondary", () => undefined);
  addRelation.disabled = true;
  addRelation.title = l10n.t("dependencies.cliOnly");
  toolbar.append(search, filters, addRelation);

  const body = document.createElement("div");
  body.className = "dependencies-body";
  const tableRegion = document.createElement("div");
  tableRegion.className = "dependency-table-region";
  const table = document.createElement("table");
  table.className = "dependency-table";
  const head = document.createElement("thead");
  const headRow = document.createElement("tr");
  headRow.append(tableHeader(l10n.t("dependencies.task")), tableHeader(l10n.t("dependencies.relation")), tableHeader(l10n.t("dependencies.status")), tableHeader(l10n.t("dependencies.priority")));
  head.append(headRow);
  const tableBody = document.createElement("tbody");
  table.append(head, tableBody);
  tableRegion.append(table);
  const inspector = document.createElement("aside");
  inspector.className = "dependency-inspector";
  body.append(tableRegion, inspector);

  const rows = dependencyRelationshipRows(task);
  let activeFilter = "all";
  let selectedDependency = rows[0]?.taskId;
  const selectDependency = (taskId: string): void => {
    selectedDependency = taskId;
    renderRows();
  };
  const filterDefinitions = [
    { id: "all", label: l10n.t("dependencies.all") },
    { id: "blocking", label: l10n.t("dependencies.blocking") },
    { id: "blocked", label: l10n.t("dependencies.blocked") },
    { id: "related", label: l10n.t("dependencies.related") }
  ];
  for (const definition of filterDefinitions) {
    const filterButton = document.createElement("button");
    filterButton.type = "button";
    filterButton.className = "dependency-filter";
    filterButton.dataset.filter = definition.id;
    filterButton.addEventListener("click", () => {
      activeFilter = definition.id;
      renderRows();
    });
    filters.append(filterButton);
  }
  const renderRows = (): void => {
    const query = search.value.trim().toLocaleLowerCase();
    tableBody.replaceChildren();
    const visible = rows.filter(candidate =>
      (activeFilter === "all" || candidate.category === activeFilter) &&
      (query.length === 0 || `${candidate.taskId} ${candidate.task?.title ?? ""}`.toLocaleLowerCase().includes(query)));
    for (const candidate of visible) {
      const row = document.createElement("tr");
      row.className = candidate.taskId === selectedDependency ? "selected" : "";
      row.tabIndex = 0;
      const taskCell = document.createElement("td");
      taskCell.style.setProperty("--dependency-depth", String(candidate.depth));
      const id = document.createElement("strong");
      id.textContent = candidate.taskId;
      const title = document.createElement("span");
      appendInlineStrong(title, candidate.task?.title ?? l10n.t("dependencies.missingSnapshot"));
      taskCell.append(id, title);
      const relation = document.createElement("td");
      relation.textContent = candidate.relation;
      const status = document.createElement("td");
      status.append(statusDot(candidate.task));
      const priority = document.createElement("td");
      priority.textContent = candidate.task?.priority ?? "—";
      row.append(taskCell, relation, status, priority);
      row.addEventListener("click", () => selectDependency(candidate.taskId));
      row.addEventListener("keydown", event => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          selectDependency(candidate.taskId);
        }
      });
      tableBody.append(row);
    }
    if (visible.length === 0) {
      const row = document.createElement("tr");
      const cell = document.createElement("td");
      cell.colSpan = 4;
      cell.append(emptyState(l10n.t("dependencies.none")));
      row.append(cell);
      tableBody.append(row);
    }
    for (const [index, definition] of filterDefinitions.entries()) {
      const count = rows.filter(candidate => definition.id === "all" || candidate.category === definition.id).length;
      const button = filters.children[index] as HTMLButtonElement;
      button.textContent = `${definition.label}  ${count}`;
      button.classList.toggle("active", definition.id === activeFilter);
    }
    renderDependencyInspector(inspector, rows.find(candidate => candidate.taskId === selectedDependency), task);
  };
  search.addEventListener("input", renderRows);
  panel.append(toolbar, body);
  renderRows();
  return panel;
}

function createActivityTabPanel(task: BoardTask): HTMLElement {
  const panel = document.createElement("div");
  panel.className = "activity-workspace";
  const toolbar = document.createElement("div");
  toolbar.className = "activity-toolbar";
  const kindOptions: ReadonlyArray<readonly [string, string]> = [
    ["", l10n.t("activity.allEvents")],
    ...[...new Set((task.activity ?? []).map(entry => entry.kind))].map(kind => [kind, kind] as const)
  ];
  let selectedKind = "";
  const search = document.createElement("input");
  search.type = "search";
  search.placeholder = l10n.t("activity.search");
  search.setAttribute("aria-label", search.placeholder);
  const systemToggle = document.createElement("label");
  systemToggle.className = "activity-system-toggle";
  const showSystem = document.createElement("input");
  showSystem.type = "checkbox";
  systemToggle.append(document.createTextNode(l10n.t("activity.system")), showSystem);

  const timeline = document.createElement("div");
  timeline.className = "activity-timeline";
  const renderTimeline = (): void => {
    timeline.replaceChildren();
    const query = search.value.trim().toLocaleLowerCase();
    const entries = (task.activity ?? []).filter(entry =>
      (selectedKind.length === 0 || entry.kind === selectedKind) &&
      (showSystem.checked || entry.actorKind !== "Cli" || isCodexAgentActivity(entry)) &&
      (query.length === 0 || `${formatActivityActor(entry)} ${entry.actorId} ${entry.kind} ${formatActivityPayload(entry)}`.toLocaleLowerCase().includes(query)));
    let previousDay = "";
    for (const entry of entries) {
      const day = formatTaskDay(entry.createdAt);
      if (day !== previousDay) {
        const dayHeading = document.createElement("h3");
        dayHeading.textContent = day;
        timeline.append(dayHeading);
        previousDay = day;
      }
      const item = document.createElement("article");
      item.className = "activity-event";
      const time = document.createElement("time");
      time.dateTime = entry.createdAt;
      time.textContent = formatTaskTime(entry.createdAt);
      const avatar = createActivityAvatar(entry);
      const content = document.createElement("div");
      content.className = "activity-event-content";
      const eventHeader = document.createElement("header");
      const actor = document.createElement("strong");
      actor.textContent = formatActivityActor(entry);
      const kind = document.createElement("span");
      kind.className = `activity-kind ${activityKindClass(entry.kind)}`;
      kind.textContent = entry.kind;
      eventHeader.append(actor, kind);
      const payload = document.createElement("div");
      payload.className = "activity-payload";
      const displayPayload = formatActivityPayload(entry);
      if (isMarkdownActivityKind(entry.kind)) {
        appendCommentMarkdown(payload, entry.payload);
      } else {
        appendInlineStrong(payload, displayPayload);
      }
      content.append(eventHeader, payload);
      item.append(time, avatar, content);
      timeline.append(item);
    }
    if (entries.length === 0) {
      timeline.append(emptyState(l10n.t("activity.none")));
    }
  };
  const kindFilter = createActivityKindDropdown(kindOptions, value => {
    selectedKind = value;
    renderTimeline();
  });
  toolbar.append(kindFilter, search, systemToggle);
  search.addEventListener("input", renderTimeline);
  showSystem.addEventListener("change", renderTimeline);

  panel.append(toolbar, timeline);
  renderTimeline();
  return panel;
}

function formatActivityPayload(entry: ActivitySnapshot): string {
  if (entry.kind === "StatusChange") {
    const change = parseStatusChangePayload(entry.payload);
    if (change === undefined) {
      return entry.payload;
    }

    return l10n.t(
      "activity.statusChanged",
      taskStatusPresentation[change.previous].label,
      taskStatusPresentation[change.next].label,
      change.reason);
  }

  if (entry.kind !== "TaskPatched") {
    return entry.payload;
  }

  const fields = taskPatchTopLevelFields(entry.payload);
  if (fields === undefined) {
    return entry.payload;
  }

  if (fields.length === 0) {
    return l10n.t("activity.taskPatchedEmpty");
  }

  const labels = fields.map(taskPatchFieldLabel);
  return l10n.t("activity.taskPatched", labels.join(", "));
}

function taskPatchFieldLabel(field: string): string {
  switch (field) {
    case "title":
      return l10n.t("task.formTitle");
    case "description":
      return l10n.t("dependencies.description");
    case "acceptanceCriteria":
      return l10n.t("details.criteria");
    case "links":
      return l10n.t("overview.linkedFiles");
    case "executionContract":
      return l10n.t("details.contract");
    case "tagIds":
      return l10n.t("details.tags");
    case "relations":
      return l10n.t("details.relatedTasks");
    case "parentTaskUid":
      return l10n.t("dependencies.parent");
    case "priority":
      return l10n.t("details.priority");
    case "status":
      return l10n.t("details.status");
    case "acceptanceState":
      return l10n.t("details.acceptanceState");
    case "deadline":
      return l10n.t("indicator.deadline");
    case "attachments":
      return l10n.t("overview.attachments");
    default:
      return field;
  }
}

function createActivityKindDropdown(
  options: ReadonlyArray<readonly [string, string]>,
  onChange: (value: string) => void): HTMLDivElement {
  const root = document.createElement("div");
  root.className = "activity-kind-dropdown";
  const trigger = document.createElement("button");
  trigger.type = "button";
  trigger.className = "activity-kind-trigger";
  trigger.setAttribute("role", "combobox");
  trigger.setAttribute("aria-label", l10n.t("activity.filter"));
  trigger.setAttribute("aria-haspopup", "listbox");
  trigger.setAttribute("aria-expanded", "false");
  const label = document.createElement("span");
  label.className = "activity-kind-label";
  label.textContent = options[0]?.[1] ?? l10n.t("activity.allEvents");
  trigger.append(label, createChevronIcon("activity-kind-chevron"));

  const popup = document.createElement("div");
  popup.className = "activity-kind-popup";
  popup.setAttribute("role", "listbox");
  popup.hidden = true;
  const popupId = `activity-kind-list-${++activityDropdownSequence}`;
  popup.id = popupId;
  trigger.setAttribute("aria-controls", popupId);

  const optionElements: HTMLButtonElement[] = [];
  let selectedIndex = 0;
  let activeIndex = 0;
  let expanded = false;

  const setActive = (index: number): void => {
    if (optionElements.length === 0) {
      return;
    }

    activeIndex = (index + optionElements.length) % optionElements.length;
    optionElements.forEach((option, optionIndex) => {
      option.classList.toggle("active", optionIndex === activeIndex);
    });
    const activeOption = optionElements[activeIndex]!;
    trigger.setAttribute("aria-activedescendant", activeOption.id);
    activeOption.scrollIntoView({ block: "nearest" });
  };

  const closeOnOutsidePointer = (event: PointerEvent): void => {
    if (event.target instanceof Node && !root.contains(event.target)) {
      close(false);
    }
  };

  const open = (): void => {
    if (expanded || optionElements.length === 0) {
      return;
    }

    expanded = true;
    popup.hidden = false;
    trigger.setAttribute("aria-expanded", "true");
    setActive(selectedIndex);
    document.addEventListener("pointerdown", closeOnOutsidePointer, true);
  };

  const close = (restoreFocus: boolean): void => {
    if (!expanded) {
      return;
    }

    expanded = false;
    popup.hidden = true;
    trigger.setAttribute("aria-expanded", "false");
    trigger.removeAttribute("aria-activedescendant");
    document.removeEventListener("pointerdown", closeOnOutsidePointer, true);
    if (restoreFocus) {
      trigger.focus();
    }
  };

  const select = (index: number): void => {
    selectedIndex = index;
    activeIndex = index;
    const [value, optionLabel] = options[index]!;
    label.textContent = optionLabel;
    optionElements.forEach((option, optionIndex) => {
      option.setAttribute("aria-selected", String(optionIndex === selectedIndex));
    });
    close(true);
    onChange(value);
  };

  options.forEach(([value, optionLabel], index) => {
    const option = document.createElement("button");
    option.type = "button";
    option.className = "activity-kind-option";
    option.id = `${popupId}-option-${index}`;
    option.dataset.value = value;
    option.setAttribute("role", "option");
    option.setAttribute("aria-selected", String(index === selectedIndex));
    option.tabIndex = -1;
    option.textContent = optionLabel;
    option.addEventListener("pointerenter", () => setActive(index));
    option.addEventListener("click", () => select(index));
    optionElements.push(option);
    popup.append(option);
  });

  trigger.addEventListener("click", () => {
    if (expanded) {
      close(false);
    } else {
      open();
    }
  });
  trigger.addEventListener("keydown", event => {
    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        event.stopPropagation();
        if (!expanded) {
          open();
        } else {
          setActive(activeIndex + 1);
        }
        break;
      case "ArrowUp":
        event.preventDefault();
        event.stopPropagation();
        if (!expanded) {
          open();
        } else {
          setActive(activeIndex - 1);
        }
        break;
      case "Home":
        event.preventDefault();
        event.stopPropagation();
        open();
        setActive(0);
        break;
      case "End":
        event.preventDefault();
        event.stopPropagation();
        open();
        setActive(optionElements.length - 1);
        break;
      case "Enter":
      case " ":
        event.preventDefault();
        event.stopPropagation();
        if (expanded) {
          select(activeIndex);
        } else {
          open();
        }
        break;
      case "Escape":
        if (expanded) {
          event.preventDefault();
          event.stopPropagation();
          close(true);
        }
        break;
    }
  });

  root.append(trigger, popup);
  return root;
}

interface DependencyRelationshipRow {
  readonly taskId: string;
  readonly task: BoardTask | undefined;
  readonly relation: string;
  readonly category: "blocking" | "blocked" | "related";
  readonly depth: number;
}

function numberedList(items: readonly string[] | undefined): HTMLElement {
  const list = document.createElement("ol");
  list.className = "reference-numbered-list";
  for (const item of items ?? []) {
    const entry = document.createElement("li");
    const content = document.createElement("span");
    appendInlineStrong(content, item);
    entry.append(content);
    list.append(entry);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("overview.planEmpty")));
  }
  return list;
}

function textList(items: readonly string[]): HTMLElement {
  const list = document.createElement("ul");
  list.className = "reference-text-list";
  for (const item of items) {
    const entry = document.createElement("li");
    appendInlineStrong(entry, item);
    list.append(entry);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("overview.contextEmpty")));
  }
  return list;
}

function createCopyIcon(): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.setAttribute("class", "task-id-copy-icon");
  icon.setAttribute("viewBox", "0 0 16 16");
  icon.setAttribute("aria-hidden", "true");
  icon.setAttribute("focusable", "false");
  const path = document.createElementNS(SVG_NAMESPACE, "path");
  path.setAttribute("d", "M5 5h7v7H5zM3.5 10.5H3a1 1 0 0 1-1-1V3a1 1 0 0 1 1-1h6.5a1 1 0 0 1 1 1v.5");
  icon.append(path);
  return icon;
}

function createTaskIdCopyButton(taskId: string): HTMLButtonElement {
  const button = document.createElement("button");
  const copyLabel = l10n.t("task.copyNumber");
  button.type = "button";
  button.className = "task-id-copy";
  button.setAttribute("aria-label", l10n.t("task.copyNumber"));
  button.title = copyLabel;
  button.append(createCopyIcon());
  button.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(taskId);
    } catch {
      return;
    }
    const copiedLabel = l10n.t("task.numberCopied");
    button.classList.add("copied");
    button.setAttribute("aria-label", copiedLabel);
    button.title = copiedLabel;
    window.setTimeout(() => {
      button.classList.remove("copied");
      button.setAttribute("aria-label", copyLabel);
      button.title = copyLabel;
    }, 1200);
  });
  return button;
}

function createSummaryTags(task: BoardTask): HTMLElement {
  const tags = document.createElement("span");
  tags.className = "card-tags overview-summary-tags";
  for (const tag of resolveTaskTags(task, currentView?.tags ?? [])) {
    tags.append(createTaskTagChip(tag));
  }
  if (tags.childElementCount === 0) {
    tags.append(emptyState(l10n.t("details.notSpecified")));
  }
  return tags;
}

function directRelatedTaskIds(task: BoardTask): readonly string[] {
  const relatedTaskIds: string[] = [];
  const seen = new Set<string>([task.taskId]);
  const append = (taskId: string | null | undefined): void => {
    if (taskId && !seen.has(taskId)) {
      seen.add(taskId);
      relatedTaskIds.push(taskId);
    }
  };
  append(task.parentTaskId);
  for (const dependencyId of task.dependencies) {
    append(dependencyId);
  }
  const allTasks = currentView?.columns.flatMap(column => column.tasks) ?? [];
  for (const candidate of allTasks) {
    if (candidate.parentTaskId === task.taskId || candidate.dependencies.includes(task.taskId)) {
      append(candidate.taskId);
    }
  }
  return relatedTaskIds;
}

function createRelatedTasksList(task: BoardTask): HTMLElement {
  const list = document.createElement("ul");
  list.className = "overview-related-tasks";
  for (const taskId of directRelatedTaskIds(task)) {
    const entry = document.createElement("li");
    entry.append(createRelatedTaskLink(taskId));
    list.append(entry);
  }
  if (list.childElementCount === 0) {
    const entry = document.createElement("li");
    entry.append(emptyState(l10n.t("details.notSpecified")));
    list.append(entry);
  }
  return list;
}

function createRelatedTaskLink(taskId: string): HTMLAnchorElement {
  const link = document.createElement("a");
  link.className = "overview-related-task";
  link.href = `#${encodeURIComponent(taskId)}`;
  const relatedTask = currentView?.columns.flatMap(column => column.tasks)
    .find(candidate => candidate.taskId === taskId);
  appendInlineStrong(link, relatedTask ? `${taskId} — ${relatedTask.title}` : taskId);
  const openLabel = `${l10n.t("dependencies.openTask")}: ${taskId}`;
  link.setAttribute("aria-label", openLabel);
  link.title = openLabel;
  link.addEventListener("click", event => {
    event.preventDefault();
    vscode.postMessage({ type: "openTask", taskId, navigation: "internal" });
  });
  return link;
}

function overviewSummary(task: BoardTask): HTMLElement {
  const list = document.createElement("dl");
  list.className = "overview-summary";
  const taskIdRow = document.createElement("div");
  const taskIdName = document.createElement("dt");
  taskIdName.textContent = l10n.t("details.taskNumber");
  const taskIdValue = document.createElement("dd");
  taskIdValue.className = "task-id-summary-value";
  taskIdValue.append(document.createTextNode(task.taskId), createTaskIdCopyButton(task.taskId));
  taskIdRow.append(taskIdName, taskIdValue);
  const statusRow = document.createElement("div");
  const statusName = document.createElement("dt");
  statusName.textContent = l10n.t("details.status");
  const statusValue = document.createElement("dd");
  statusValue.append(statusDot(task));
  statusRow.append(statusName, statusValue);
  list.append(taskIdRow, statusRow);
  appendDefinition(list, l10n.t("details.priority"), task.priority);
  appendDefinition(list, l10n.t("details.tags"), createSummaryTags(task));
  appendDefinition(list, l10n.t("details.relatedTasks"), createRelatedTasksList(task));
  appendDefinition(list, l10n.t("details.created"), formatTaskDate(task.createdAt));
  return list;
}

function linkedArtifactsList(artifacts: readonly string[]): HTMLElement {
  const list = document.createElement("ul");
  list.className = "artifact-list";
  for (const artifact of artifacts) {
    const entry = document.createElement("li");
    entry.append(createLinkedFileAction(artifact));
    list.append(entry);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("files.none")));
  }
  return list;
}

function attachmentArtifactsList(task: BoardTask): HTMLElement {
  const list = document.createElement("ul");
  list.className = "artifact-list";
  const attachments = task.attachments ?? [];
  const rasterAttachments = attachments.filter(attachment => attachment.previewUri);
  const effectivePreviewId = task.previewAttachmentId ?? rasterAttachments[0]?.attachmentId;
  if (attachments.length > 0 && task.previewAttachmentId) {
    const reset = document.createElement("li");
    reset.className = "overview-cover-reset";
    reset.append(
      createArtifactIcon("image"),
      actionButton(l10n.t("files.automaticCover"), () => vscode.postMessage({
        type: "clearAttachmentPreview",
        taskId: task.taskId,
        expectedTaskRevision: task.revision
      })));
    list.append(reset);
  }
  for (const attachment of task.attachments ?? []) {
    const entry = document.createElement("li");
    entry.className = "overview-attachment";
    const actions = document.createElement("span");
    actions.className = "artifact-actions";
    if (attachment.previewUri) {
      if (attachment.attachmentId === effectivePreviewId) {
        const selected = document.createElement("span");
        selected.className = "attachment-cover-state";
        selected.textContent = l10n.t("files.coverSelected");
        actions.append(selected);
      } else {
        actions.append(actionButton(l10n.t("files.useAsCover"), () => vscode.postMessage({
          type: "setAttachmentPreview",
          taskId: task.taskId,
          attachmentId: attachment.attachmentId,
          expectedTaskRevision: task.revision
        })));
      }
    }
    const remove = actionButton(l10n.t("files.remove"), () => vscode.postMessage({
      type: "removeAttachment",
      taskId: task.taskId,
      attachmentId: attachment.attachmentId,
      expectedTaskRevision: task.revision
    }));
    remove.className = "artifact-remove";
    actions.append(remove);
    entry.append(createAttachmentFileAction(attachment), actions);
    list.append(entry);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("files.attachmentsNone")));
  }
  return list;
}

function createLinkedFileAction(artifact: string): HTMLButtonElement {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "artifact-open";
  button.setAttribute("aria-label", l10n.t("files.openLabel", artifact));
  button.title = l10n.t("files.open", artifact);
  const path = document.createElement("span");
  appendInlineStrong(path, artifact);
  button.append(createArtifactIcon(artifactIconKind(artifact)), path);
  button.addEventListener("click", () => vscode.postMessage({ type: "openFile", path: artifact }));
  return button;
}

function createAttachmentFileAction(attachment: AttachmentSnapshot): HTMLButtonElement {
  const button = document.createElement("button");
  button.type = "button";
  button.className = "artifact-open";
  const actionLabel = attachment.previewUri
    ? l10n.t("files.previewLabel", attachment.displayName)
    : l10n.t("files.openLabel", attachment.displayName);
  button.setAttribute("aria-label", actionLabel);
  button.title = actionLabel;
  const name = document.createElement("span");
  name.textContent = attachment.displayName;
  button.append(createArtifactIcon(artifactIconKind(attachment.displayName, attachment.mediaType)), name);
  button.addEventListener("click", () => {
    if (attachment.previewUri) {
      openAttachmentPreview(attachment, button);
      return;
    }
    vscode.postMessage({ type: "openFile", path: attachment.relativePath });
  });
  return button;
}

function openAttachmentPreview(attachment: AttachmentSnapshot, trigger: HTMLButtonElement): void {
  if (!attachment.previewUri) {
    return;
  }

  const detailsWindow = details.querySelector<HTMLElement>(".details-window");
  if (detailsWindow) {
    detailsWindow.inert = true;
  }

  const overlay = document.createElement("div");
  overlay.className = "attachment-preview-overlay";
  overlay.setAttribute("role", "dialog");
  overlay.setAttribute("aria-modal", "true");
  overlay.setAttribute("aria-labelledby", "attachment-preview-title");

  const preview = document.createElement("section");
  preview.className = "attachment-preview-window";
  const header = document.createElement("header");
  header.className = "attachment-preview-header";
  const title = document.createElement("h2");
  title.id = "attachment-preview-title";
  title.textContent = l10n.t("files.previewTitle", attachment.displayName);
  const close = document.createElement("button");
  close.type = "button";
  close.className = "window-close";
  close.setAttribute("aria-label", l10n.t("files.closePreview"));
  close.title = l10n.t("files.closePreview");
  close.textContent = "×";

  const body = document.createElement("div");
  body.className = "attachment-preview-body";
  const image = document.createElement("img");
  image.className = "attachment-preview-image";
  image.src = attachment.previewUri;
  image.alt = attachment.displayName;
  image.decoding = "async";

  const closePreview = (): void => {
    overlay.remove();
    if (detailsWindow) {
      detailsWindow.inert = false;
    }
    trigger.focus();
  };
  close.addEventListener("click", closePreview);
  overlay.addEventListener("click", event => {
    if (event.target === overlay) {
      closePreview();
    }
  });
  overlay.addEventListener("keydown", event => {
    if (event.key === "Escape") {
      event.preventDefault();
      event.stopPropagation();
      closePreview();
    }
  });

  header.append(title, close);
  body.append(image);
  preview.append(header, body);
  overlay.append(preview);
  details.append(overlay);
  close.focus();
}

function directoryArtifactsList(directories: readonly string[]): HTMLElement {
  const list = document.createElement("ul");
  list.className = "artifact-list";
  for (const directory of directories) {
    const entry = document.createElement("li");
    const path = document.createElement("span");
    appendInlineStrong(path, directory);
    entry.append(createArtifactIcon("folder"), path);
    list.append(entry);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("files.directoriesNone")));
  }
  return list;
}

function commandList(commands: readonly string[] | undefined): HTMLElement {
  const list = document.createElement("div");
  list.className = "command-chips";
  for (const command of commands ?? []) {
    const code = document.createElement("code");
    appendInlineStrong(code, command);
    list.append(code);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("files.commandsNone")));
  }
  return list;
}

function createArtifactIcon(kind: ArtifactIconKind): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.setAttribute("class", "artifact-icon");
  icon.classList.add(`artifact-icon--${kind}`);
  icon.setAttribute("viewBox", "0 0 16 16");
  icon.setAttribute("aria-hidden", "true");
  icon.setAttribute("focusable", "false");
  switch (kind) {
    case "folder":
      appendIconPath(icon, "M1 3.25h5.15l1.35 1.5H15v8.75H1z", true);
      break;
    case "readme":
      appendIconCircle(icon, 8, 8, 6.25);
      appendIconPath(icon, "M8 7v4M8 4.5v.25");
      break;
    case "markdown":
      appendIconPath(icon, "M7 1.5h2v7.35l2.4-2.4 1.4 1.4L8 12.65 3.2 7.85l1.4-1.4L7 8.85z", true);
      break;
    case "json":
      appendIconPath(icon, "M6 2.25H4.75c-.8 0-1.25.5-1.25 1.25v2.25c0 .8-.35 1.25-1 1.25.65 0 1 .45 1 1.25v2.25c0 .75.45 1.25 1.25 1.25H6M10 2.25h1.25c.8 0 1.25.5 1.25 1.25v2.25c0 .8.35 1.25 1 1.25-.65 0-1 .45-1 1.25v2.25c0 .75-.45 1.25-1.25 1.25H10");
      break;
    case "csharp":
      appendIconPath(icon, "M9.2 4.25A4 4 0 1 0 9.2 11.75M10.75 5v6M13 5v6M10 7h4M10 9h4");
      break;
    case "typescript":
      appendIconPath(icon, "M1.75 1.75h12.5v12.5H1.75zM3.25 5h4M5.25 5v6M12.75 5.55c-.5-.45-1.1-.65-1.8-.65-.85 0-1.45.4-1.45 1.05 0 1.7 3.25.85 3.25 2.75 0 .8-.7 1.35-1.75 1.35-.7 0-1.35-.25-1.85-.7");
      break;
    case "css":
      appendIconPath(icon, "M2.75 1.75h10.5l-.9 10.5L8 14.25l-4.35-2zM5 4.75h6L10.75 7h-5.5l.2 2.15h4.95l-.2 2-2.2.75-2.2-.75-.1-1.1");
      break;
    case "project":
      appendIconCircle(icon, 3.25, 8, 1.5);
      appendIconCircle(icon, 12.25, 4, 1.5);
      appendIconCircle(icon, 12.25, 12, 1.5);
      appendIconPath(icon, "M4.75 7.45 10.8 4.7M4.75 8.55l6.05 2.75");
      break;
    case "image":
      appendIconPath(icon, "M1.75 2.25h12.5v11.5H1.75zM2.5 11l3.2-3.4 2.15 2.1 1.45-1.45L13.5 12");
      appendIconCircle(icon, 10.75, 5.25, 1.25);
      break;
    case "archive":
      appendIconPath(icon, "M2 3.25h12v10.5H2zM2 6h12M7 3.25v2.7M9 3.25v2.7M7 8h2v3H7z");
      break;
    case "vsix":
      appendIconPath(icon, "M2 4.5 8 1.75l6 2.75v7L8 14.25 2 11.5zM2 4.5 8 7.25l6-2.75M8 7.25v7M4.75 10.35h2.5M6 9.1v2.5");
      break;
    default:
      appendIconPath(icon, "M3.5 1.5h5l4 4v9h-9zM8.5 1.5v4h4");
      break;
  }
  return icon;
}

function appendIconPath(icon: SVGSVGElement, data: string, filled = false): void {
  const path = document.createElementNS(SVG_NAMESPACE, "path");
  path.setAttribute("d", data);
  if (filled) {
    path.setAttribute("class", "artifact-icon-fill");
  }
  icon.append(path);
}

function appendIconCircle(icon: SVGSVGElement, cx: number, cy: number, radius: number): void {
  const circle = document.createElementNS(SVG_NAMESPACE, "circle");
  circle.setAttribute("cx", String(cx));
  circle.setAttribute("cy", String(cy));
  circle.setAttribute("r", String(radius));
  icon.append(circle);
}

function appendCriteria(
  section: HTMLElement,
  criteria: readonly AcceptanceCriterionSnapshot[] | undefined,
  compact: boolean): void {
  const allCriteria = criteria ?? [];
  const passed = allCriteria.filter(criterion => criterion.state === "Passed").length;
  const progress = document.createElement("span");
  progress.className = "criteria-progress";
  progress.textContent = `${passed}/${allCriteria.length}`;
  section.querySelector("h3")?.append(progress);
  if (compact) {
    const track = document.createElement("div");
    track.className = "criteria-progress-track";
    const value = document.createElement("span");
    value.style.width = allCriteria.length === 0 ? "0" : `${Math.round((passed / allCriteria.length) * 100)}%`;
    track.append(value);
    section.append(track);
  }
  const criteriaList = document.createElement("div");
  criteriaList.className = compact ? "criteria-list compact" : "criteria-list";
  for (const criterion of allCriteria) {
    const row = document.createElement("div");
    row.className = "criterion-row";
    const marker = document.createElement("span");
    marker.className = `criterion-marker ${criterion.state.toLocaleLowerCase()}`;
    marker.setAttribute("aria-label", criterion.state);
    marker.setAttribute("role", "img");
    const text = document.createElement("span");
    appendInlineStrong(text, criterion.description);
    row.append(marker, text);
    criteriaList.append(row);
  }
  if (allCriteria.length === 0) {
    criteriaList.append(emptyState(l10n.t("criteria.none")));
  }
  section.append(criteriaList);
}

function referenceField(label: string, value: string, tone = ""): HTMLElement {
  const field = document.createElement("label");
  field.className = `reference-field ${tone}`.trim();
  const name = document.createElement("span");
  name.className = "reference-field-label";
  name.textContent = label;
  const control = document.createElement("span");
  control.className = "reference-field-value";
  appendInlineStrong(control, value);
  field.append(name, control);
  return field;
}

function checklist(items: readonly string[] | undefined): HTMLElement {
  const list = document.createElement("div");
  list.className = "reference-checklist";
  for (const item of items ?? []) {
    const row = document.createElement("div");
    const marker = document.createElement("span");
    marker.className = "criterion-marker open";
    marker.setAttribute("aria-hidden", "true");
    const text = document.createElement("span");
    appendInlineStrong(text, item);
    row.append(marker, text);
    list.append(row);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("readiness.none")));
  }
  return list;
}

function tableHeader(label: string): HTMLTableCellElement {
  const cell = document.createElement("th");
  cell.scope = "col";
  cell.textContent = label;
  return cell;
}

function dependencyRelationshipRows(task: BoardTask): DependencyRelationshipRow[] {
  const allTasks = currentView?.columns.flatMap(column => column.tasks) ?? [];
  const byId = new Map(allTasks.map(candidate => [candidate.taskId, candidate]));
  const rows: DependencyRelationshipRow[] = [];
  const seen = new Set<string>();
  const append = (
    taskId: string,
    relation: string,
    category: DependencyRelationshipRow["category"],
    depth: number,
    ancestors: ReadonlySet<string>): void => {
    const key = `${category}:${taskId}`;
    if (seen.has(key)) {
      return;
    }
    seen.add(key);
    const dependency = byId.get(taskId);
    rows.push({ taskId, task: dependency, relation, category, depth });
    if (!dependency || ancestors.has(taskId) || category !== "blocking" || taskId === task.taskId) {
      return;
    }
    const nextAncestors = new Set(ancestors).add(taskId);
    for (const nestedId of dependency.dependencies) {
      append(nestedId, l10n.t("dependencies.blocks", dependency.taskId), "blocking", depth + 1, nextAncestors);
    }
  };
  for (const dependencyId of task.dependencies) {
    append(dependencyId, l10n.t("dependencies.blocks", task.taskId), "blocking", 0, new Set([task.taskId]));
  }
  for (const dependent of allTasks.filter(candidate => candidate.dependencies.includes(task.taskId))) {
    append(dependent.taskId, l10n.t("dependencies.blockedBy", task.taskId), "blocked", 0, new Set([task.taskId]));
  }
  if (task.parentTaskId) {
    append(task.parentTaskId, l10n.t("dependencies.parent"), "related", 0, new Set([task.taskId]));
  }
  return rows;
}

function statusDot(task: BoardTask | undefined): HTMLElement {
  const status = document.createElement("span");
  status.className = `dependency-status ${task?.boardStatus ?? task?.status ?? "missing"}`;
  status.textContent = task
    ? taskStatusPresentation[task.boardStatus ?? task.status].label
    : l10n.t("dependencies.missing");
  return status;
}

function renderDependencyInspector(
  inspector: HTMLElement,
  row: DependencyRelationshipRow | undefined,
  currentTask: BoardTask): void {
  inspector.replaceChildren();
  if (!row) {
    inspector.append(emptyState(l10n.t("dependencies.select")));
    return;
  }
  const eyebrow = document.createElement("span");
  eyebrow.className = "dependency-inspector-eyebrow";
  eyebrow.textContent = l10n.t("dependencies.eyebrow");
  const id = document.createElement("h3");
  id.textContent = row.taskId;
  const title = document.createElement("h4");
  appendInlineStrong(title, row.task?.title ?? l10n.t("dependencies.missingSnapshot"));
  const status = statusDot(row.task);
  const metadata = document.createElement("dl");
  appendDefinition(metadata, l10n.t("details.priority"), row.task?.priority ?? "—");
  appendDefinition(metadata, l10n.t("dependencies.component"), row.task?.labels[0] ?? l10n.t("details.notSpecified"));
  const relationHeading = document.createElement("h4");
  relationHeading.textContent = l10n.t("dependencies.currentRelation");
  const relation = document.createElement("p");
  relation.className = row.category === "blocking" ? "dependency-relation blocking" : "dependency-relation";
  relation.textContent = row.relation;
  const relationNote = document.createElement("p");
  relationNote.className = "muted";
  relationNote.textContent = row.category === "blocking"
    ? l10n.t("dependencies.blockingNote", row.taskId)
    : l10n.t("dependencies.relatedNote", currentTask.taskId);
  const descriptionHeading = document.createElement("h4");
  descriptionHeading.textContent = l10n.t("dependencies.description");
  const description = document.createElement("p");
  appendInlineStrong(description, row.task?.description ?? l10n.t("dependencies.descriptionAfterOpen"));
  inspector.append(eyebrow, id, title, status, metadata, relationHeading, relation, relationNote, descriptionHeading, description);
  if (row.task) {
    inspector.append(dependencyActionButton(l10n.t("dependencies.openTask"), "open", "primary", () => vscode.postMessage({ type: "openTask", taskId: row.taskId, navigation: "internal" })));
  }
}

function isMarkdownActivityKind(kind: string): boolean {
  return kind === "Comment" || kind === "AgentSummary";
}

function appendCommentMarkdown(parent: HTMLElement, source: string): void {
  parent.classList.add("comment-markdown");
  for (const node of parseCommentMarkdown(source, l10n)) {
    parent.append(createCommentMarkdownNode(node));
  }
}

function createCommentMarkdownNode(node: CommentMarkdownNode): Node {
  if (node.kind === "text") {
    return document.createTextNode(node.text);
  }
  const element = document.createElement(node.tag);
  if (node.tag === "a" && node.attributes) {
    element.setAttribute("href", node.attributes.href ?? "");
    element.setAttribute("target", node.attributes.target ?? "_blank");
    element.setAttribute("rel", node.attributes.rel ?? "noopener noreferrer");
  }
  for (const child of node.children) {
    element.append(createCommentMarkdownNode(child));
  }
  return element;
}

function formatTaskDay(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.valueOf())
    ? value
    : new Intl.DateTimeFormat(l10n.locale === "ru" ? "ru-RU" : "en-US", { day: "numeric", month: "long", year: "numeric" }).format(date);
}

function formatTaskTime(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.valueOf())
    ? "—"
    : new Intl.DateTimeFormat(l10n.locale === "ru" ? "ru-RU" : "en-US", { hour: "2-digit", minute: "2-digit" }).format(date);
}

function formatActivityActor(entry: ActivitySnapshot): string {
  return isCodexAgentActivity(entry)
    ? "Agent"
    : entry.actorKind ? `${entry.actorKind} / ${entry.actorId}` : entry.actorId;
}

function isCodexAgentActivity(entry: ActivitySnapshot): boolean {
  return entry.actorKind === "Agent" ||
    (entry.actorKind === "Cli" && entry.actorId.toLocaleLowerCase() === "cli");
}

function createActivityAvatar(entry: ActivitySnapshot): HTMLSpanElement {
  const avatar = document.createElement("span");
  avatar.className = "activity-avatar";
  if (isCodexAgentActivity(entry)) {
    avatar.classList.add("agent");
    avatar.append(createCodexIcon());
  } else {
    avatar.textContent = entry.actorId.slice(0, 1).toLocaleUpperCase();
  }
  return avatar;
}

function createCodexIcon(): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.classList.add("activity-agent-icon");
  icon.setAttribute("viewBox", "0 0 24 24");
  icon.setAttribute("fill", "none");
  icon.setAttribute("stroke", "currentColor");
  icon.setAttribute("stroke-linecap", "round");
  icon.setAttribute("stroke-linejoin", "round");
  icon.setAttribute("role", "img");
  icon.setAttribute("aria-label", "Codex");
  icon.setAttribute("focusable", "false");
  const segments = [
    "M11.217 19.384a3.501 3.501 0 0 0 6.783-1.217V13l-6-3.35",
    "M5.214 15.014a3.501 3.501 0 0 0 4.446 5.266L14 17.746V10.8",
    "M6 7.63c-1.391-.236-2.787.395-3.534 1.689a3.474 3.474 0 0 0 1.271 4.745L8 16.578l6-3.348",
    "M12.783 4.616A3.501 3.501 0 0 0 6 5.833V10.9l6 3.45",
    "M18.786 8.986A3.501 3.501 0 0 0 14.34 3.72L10 6.254V13.2",
    "M18 16.302c1.391.236 2.787-.395 3.534-1.689a3.474 3.474 0 0 0-1.271-4.745l-4.308-2.514L10 10.774"
  ];
  for (const data of segments) {
    const path = document.createElementNS(SVG_NAMESPACE, "path");
    path.setAttribute("d", data);
    icon.append(path);
  }
  return icon;
}

function activityKindClass(kind: string): string {
  return kind.toLocaleLowerCase().replace(/[^a-z0-9]+/g, "-");
}

function detailSection(title: string): HTMLElement {
  const section = document.createElement("section");
  section.className = "detail-section";
  const heading = document.createElement("h3");
  heading.textContent = title;
  section.append(heading);
  return section;
}

function appendDefinition(list: HTMLDListElement, term: string, value: string | Node): void {
  const row = document.createElement("div");
  const name = document.createElement("dt");
  name.textContent = term;
  const description = document.createElement("dd");
  if (typeof value === "string") {
    appendInlineStrong(description, value);
  } else {
    description.append(value);
  }
  row.append(name, description);
  list.append(row);
}

function contractCard(title: string, items: readonly string[] | undefined, tone: string): HTMLElement {
  const card = document.createElement("article");
  card.className = `contract-card ${tone}`;
  const heading = document.createElement("h4");
  heading.textContent = title;
  const list = document.createElement("ul");
  for (const item of items ?? []) {
    const entry = document.createElement("li");
    appendInlineStrong(entry, item);
    list.append(entry);
  }
  if (list.childElementCount === 0) {
    list.append(emptyState(l10n.t("common.notSpecified")));
  }
  card.append(heading, list);
  return card;
}

function emptyState(text: string): HTMLSpanElement {
  const empty = document.createElement("span");
  empty.className = "details-empty";
  empty.textContent = text;
  return empty;
}

function appendInlineStrong(parent: HTMLElement, value: string): void {
  const matcher = /`([^`\r\n]+)`/g;
  let cursor = 0;
  for (let match = matcher.exec(value); match; match = matcher.exec(value)) {
    if (match.index > cursor) {
      parent.append(document.createTextNode(value.slice(cursor, match.index)));
    }
    const strong = document.createElement("strong");
    strong.className = "inline-emphasis";
    strong.append(document.createTextNode(match[1]!));
    parent.append(strong);
    cursor = match.index + match[0].length;
  }
  if (cursor < value.length) {
    parent.append(document.createTextNode(value.slice(cursor)));
  }
}

function formatTaskDate(value: string | undefined): string {
  if (!value) {
    return l10n.t("common.notSpecifiedValue");
  }
  const date = new Date(value);
  return Number.isNaN(date.valueOf())
    ? value
    : new Intl.DateTimeFormat(l10n.locale === "ru" ? "ru-RU" : "en-US", { dateStyle: "medium", timeStyle: "short" }).format(date);
}

function createDetailsSkeleton(): HTMLDivElement {
  const skeleton = document.createElement("div");
  skeleton.className = "details-skeleton";
  skeleton.setAttribute("aria-hidden", "true");
  for (const size of ["medium", "full", "full", "short", "full", "medium", "full", "short"]) {
    const line = document.createElement("span");
    line.className = `details-skeleton-line ${size}`;
    skeleton.append(line);
  }
  return skeleton;
}

function renderDetailsError(task: BoardTask, message: string, canGoBack = false): void {
  details.replaceChildren();
  setDetailsVisibility(true);
  const detailsWindow = document.createElement("section");
  detailsWindow.className = "details-window";
  const titleBar = createDetailsTitleBar(task, canGoBack);

  const detailsContent = document.createElement("div");
  detailsContent.className = "details-content details-error";
  const errorMessage = document.createElement("p");
  errorMessage.setAttribute("role", "alert");
  errorMessage.textContent = l10n.t("task.loadFailed", message);
  const retry = actionButton(l10n.t("task.retry"), () => {
    vscode.postMessage({ type: "openTask", taskId: task.taskId, navigation: "internal" });
  });
  detailsContent.append(errorMessage, retry);
  detailsWindow.append(titleBar, detailsContent);
  details.append(detailsWindow);
  retry.focus();
}

function closeTaskDetails(): void {
  vscode.postMessage({ type: "closeTaskDetails" });
  details.replaceChildren();
  setDetailsVisibility(false);
  taskCardToRestoreFocus?.focus();
  taskCardToRestoreFocus = null;
}

function setDetailsVisibility(visible: boolean): void {
  details.hidden = !visible;
  details.inert = !visible;
  details.setAttribute("aria-hidden", String(!visible));
}

function renderDependencyTree(task: BoardTask): HTMLUListElement {
  const list = document.createElement("ul");
  const allTasks = currentView?.columns.flatMap(column => column.tasks) ?? [];
  const appendDependency = (taskId: string, parent: HTMLUListElement, ancestors: ReadonlySet<string>): void => {
    const item = document.createElement("li");
    const dependency = allTasks.find(candidate => candidate.taskId === taskId);
    appendInlineStrong(
      item,
      dependency ? `${dependency.taskId} — ${dependency.title}` : `${taskId} — ${l10n.t("dependencies.missingShort")}`);
    parent.append(item);
    if (!dependency || ancestors.has(taskId) || dependency.dependencies.length === 0) {
      return;
    }

    const children = document.createElement("ul");
    const nextAncestors = new Set(ancestors).add(taskId);
    for (const childId of dependency.dependencies) {
      appendDependency(childId, children, nextAncestors);
    }
    item.append(children);
  };
  for (const dependencyId of task.dependencies) {
    appendDependency(dependencyId, list, new Set([task.taskId]));
  }
  if (task.dependencies.length === 0) {
    const empty = document.createElement("li");
    empty.textContent = l10n.t("common.none");
    list.append(empty);
  }
  return list;
}

function openCreateTaskForm(): void {
  createTaskForm.reset();
  createTaskPending = false;
  clearCreateTaskErrors();
  updateCreateTaskSubmit();
  createTaskDialog.hidden = false;
  createTaskDialog.inert = false;
  createTaskDialog.setAttribute("aria-hidden", "false");
  createTaskWindow.setAttribute("aria-busy", "false");
  createTaskTitle.focus();
}

function closeCreateTaskForm(force = false): void {
  if (createTaskPending && !force) {
    return;
  }

  createTaskPending = false;
  createTaskForm.reset();
  clearCreateTaskErrors();
  updateCreateTaskSubmit();
  createTaskDialog.hidden = true;
  createTaskDialog.inert = true;
  createTaskDialog.setAttribute("aria-hidden", "true");
  createTaskWindow.setAttribute("aria-busy", "false");
  createTaskTrigger.focus();
}

function submitCreateTask(): void {
  if (createTaskPending) {
    return;
  }

  const title = createTaskTitle.value.trim();
  if (!title) {
    updateCreateTaskSubmit();
    createTaskTitle.focus();
    return;
  }
  if (!validateCreateTaskDeadline(true)) {
    updateCreateTaskSubmit();
    createTaskDeadline.focus();
    return;
  }

  clearCreateTaskServerError();
  setCreateTaskPending(true);
  const deadline = createTaskDeadline.value;
  vscode.postMessage({
    type: "create",
    title: createTaskTitle.value.trim(),
    description: createTaskDescription.value,
    priority: createTaskPriority.value,
    ...(deadline ? { deadline } : {})
  });
}

function setCreateTaskPending(pending: boolean): void {
  createTaskPending = pending;
  for (const control of [
    createTaskTitle,
    createTaskDescription,
    createTaskPriority,
    createTaskDeadline,
    createTaskCancel,
    createTaskClose
  ]) {
    control.disabled = pending;
  }
  createTaskWindow.setAttribute("aria-busy", String(pending));
  createTaskSubmit.textContent = l10n.t(pending ? "task.creating" : "task.create");
  updateCreateTaskSubmit();
  if (pending) {
    createTaskWindow.focus();
  }
}

function updateCreateTaskSubmit(): void {
  createTaskSubmit.disabled = createTaskPending ||
    createTaskTitle.value.trim().length === 0 ||
    !validateCreateTaskDeadline(false);
}

function validateCreateTaskDeadline(showError: boolean): boolean {
  const value = createTaskDeadline.value;
  const valid = !createTaskDeadline.validity.badInput && (value.length === 0 || isIsoCalendarDate(value));
  createTaskDeadline.setAttribute("aria-invalid", String(!valid));
  createTaskDeadlineError.textContent = valid ? "" : l10n.t("task.deadlineInvalid");
  createTaskDeadlineError.hidden = valid || !showError;
  return valid;
}

function isIsoCalendarDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false;
  }
  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(Date.UTC(year!, month! - 1, day!));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month! - 1 && date.getUTCDate() === day;
}

function clearCreateTaskErrors(): void {
  createTaskDeadlineError.textContent = "";
  createTaskDeadlineError.hidden = true;
  createTaskDeadline.setAttribute("aria-invalid", "false");
  clearCreateTaskServerError();
}

function clearCreateTaskServerError(): void {
  createTaskError.textContent = "";
  createTaskError.hidden = true;
}

function trapCreateTaskFocus(event: KeyboardEvent): void {
  const controls = [...createTaskWindow.querySelectorAll<HTMLElement>(
    'button:not(:disabled), input:not(:disabled), textarea:not(:disabled), select:not(:disabled)')];
  const first = controls.at(0);
  const last = controls.at(-1);
  if (!first || !last) {
    event.preventDefault();
    createTaskWindow.focus();
    return;
  }

  if (event.shiftKey && document.activeElement === first) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}

function sendMove(task: BoardTask, groupId: string | null, rank: string): void {
  if (!currentView) {
    return;
  }

  vscode.postMessage({
    type: "move",
    taskId: task.taskId,
    groupId,
    rank,
    expectedBoardRevision: currentView.boardRevision
  });
}

function nextRank(tasks: readonly BoardTask[]): string {
  const last = tasks.at(-1)?.rank;
  const numeric = last && /^\d+$/.test(last) ? Number.parseInt(last, 10) : tasks.length * 1000;
  return String(numeric + 1000).padStart(8, "0");
}

function actionButton(text: string, action: () => void): HTMLButtonElement {
  const button = document.createElement("button");
  button.type = "button";
  button.textContent = text;
  button.addEventListener("click", action);
  return button;
}

function dependencyActionButton(
  text: string,
  icon: "plus" | "open",
  tone: "secondary" | "primary",
  action: () => void): HTMLButtonElement {
  const button = document.createElement("button");
  button.type = "button";
  button.className = `dependency-action dependency-action--${tone}`;
  const label = document.createElement("span");
  label.textContent = text;
  button.append(createDependencyActionIcon(icon), label);
  button.addEventListener("click", action);
  return button;
}

function createDependencyActionIcon(kind: "plus" | "open"): SVGSVGElement {
  const icon = document.createElementNS(SVG_NAMESPACE, "svg");
  icon.setAttribute("class", "dependency-action-icon");
  icon.setAttribute("viewBox", "0 0 16 16");
  icon.setAttribute("aria-hidden", "true");
  icon.setAttribute("focusable", "false");
  const path = document.createElementNS(SVG_NAMESPACE, "path");
  path.setAttribute("d", kind === "plus" ? "M8 3v10M3 8h10" : "M4 8h8M9 5l3 3-3 3");
  icon.append(path);
  return icon;
}

function togglePopover(triggerId: string, panelId: string): void {
  const trigger = requiredElement(triggerId);
  const panel = requiredElement(panelId);
  const expanded = trigger.getAttribute("aria-expanded") === "true";
  trigger.setAttribute("aria-expanded", String(!expanded));
  panel.hidden = expanded;
}

function closePopover(triggerId: string, panelId: string): void {
  requiredElement(triggerId).setAttribute("aria-expanded", "false");
  requiredElement(panelId).hidden = true;
}

function requiredElement(id: string): HTMLElement {
  const element = document.getElementById(id);
  if (!element) {
    throw new Error(`Required webview element '${id}' is missing.`);
  }

  return element;
}
