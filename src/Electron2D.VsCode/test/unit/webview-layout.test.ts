/* Electron2D — MIT License — SPDX-License-Identifier: MIT */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";

const css = readFileSync(resolve(process.cwd(), "src/webview.css"), "utf8");
const script = readFileSync(resolve(process.cwd(), "src/webview.ts"), "utf8");
const html = readFileSync(resolve(process.cwd(), "src/webviewHtml.ts"), "utf8");

test("project breadcrumb uses the workspace folder name without duplicate labels", () => {
  assert.match(script, /project\.textContent = typeof message\.project === "string" \? message\.project : ""/);
  assert.match(html, /<h1 id="project"><\/h1>/);
  assert.match(html, /class="project-breadcrumb"/);
  assert.match(html, /class="board-name">\$\{text\("board\.ariaLabel"\)\}<\/span>/);
  assert.doesNotMatch(html, /taskboard \(main\)|<h1>Electron2D Task Board<\/h1>/);
  assert.match(css, /\.project-breadcrumb\s*\{/);
});

test("interactive column controls use centered SVG chevrons", () => {
  assert.match(script, /function createChevronIcon\(className = "chevron-icon"\)/);
  assert.match(script, /document\.createElementNS\(SVG_NAMESPACE, "svg"\)/);
  assert.match(script, /path\.setAttribute\("d", "M4 6l4 4 4-4"\)/);
  assert.doesNotMatch(script, /collapse\.textContent = "[⌄›]"/);
  assert.match(css, /\.chevron-icon\s*{[^}]*transform-box:\s*fill-box;[^}]*transform-origin:\s*center;/s);
  assert.match(css, /\.column-action\[aria-expanded="false"\] \.chevron-icon\s*{[^}]*transform:\s*rotate\(-90deg\);/s);
});

test("primary task creation action has no split menu", () => {
  assert.match(html, /<button id="create" class="primary-button" type="button">\$\{text\("board\.createTask"\)\}<\/button>/);
  assert.doesNotMatch(html, /create-menu-toggle|id="create-menu"|create-secondary|refresh-secondary/);
  assert.match(script, /requiredElement\("create"\)\.addEventListener\("click", \(\) => openCreateTaskForm\(\)\)/);
  assert.doesNotMatch(script, /create-menu-toggle|create-menu|create-secondary|refresh-secondary/);
  assert.doesNotMatch(css, /\.create-group|\.create-menu-toggle|\.create-menu(?:\s|\{|\.)/);
});

test("task creation uses an accessible acknowledged modal instead of prompt chaining", () => {
  assert.match(html, /id="create-dialog"[^>]+aria-hidden="true"[^>]+inert[^>]+hidden/);
  assert.match(html, /class="create-task-window"[^>]+role="dialog"[^>]+aria-modal="true"[^>]+aria-labelledby="create-task-heading"/);
  assert.match(html, /id="create-task-heading">\$\{text\("task\.createDialogTitle"\)\}/);
  assert.match(html, /<label for="create-task-title">\$\{text\("task\.formTitle"\)\}<\/label>[\s\S]*id="create-task-title"[^>]+required[^>]+maxlength="512"/);
  assert.match(html, /<label for="create-task-description">\$\{text\("task\.formDescription"\)\}<\/label>[\s\S]*<textarea id="create-task-description"[^>]+maxlength="262144"/);
  assert.match(html, /id="create-task-priority"[\s\S]*<option value="P0">P0<\/option>[\s\S]*<option value="P2" selected>P2<\/option>[\s\S]*<option value="P3">P3<\/option>/);
  assert.match(html, /id="create-task-deadline" type="date"/);
  assert.match(html, /id="create-task-error"[^>]+role="alert"/);
  assert.match(html, /id="create-task-cancel"[\s\S]*id="create-task-submit"[^>]+disabled/);

  assert.doesNotMatch(script, /window\.prompt/);
  assert.match(script, /requiredElement\("create"\)\.addEventListener\("click", \(\) => openCreateTaskForm\(\)\)/);
  assert.match(script, /let createTaskPending = false/);
  assert.match(script, /if \(createTaskPending\) \{\s*return;/);
  assert.match(script, /title: createTaskTitle\.value\.trim\(\)/);
  assert.match(script, /description: createTaskDescription\.value/);
  assert.match(script, /createTaskDialog\.addEventListener\("click", event => \{\s*if \(event\.target === createTaskDialog\)/);
  assert.match(script, /event\.ctrlKey && event\.key === "Enter"/);
  assert.match(script, /event\.key === "Escape"[\s\S]*closeCreateTaskForm/);
  assert.match(script, /trapCreateTaskFocus\(event\)/);
  assert.match(script, /message\.type === "createResult"/);
  assert.match(script, /message\.ok === true[\s\S]*closeCreateTaskForm\(true\)/);
  assert.match(script, /createTaskError\.textContent = typeof message\.message === "string"/);
  assert.match(script, /createTaskTitle\.focus\(\)/);

  assert.match(css, /\.create-dialog\s*\{[^}]*position:\s*fixed;[^}]*inset:\s*0;[^}]*overflow:\s*auto;/s);
  assert.match(css, /\.create-task-window\s*\{[^}]*width:\s*min\([^;]+;[^}]*max-height:\s*calc\(100vh - 2rem\);[^}]*overflow:\s*auto;/s);
  assert.match(css, /\.create-task-form\s*\{[^}]*display:\s*grid;/s);
  assert.match(css, /@media \(max-width: 620px\)[\s\S]*\.create-task-actions\s*\{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\);/s);
});

test("task board keeps horizontal scrolling inside the visible webview", () => {
  assert.match(css, /body\s*{[^}]*display:\s*grid;[^}]*grid-template-rows:\s*auto minmax\(0,\s*1fr\);[^}]*overflow:\s*hidden;/s);
  assert.match(css, /main\s*{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\);[^}]*min-height:\s*0;[^}]*overflow:\s*hidden;/s);
  assert.doesNotMatch(css, /main:has\(\.details:not\(\[hidden\]\)\)/);
  assert.match(css, /\.board\s*{[^}]*grid-template-columns:\s*repeat\(6,\s*minmax\(calc\(13\.25rem \+ 10px\),\s*20rem\)\);[^}]*min-width:\s*0;[^}]*min-height:\s*0;[^}]*overflow:\s*auto;/s);
  assert.match(css, /\.board\s*{[^}]*scrollbar-gutter:\s*stable;/s);
  assert.doesNotMatch(css, /scrollbar-gutter:\s*stable\s+both-edges/);
});

test("status columns share an adaptive track between the minimum and maximum widths", () => {
  assert.match(css, /\.board\s*{[^}]*grid-template-columns:\s*repeat\(6,\s*minmax\(calc\(13\.25rem \+ 10px\),\s*20rem\)\);/s);
  assert.doesNotMatch(css, /grid-auto-columns:\s*calc\(13\.25rem \+ 10px\)/);
  assert.match(css, /\.board\s*{[^}]*gap:\s*var\(--board-gap\);[^}]*padding:\s*1rem;[^}]*overflow:\s*auto;[^}]*align-items:\s*start;/s);
});

test("column height follows its content while long columns keep board scrolling", () => {
  assert.match(css, /\.board\s*{[^}]*overflow:\s*auto;[^}]*align-items:\s*start;/s);
  assert.match(css, /\.column\s*{[^}]*height:\s*max-content;/s);
  assert.doesNotMatch(css, /\.column\s*{[^}]*min-height:\s*100%;/s);
  assert.doesNotMatch(css, /\.board\s*{[^}]*align-items:\s*stretch;/s);
});

test("board renders flat status columns without epoch or milestone lanes", () => {
  assert.match(script, /columnBody\.append\(createTaskList\(column\.status, undefined, tasks\)\)/);
  assert.doesNotMatch(script, /function createLane\(/);
  assert.doesNotMatch(script, /className = `swimlane/);
  assert.match(script, /groupId === undefined \? task\.groupId : groupId/);
  assert.doesNotMatch(css, /\.swimlane|\.lane-summary|\.milestone-lane/);
});

test("task cards cannot exceed their status column", () => {
  assert.match(css, /\.column,\s*\.card-list\s*{[^}]*min-width:\s*0;/s);
  assert.match(css, /\.card\s*{[^}]*box-sizing:\s*border-box;[^}]*min-width:\s*0;[^}]*max-width:\s*100%;[^}]*overflow-wrap:\s*anywhere;/s);
});

test("task details use a centered modal window with independent scrolling and close controls", () => {
  assert.match(css, /\.details\s*{[^}]*position:\s*fixed;[^}]*inset:\s*0;[^}]*display:\s*grid;[^}]*place-items:\s*center;/s);
  assert.match(css, /\.details\[hidden\]\s*{[^}]*display:\s*none\s*!important;[^}]*pointer-events:\s*none\s*!important;/s);
  assert.match(css, /\.details-window\s*{[^}]*height:\s*min\(58rem,\s*calc\(100vh - 3rem\)\);[^}]*display:\s*grid;[^}]*grid-template-rows:\s*auto minmax\(0,\s*1fr\) auto;[^}]*overflow:\s*hidden;/s);
  assert.match(css, /\.details-content\s*{[^}]*min-height:\s*0;[^}]*overflow:\s*auto;/s);
  assert.match(css, /\.window-close:hover\s*{[^}]*background:\s*var\(--vscode-statusBarItem-errorBackground/s);
  assert.match(script, /detailsWindow\.className = "details-window"/);
  assert.match(script, /close\.setAttribute\("aria-label", l10n\.t\("task\.closeDetails"\)\)/);
  assert.match(script, /event\.key === "Escape"/);
  assert.match(script, /function setDetailsVisibility\(visible: boolean\)/);
  assert.match(script, /details\.inert = !visible/);
  assert.match(script, /details\.setAttribute\("aria-hidden", String\(!visible\)\)/);
  assert.match(script, /taskCardToRestoreFocus\?\.focus\(\)/);
});

test("task details follow the reference header tabs and fixed footer shell", () => {
  assert.match(script, /detailsHeader\.className = "details-header"/);
  assert.match(script, /titleBar\.append\(createDetailsBackButton\(canGoBack\), heading, close\)/);
  assert.doesNotMatch(script, /details-breadcrumb|window-overflow/);
  assert.doesNotMatch(script, /className = "details-metadata"|metadataChip|metadataTagChip|createRelatedTaskChip/);
  assert.match(script, /detailsHeader\.append\(titleBar, tabs\)/);
  assert.match(script, /tabs\.className = "details-tabs"/);
  assert.match(script, /const defaultTaskTabId = "overview";/);
  assert.match(script, /activateTab\(defaultTaskTabId\);/);
  assert.match(script, /candidate\.id === defaultTaskTabId/);
  for (const key of ["overview", "details", "dependencies", "activity"]) {
    assert.match(script, new RegExp(`label: l10n\\.t\\("task\\.tab\\.${key}"\\)`));
  }
  assert.doesNotMatch(script, /label: "Файлы"/);
  assert.match(script, /tab\.setAttribute\("role", "tab"\)/);
  assert.match(script, /panel\.setAttribute\("role", "tabpanel"\)/);
  assert.match(script, /footer\.className = "details-footer"/);
  assert.match(css, /\.details-window\s*{[^}]*grid-template-rows:\s*auto minmax\(0,\s*1fr\) auto;/s);
  assert.match(css, /\.details-tabs\s*{[^}]*display:\s*flex;/s);
  assert.match(css, /\.details-footer\s*{[^}]*display:\s*flex;/s);
  assert.match(css, /\.details\s*{[^}]*background:\s*color-mix\(in srgb,\s*var\(--vscode-editor-background\)/s);
  assert.match(css, /\.details-window\s*{[^}]*box-shadow:[^;]*var\(--vscode-widget-shadow/s);
});

test("task details footer exposes canonical actions for review cancelled and archived tasks", () => {
  const renderer = script.match(/function renderDetails\(task: BoardTask[\s\S]+?\n\}\n\nfunction createTaskTagChip/);
  assert.ok(renderer, "task details renderer must be present");
  assert.match(renderer[0], /if \(task\.archivedAt\)/);
  assert.match(renderer[0], /l10n\.t\("task\.unarchive"\)/);
  assert.match(renderer[0], /const expectedBoardRevision = currentView\.boardRevision/);
  assert.match(renderer[0], /type: "unarchive"[\s\S]*expectedTaskRevision: task\.revision[\s\S]*expectedBoardRevision/);
  assert.match(renderer[0], /else if \(task\.status === "Review"\)/);
  assert.match(renderer[0], /task\.acceptanceCriteria\?\.every\(criterion => criterion\.state === "Passed"\) \?\? true/);
  assert.match(renderer[0], /accept\.className = "details-accept-action"/);
  assert.match(renderer[0], /accept\.disabled = !allCriteriaPassed/);
  assert.match(renderer[0], /footer\.append\(accept\)/);
  assert.match(renderer[0], /else if \(task\.status === "Cancelled"\)/);
  assert.match(renderer[0], /l10n\.t\("task\.archive"\)/);
  assert.match(renderer[0], /type: "archive"[\s\S]*expectedTaskRevision: task\.revision[\s\S]*expectedBoardRevision/);
  assert.match(renderer[0], /archive\.className = "details-destructive-action"/);
  assert.match(renderer[0], /l10n\.t\("task\.reopen"\)/);
  assert.match(renderer[0], /type: "reopen"[\s\S]*reason: l10n\.t\("task\.reopenReason"\)[\s\S]*expectedTaskRevision: task\.revision/);
  assert.match(renderer[0], /reopen\.className = "details-primary-action"/);
  assert.match(renderer[0], /footer\.append\(archive, reopen\)/);
  assert.match(css, /\.details-footer\s*{[^}]*justify-content:\s*flex-end;/s);
  assert.match(css, /\.details-accept-action\s*{[^}]*background:\s*var\(--vscode-testing-iconPassed\);/s);
  assert.match(css, /\.details-accept-action:hover:not\(:disabled\)/);
  assert.match(css, /\.details-primary-action\s*\{[^}]*background:\s*var\(--vscode-button-background\)/s);
  assert.match(css, /\.details-destructive-action\s*\{[^}]*color:\s*var\(--vscode-errorForeground\)/s);
  assert.match(css, /\.details-accept-action:disabled/);
  assert.match(css, /\.details-accept-action:focus-visible[^}]*var\(--vscode-focusBorder\)/s);
});

test("overview summary reuses card tag names colors and compact presentation", () => {
  const summary = script.match(/function overviewSummary\(task: BoardTask\): HTMLElement \{[\s\S]+?\n\}\n\nfunction linkedArtifactsList/);
  assert.ok(summary, "overview summary renderer must be present");
  assert.match(summary[0], /appendDefinition\(list, l10n\.t\("details\.tags"\), createSummaryTags\(task\)\)/);
  assert.match(script, /function createTaskTagChip\(tag: TaskBoardTagSnapshot\): HTMLSpanElement/);
  assert.match(script, /chip\.className = "card-tag"/);
  assert.match(script, /chip\.textContent = tag\.name/);
  assert.match(script, /for \(const tag of resolveTaskTags\(task, currentView\?\.tags \?\? \[\]\)\)/);
  assert.match(script, /tags\.append\(createTaskTagChip\(tag\)\)/);
  assert.match(script, /applyTagAccent\(chip, tag\.color\)/);
  assert.match(script, /style\.setProperty\("--tag-accent", resolveTagAccent\(color\)\)/);
  assert.doesNotMatch(script, /tag-color--\$\{tag\.color\.toLowerCase\(\)\}|card-tag--\$\{tag\.color\.toLowerCase\(\)\}/);
  assert.match(css, /\.card-tag\s*\{[^}]*--tag-accent:/s);
  assert.doesNotMatch(css, /details-chip/);
});

test("task details title bar provides a themed SVG back action", () => {
  assert.match(script, /function createDetailsBackButton\(canGoBack: boolean\)/);
  assert.match(script, /back\.className = "details-back"/);
  assert.match(script, /back\.setAttribute\("aria-label", l10n\.t\("task\.back"\)\)/);
  assert.match(script, /back\.title = l10n\.t\("task\.back"\)/);
  assert.match(script, /back\.disabled = !canGoBack/);
  assert.match(script, /vscode\.postMessage\(\{ type: "navigateBack" \}\)/);
  assert.match(script, /function createBackIcon\(\): SVGSVGElement/);
  assert.match(script, /back\.append\(createBackIcon\(\)\)/);
  assert.doesNotMatch(script, /[←↩] Назад|textContent = "[←↩]"/);
  assert.match(css, /\.details-titlebar\s*{[^}]*display:\s*grid;[^}]*grid-template-columns:\s*2\.35rem minmax\(0,\s*1fr\) 2\.35rem;/s);
  assert.match(css, /\.details-titlebar h2\s*{[^}]*white-space:\s*normal;[^}]*overflow-wrap:\s*anywhere;/s);
  assert.match(css, /\.details-back\s*{[^}]*display:\s*grid;[^}]*place-items:\s*center;[^}]*background:\s*transparent;/s);
  assert.match(css, /\.details-back:hover:not\(:disabled\)/);
  assert.match(css, /\.details-back:disabled/);
  assert.match(css, /\.details-back:focus-visible/);
});

test("overview summary lists direct related tasks as internal links", () => {
  const summary = script.match(/function overviewSummary\(task: BoardTask\): HTMLElement \{[\s\S]+?\n\}\n\nfunction linkedArtifactsList/);
  assert.ok(summary, "overview summary renderer must be present");
  assert.match(summary[0], /appendDefinition\(list, l10n\.t\("details\.relatedTasks"\), createRelatedTasksList\(task\)\)/);

  assert.match(script, /function directRelatedTaskIds\(task: BoardTask\): readonly string\[\]/);
  assert.match(script, /append\(task\.parentTaskId\)/);
  assert.match(script, /for \(const dependencyId of task\.dependencies\)/);
  assert.match(script, /candidate\.parentTaskId === task\.taskId \|\| candidate\.dependencies\.includes\(task\.taskId\)/);
  assert.match(script, /function createRelatedTaskLink\(taskId: string\): HTMLAnchorElement/);
  assert.match(script, /link\.className = "overview-related-task"/);
  assert.match(script, /type: "openTask", taskId, navigation: "internal"/);
  assert.match(css, /\.overview-related-tasks\s*\{[^}]*overflow:\s*auto;/s);
  assert.match(css, /\.overview-related-task\s*{[^}]*color:\s*var\(--vscode-textLink-foreground\);/s);
});

test("task links declare whether navigation resets or extends modal history", () => {
  assert.match(script, /type: "openTask", taskId: task\.taskId, navigation: "direct"/);
  assert.match(script, /type: "openTask", taskId: row\.taskId, navigation: "internal"/);
  assert.match(script, /type: "closeTaskDetails"/);
});

test("overview is the default hydrated task tab", () => {
  assert.match(script, /const defaultTaskTabId = "overview";/);
  assert.match(script, /activateTab\(defaultTaskTabId\)/);
  assert.match(script, /candidate\.id === defaultTaskTabId/);
  assert.doesNotMatch(script, /activateTab\("details"\)/);
});

test("hydrated modal focuses its container without painting focus on the close button", () => {
  assert.match(script, /detailsWindow\.tabIndex = -1/);
  assert.match(script, /detailsWindow\.focus\(\)/);
  assert.match(css, /\.details-window:focus\s*{[^}]*outline:\s*none;/s);
});

test("overview tab follows the reference two-column information architecture", () => {
  assert.match(script, /panel\.className = "details-overview overview-grid"/);
  assert.match(script, /main\.className = "overview-main overview-main-panel"/);
  for (const key of ["goal", "steps", "context", "summary", "linkedFiles", "attachments", "directories"]) {
    assert.match(script, new RegExp(`detailSection\\(l10n\\.t\\("overview\\.${key}"\\)\\)`));
  }
  const overviewRenderer = script.match(/function createOverviewTabPanel\(task: BoardTask\): HTMLElement \{[\s\S]+?\n\}\n\nfunction createDetailsTabPanel/);
  assert.ok(overviewRenderer, "overview renderer must be present");
  assert.doesNotMatch(overviewRenderer[0], /Критерии приёмки|appendCriteria|acceptanceCriteria|Команды|commandList|requiredCommands/);
  assert.match(script, /overviewGoalText\(task, l10n\)/);
  assert.match(script, /task\.executionContract\?\.requiredOutputs/);
  assert.match(script, /classifyLinkedArtifacts\(task\.linkedArtifacts\)/);
  assert.match(script, /linkedArtifactsList\(classifiedArtifacts\.files\)/);
  assert.match(script, /attachmentArtifactsList\(task\)/);
  assert.match(script, /directoryArtifactsList\(classifiedArtifacts\.directories\)/);
  assert.doesNotMatch(script, /commandList\([^)]*linkedArtifacts/);
  assert.match(script, /artifactIconKind\(artifact\)/);
  assert.match(script, /createArtifactIcon\(artifactIconKind\(artifact\)\)/);
  assert.match(script, /createArtifactIcon\(artifactIconKind\(attachment\.displayName, attachment\.mediaType\)\)/);
  assert.match(script, /icon\.classList\.add\(`artifact-icon--\$\{kind\}`\)/);
  assert.doesNotMatch(script, /function createFileIcon\(/);
  assert.doesNotMatch(script, /Показать в проводнике|createExternalLinkIcon|revealWorkspace|artifact-reveal/);
  assert.doesNotMatch(css, /artifact-reveal|external-link-icon/);
  assert.match(script, /overviewSummary\(task\)/);
  assert.match(css, /\.overview-grid\s*{[^}]*grid-template-columns:\s*minmax\(0,\s*1\.5fr\) minmax\(19rem,\s*1fr\);/s);
  assert.match(css, /\.overview-main-panel\s*{[^}]*border:\s*1px solid var\(--vscode-panel-border\);[^}]*background:\s*var\(--vscode-editor-background\);/s);
  assert.match(css, /\.overview-main-panel > \.detail-section\s*{[^}]*border:\s*0;[^}]*background:\s*transparent;/s);
  assert.match(css, /\.details-window\s*{[^}]*width:\s*min\(71rem,[^}]*height:\s*min\(58rem,/s);
});

test("overview contains a full-width task-scoped agent chat", () => {
  const overviewRenderer = script.match(/function createOverviewTabPanel\(task: BoardTask\): HTMLElement \{[\s\S]+?\n\}\n\nfunction createDetailsTabPanel/);
  assert.ok(overviewRenderer, "overview renderer must be present");
  assert.match(overviewRenderer[0], /const chat = createAgentChat\(task\)/);
  assert.match(overviewRenderer[0], /panel\.append\(main, sidebar, chat\)/);
  assert.match(script, /className = "agent-chat"/);
  assert.match(script, /type: "sendAgentMessage"/);
  assert.match(script, /type: "cancelAgentRun"/);
  assert.match(script, /task\.conversation\?\.messages/);
  assert.match(css, /\.agent-chat\s*\{[^}]*grid-column:\s*1\s*\/\s*-1;/s);
  assert.match(css, /\.agent-chat-messages\s*\{[^}]*overflow:\s*auto;/s);
});

test("overview keeps linked and attached files in separate ordered sections", () => {
  const overviewRenderer = script.match(/function createOverviewTabPanel\(task: BoardTask\): HTMLElement \{[\s\S]+?\n\}\n\nfunction createDetailsTabPanel/);
  assert.ok(overviewRenderer, "overview renderer must be present");
  assert.match(
    overviewRenderer[0],
    /sidebar\.append\(summary, linked, attachments, directories\)/);

  const linkedArtifacts = script.match(/function linkedArtifactsList\([\s\S]+?\n\}\n\nfunction attachmentArtifactsList/);
  assert.ok(linkedArtifacts, "linked files must have an independent renderer");
  assert.doesNotMatch(linkedArtifacts[0], /task\.attachments|setAttachmentPreview|clearAttachmentPreview|removeAttachment/);
  assert.match(linkedArtifacts[0], /emptyState\(l10n\.t\("files\.none"\)\)/);

  const attachments = script.match(/function attachmentArtifactsList\([\s\S]+?\n\}\n\nfunction createLinkedFileAction/);
  assert.ok(attachments, "attached files must have an independent renderer");
  assert.match(attachments[0], /for \(const attachment of task\.attachments \?\? \[\]\)/);
  assert.match(attachments[0], /createAttachmentFileAction\(attachment\)/);
  assert.match(attachments[0], /emptyState\(l10n\.t\("files\.attachmentsNone"\)\)/);
  assert.match(script, /type: "openFile", path: attachment\.relativePath/);
});

test("artifact icon renderer has distinct theme-compatible SVG variants for TypeScript, CSS and VSIX", () => {
  const renderer = script.match(/function createArtifactIcon\(kind: ArtifactIconKind\): SVGSVGElement \{[\s\S]+?\n\}\n\nfunction appendIconPath/);
  assert.ok(renderer, "artifact icon renderer must be present");

  const branches = ["typescript", "css", "vsix"].map(kind => {
    const branch = renderer[0].match(new RegExp(`case "${kind}":[\\s\\S]+?break;`));
    assert.ok(branch, `${kind} must have its own SVG branch`);
    assert.match(branch[0], /appendIconPath\(icon,/);
    return branch[0];
  });
  assert.equal(new Set(branches).size, 3, "the new file kinds must not share one generic geometry");
  assert.doesNotMatch(renderer[0], /innerHTML|createElementNS\(SVG_NAMESPACE, "text"\)|fetch\(/);

  assert.match(css, /\.artifact-icon--typescript\s*{[^}]*color:\s*var\(--vscode-charts-blue,/s);
  assert.match(css, /\.artifact-icon--css\s*{[^}]*color:\s*var\(--vscode-charts-purple,/s);
  assert.match(css, /\.artifact-icon--vsix\s*{[^}]*color:\s*var\(--vscode-charts-green,/s);
  assert.doesNotMatch(css, /\.artifact-icon--(?:typescript|css|vsix)\s*{[^}]*#[0-9a-f]{3,8}/is);
});

test("raster attachments open in an internal task board image viewer", () => {
  const action = script.match(/function createAttachmentFileAction\([\s\S]+?\n\}\n\nfunction openAttachmentPreview/);
  assert.ok(action, "attachment action must select the internal raster preview");
  assert.match(action[0], /if \(attachment\.previewUri\) \{[\s\S]+openAttachmentPreview\(attachment, button\)/);
  assert.match(action[0], /vscode\.postMessage\(\{ type: "openFile", path: attachment\.relativePath \}\)/);

  const viewer = script.match(/function openAttachmentPreview\([\s\S]+?\n\}\n\nfunction directoryArtifactsList/);
  assert.ok(viewer, "internal attachment viewer must be present");
  assert.match(viewer[0], /overlay\.className = "attachment-preview-overlay"/);
  assert.match(viewer[0], /overlay\.setAttribute\("role", "dialog"\)/);
  assert.match(viewer[0], /overlay\.setAttribute\("aria-modal", "true"\)/);
  assert.match(viewer[0], /image\.src = attachment\.previewUri/);
  assert.match(viewer[0], /image\.alt = attachment\.displayName/);
  assert.match(viewer[0], /event\.key === "Escape"/);
  assert.match(viewer[0], /event\.target === overlay/);
  assert.match(css, /\.attachment-preview-overlay\s*\{[^}]*position:\s*fixed;[^}]*inset:\s*0;/s);
  assert.match(css, /\.attachment-preview-image\s*\{[^}]*max-width:\s*100%;[^}]*max-height:\s*100%;[^}]*object-fit:\s*contain;/s);
});

test("overview summary shows the task number with an accessible copy action", () => {
  const summary = script.match(/function overviewSummary\(task: BoardTask\): HTMLElement \{[\s\S]+?\n\}\n\nfunction linkedArtifactsList/);
  assert.ok(summary, "overview summary renderer must be present");
  assert.match(summary[0], /taskIdName\.textContent = l10n\.t\("details\.taskNumber"\)/);
  assert.match(summary[0], /taskIdValue\.append\(document\.createTextNode\(task\.taskId\), createTaskIdCopyButton\(task\.taskId\)\)/);
  assert.match(script, /function createTaskIdCopyButton\(taskId: string\): HTMLButtonElement/);
  assert.match(script, /button\.setAttribute\("aria-label", l10n\.t\("task\.copyNumber"\)\)/);
  assert.match(script, /navigator\.clipboard\.writeText\(taskId\)/);
  assert.match(script, /button\.append\(createCopyIcon\(\)\)/);
  assert.match(script, /function createCopyIcon\(\): SVGSVGElement/);
  assert.match(css, /\.task-id-summary-value\s*\{[^}]*display:\s*inline-flex;[^}]*align-items:\s*center;/s);
  assert.match(css, /\.detail-section \.task-id-copy\s*\{[^}]*display:\s*inline-grid;[^}]*place-items:\s*center;[^}]*min-height:\s*0;[^}]*border:\s*0;[^}]*background:\s*none;/s);
  assert.match(css, /\.detail-section \.task-id-copy:hover\s*\{[^}]*background:\s*none;/s);
  assert.match(css, /\.detail-section \.task-id-copy:focus-visible\s*\{[^}]*outline:\s*none;/s);
  assert.match(css, /\.detail-section \.task-id-copy:focus-visible \.task-id-copy-icon\s*\{[^}]*outline:\s*1px solid var\(--vscode-focusBorder\);/s);
});

test("overview summary contains the requested six fields without task type", () => {
  const summary = script.match(/function overviewSummary\(task: BoardTask\): HTMLElement \{[\s\S]+?\n\}\n\nfunction linkedArtifactsList/);
  assert.ok(summary, "overview summary renderer must be present");
  for (const key of ["taskNumber", "status", "priority", "tags", "relatedTasks", "created"]) {
    assert.match(summary[0], new RegExp(`details\\.${key}`));
  }
  assert.doesNotMatch(summary[0], /details\.taskType|executionContract\?\.taskType/);
  assert.match(
    summary[0],
    /list\.append\(taskIdRow, statusRow\);[\s\S]*details\.priority[\s\S]*details\.tags[\s\S]*details\.relatedTasks[\s\S]*details\.created/);
});

test("task details and edit flow omit the assignment concept", () => {
  assert.doesNotMatch(script, /"Назначен"|"Исполнитель"|"Не назначен"|Assignee \(empty clears\)|task\.assignee/);
});

test("details tab renders the reference parameters criteria contract and readiness layout", () => {
  assert.match(script, /createDetailsTabPanel\(task\)/);
  for (const key of ["criteria", "contract", "ready", "commands"]) {
    assert.match(script, new RegExp(`detailSection\\(l10n\\.t\\("details\\.${key}"\\)\\)`));
  }
  assert.match(script, /parameterGrid\.className = "details-parameter-grid"/);
  assert.match(script, /referenceField\(l10n\.t\("details\.status"\)/);
  assert.match(script, /referenceField\(l10n\.t\("details\.updated"\), formatTaskDate\(task\.updatedAt\)\)/);
  assert.match(script, /task\.executionContract\?\.allowedChanges/);
  assert.match(script, /task\.executionContract\?\.forbiddenChanges/);
  assert.match(script, /task\.executionContract\?\.requiredOutputs/);
  assert.match(script, /task\.executionContract\?\.stopConditions/);
  assert.match(script, /task\.executionContract\?\.requiredCommands/);
  assert.match(script, /criterion\.state === "Passed"/);
  assert.match(css, /\.details-primary-grid\s*{[^}]*display:\s*grid;/s);
  assert.match(css, /\.execution-contract-grid\s*{[^}]*grid-template-columns:\s*repeat\(4,\s*minmax\(0,\s*1fr\)\);/s);
  assert.match(css, /var\(--vscode-editorWidget-background/);
});

test("dependencies tab renders searchable relation rows and a selected-task inspector", () => {
  assert.match(script, /panel\.className = "dependencies-workspace"/);
  assert.match(script, /toolbar\.className = "dependencies-toolbar"/);
  assert.match(script, /search\.placeholder = l10n\.t\("dependencies\.search"\)/);
  assert.match(script, /filters\.className = "dependency-filters"/);
  for (const key of ["task", "relation", "status", "priority"]) {
    assert.match(script, new RegExp(`tableHeader\\(l10n\\.t\\("dependencies\\.${key}"\\)\\)`));
  }
  assert.match(script, /inspector\.className = "dependency-inspector"/);
  assert.match(script, /row\.addEventListener\("click", \(\) => selectDependency/);
  assert.match(css, /\.dependencies-body\s*{[^}]*grid-template-columns:\s*minmax\(0,\s*1fr\) minmax\(16rem,\s*\.34fr\);/s);
});

test("dependency actions use SVG icons and a clear secondary-primary hierarchy", () => {
  assert.match(script, /function dependencyActionButton\(/);
  assert.match(script, /function createDependencyActionIcon\(kind: "plus" \| "open"\)/);
  assert.match(script, /dependencyActionButton\(l10n\.t\("dependencies\.add"\), "plus", "secondary"/);
  assert.match(script, /addRelation\.disabled = true/);
  assert.match(script, /dependencyActionButton\(l10n\.t\("dependencies\.openTask"\), "open", "primary"/);
  assert.match(script, /vscode\.postMessage\(\{ type: "openTask", taskId: row\.taskId, navigation: "internal" \}\)/);
  assert.doesNotMatch(script, /[＋+] Добавить связь/);
  assert.match(css, /\.dependency-action\s*{[^}]*display:\s*inline-flex;[^}]*align-items:\s*center;[^}]*min-height:\s*2\.35rem;[^}]*border-radius:\s*6px;/s);
  assert.match(css, /\.dependency-action--secondary\s*{[^}]*background:\s*var\(--vscode-button-secondaryBackground/s);
  assert.match(css, /\.dependency-action--primary\s*{[^}]*width:\s*100%;[^}]*background:\s*var\(--vscode-button-background/s);
  assert.match(css, /\.dependencies-toolbar > \.dependency-action--secondary/);
  assert.match(css, /\.dependency-inspector > \.dependency-action--primary/);
  assert.match(css, /\.dependency-action:focus-visible\s*{[^}]*outline:\s*1px solid var\(--vscode-focusBorder/s);
  assert.match(css, /\.dependency-action:disabled\s*{[^}]*cursor:\s*not-allowed;/s);
});

test("activity tab renders filters and timeline without a comment composer or mutation surface", () => {
  assert.match(script, /panel\.className = "activity-workspace"/);
  assert.match(script, /toolbar\.className = "activity-toolbar"/);
  assert.match(script, /search\.placeholder = l10n\.t\("activity\.search"\)/);
  assert.match(script, /timeline\.className = "activity-timeline"/);
  assert.match(script, /item\.className = "activity-event"/);
  assert.match(script, /panel\.append\(toolbar, timeline\)/);
  assert.doesNotMatch(script, /comment-composer/);
  assert.doesNotMatch(script, /postTaskComment/);
  assert.doesNotMatch(script, /type: "comment"/);
  assert.doesNotMatch(css, /\.comment-composer/);
  assert.doesNotMatch(css, /\.comment-preview/);
  assert.match(css, /\.activity-event\s*{[^}]*grid-template-columns:\s*3\.25rem 2rem minmax\(0,\s*1fr\);/s);
});

test("activity presents current Codex and legacy cli records as Agent", () => {
  assert.match(script, /const avatar = createActivityAvatar\(entry\)/);
  assert.match(script, /function isCodexAgentActivity\(entry: ActivitySnapshot\): boolean/);
  assert.match(script, /entry\.actorKind === "Agent"[\s\S]*entry\.actorKind === "Cli"[\s\S]*entry\.actorId\.toLocaleLowerCase\(\) === "cli"/);
  assert.match(script, /return isCodexAgentActivity\(entry\)[\s\S]*\? "Agent"[\s\S]*: entry\.actorKind \? `\$\{entry\.actorKind\} \/ \$\{entry\.actorId\}` : entry\.actorId;/);
  assert.match(script, /showSystem\.checked \|\| entry\.actorKind !== "Cli" \|\| isCodexAgentActivity\(entry\)/);
  assert.match(script, /function createCodexIcon\(\): SVGSVGElement/);
  assert.match(script, /icon\.setAttribute\("aria-label", "Codex"\)/);
  assert.match(script, /avatar\.classList\.add\("agent"\)/);
  assert.match(script, /if \(isCodexAgentActivity\(entry\)\)[\s\S]*avatar\.append\(createCodexIcon\(\)\);[\s\S]*else \{[\s\S]*avatar\.textContent = entry\.actorId\.slice\(0, 1\)\.toLocaleUpperCase\(\)/);
  assert.match(css, /\.activity-avatar\.agent\s*{[^}]*color:\s*var\(--vscode-foreground\);/s);
  assert.match(css, /\.activity-agent-icon\s*{[^}]*width:\s*1\.1rem;[^}]*height:\s*1\.1rem;/s);
});

test("legacy comments and agent summaries retain safe Markdown rendering", () => {
  assert.match(script, /function isMarkdownActivityKind\(kind: string\): boolean/);
  assert.match(script, /kind === "Comment" \|\| kind === "AgentSummary"/);
  assert.match(script, /appendCommentMarkdown\(payload, entry\.payload\)/);
  assert.match(css, /\.comment-markdown\s*{[^}]*overflow-wrap:\s*anywhere;/s);
  assert.doesNotMatch(script, /\.innerHTML\s*=/);
});

test("TaskPatched activity renders a localized field summary instead of raw JSON Patch", () => {
  assert.match(script, /taskPatchTopLevelFields/);
  assert.match(script, /function formatActivityPayload\(entry: ActivitySnapshot\): string/);
  assert.match(script, /entry\.kind !== "TaskPatched"[\s\S]*return entry\.payload/);
  assert.match(script, /const fields = taskPatchTopLevelFields\(entry\.payload\)/);
  assert.match(script, /fields === undefined[\s\S]*return entry\.payload/);
  assert.match(script, /fields\.length === 0[\s\S]*l10n\.t\("activity\.taskPatchedEmpty"\)/);
  assert.match(script, /l10n\.t\("activity\.taskPatched", labels\.join\(", "\)\)/);
  assert.match(script, /formatActivityPayload\(entry\)[\s\S]*appendInlineStrong\(payload, displayPayload\)/);
  assert.doesNotMatch(script, /JSON\.parse\(entry\.payload\)/);
});

test("StatusChange activity renders localized statuses and decoded reason instead of raw JSON", () => {
  assert.match(script, /parseStatusChangePayload/);
  assert.match(script, /entry\.kind === "StatusChange"[\s\S]*parseStatusChangePayload\(entry\.payload\)/);
  assert.match(script, /taskStatusPresentation\[change\.previous\]\.label/);
  assert.match(script, /taskStatusPresentation\[change\.next\]\.label/);
  assert.match(script, /l10n\.t\(\s*"activity\.statusChanged"[\s\S]*change\.reason/);
  assert.match(script, /change === undefined[\s\S]*return entry\.payload/);
});

test("activity kind filter is a themed accessible dropdown instead of a native select", () => {
  assert.doesNotMatch(script, /document\.createElement\("select"\)/);
  assert.match(script, /function createActivityKindDropdown/);
  assert.match(script, /trigger\.setAttribute\("role", "combobox"\)/);
  assert.match(script, /trigger\.setAttribute\("aria-haspopup", "listbox"\)/);
  assert.match(script, /popup\.setAttribute\("role", "listbox"\)/);
  assert.match(script, /option\.setAttribute\("role", "option"\)/);
  for (const key of ["ArrowDown", "ArrowUp", "Home", "End", "Enter", "Escape"]) {
    assert.match(script, new RegExp(`case "${key}"`));
  }
  assert.match(script, /case " ":/);
  assert.match(script, /document\.addEventListener\("pointerdown", closeOnOutsidePointer, true\)/);
  assert.match(script, /document\.removeEventListener\("pointerdown", closeOnOutsidePointer, true\)/);
  assert.match(script, /selectedKind = value;[\s\S]*renderTimeline\(\)/);
  assert.match(css, /\.activity-kind-trigger\s*{[^}]*background:\s*var\(--vscode-dropdown-background/s);
  assert.match(css, /\.activity-kind-popup\s*{[^}]*box-shadow:\s*0 6px 18px var\(--vscode-widget-shadow/s);
  assert.match(css, /\.activity-kind-option\[aria-selected="true"\]\s*{[^}]*background:\s*var\(--vscode-list-activeSelectionBackground/s);
  assert.doesNotMatch(css, /\.activity-toolbar select/);
});

test("full task model exposes fields required by reference details", () => {
  const model = readFileSync(resolve(process.cwd(), "src/model.ts"), "utf8");
  assert.match(model, /export interface ExecutionContractSnapshot/);
  assert.match(model, /readonly taskUid\?: string/);
  assert.match(model, /readonly createdAt\?: string/);
  assert.match(model, /readonly updatedAt\?: string/);
  assert.match(model, /readonly linkedArtifacts\?: readonly string\[\]/);
  assert.match(model, /readonly parentTaskId\?: string \| null/);
  assert.match(model, /readonly executionContract\?: ExecutionContractSnapshot/);
  assert.match(model, /readonly actorKind\?: string/);
});

test("supporting tab actions use VS Code themed button surfaces", () => {
  assert.match(css, /\.detail-section button\s*{[^}]*border-radius:\s*6px;[^}]*background:\s*var\(--vscode-button-secondaryBackground/s);
});

test("task details consume only the canonical description field", () => {
  const model = readFileSync(resolve(process.cwd(), "src/model.ts"), "utf8");
  assert.match(script, /overviewGoalText\(task, l10n\)/);
  assert.match(model, /const description = task\.description\?\.trim\(\)/);
  assert.doesNotMatch(script, /descriptionMarkdown/);
  assert.doesNotMatch(model, /descriptionMarkdown/);
});

test("task text renders paired backticks as safe strong emphasis", () => {
  assert.match(script, /function appendInlineStrong\(parent: HTMLElement, value: string\): void/);
  assert.match(script, /const matcher = \/`\(\[\^`\\r\\n\]\+\)`\/g/);
  assert.match(script, /document\.createTextNode\(/);
  assert.match(script, /document\.createElement\("strong"\)/);
  assert.match(script, /appendInlineStrong\(heading, task\.title\)/);
  assert.match(script, /appendInlineStrong\(text, criterion\.description\)/);
  assert.match(script, /appendInlineStrong\(entry, item\)/);
  assert.doesNotMatch(script, /\.innerHTML\s*=/);
});

test("status columns use localized labels and explain their lifecycle criteria", () => {
  assert.match(script, /taskStatusPresentation\[column\.status\]/);
  assert.match(script, /heading\.textContent = presentation\.label/);
  assert.match(script, /heading\.title = `\$\{presentation\.label\} — \$\{presentation\.description\}`/);
  assert.doesNotMatch(script, /function formatStatus\(/);
});

test("card drag changes placement only inside its effective status column", () => {
  assert.match(script, /if \(status !== \(task\.boardStatus \?\? task\.status\)\) \{\s*return;\s*\}/);
  assert.match(script, /function sendMove\(task: BoardTask, groupId: string \| null, rank: string\): void/);
  const sendMove = script.match(/function sendMove\([\s\S]+?\n\}/);
  assert.ok(sendMove, "rank-only sendMove must be present");
  assert.doesNotMatch(sendMove[0], /\bstatus\b|expectedTaskRevision/);
  assert.doesNotMatch(script, /event\.altKey/);
});

test("task details show an accessible skeleton without incomplete actions while hydrating", () => {
  assert.match(script, /function renderDetails\(task: BoardTask \| null, loading = false, canGoBack = false\)/);
  assert.match(script, /detailsContent\.setAttribute\("aria-busy", "true"\)/);
  assert.match(script, /loadingStatus\.setAttribute\("role", "status"\)/);
  assert.match(script, /loadingStatus\.textContent = l10n\.t\("task\.loading"\)/);
  assert.match(script, /createDetailsSkeleton\(\)/);
  const loadingBranch = script.match(/if \(loading\) \{([\s\S]+?)return;/);
  assert.ok(loadingBranch, "renderDetails must have a dedicated loading branch");
  assert.doesNotMatch(loadingBranch[1]!, /actionButton|Acceptance criteria|Activity|Attachments|Comment/);
  assert.match(css, /\.details-skeleton-line\s*{[^}]*animation:\s*details-skeleton-pulse/s);
  assert.match(css, /@media \(prefers-reduced-motion:\s*reduce\)/);
  assert.match(script, /function renderDetailsError\(/);
  assert.match(script, /errorMessage\.setAttribute\("role", "alert"\)/);
  assert.match(script, /vscode\.postMessage\(\{ type: "openTask", taskId: task\.taskId, navigation: "internal" \}\)/);
});

test("linked file rows are keyboard-accessible editor actions", () => {
  assert.match(script, /function createLinkedFileAction\(artifact: string\): HTMLButtonElement/);
  assert.match(script, /button\.className = "artifact-open"/);
  assert.match(script, /button\.setAttribute\("aria-label", l10n\.t\("files\.openLabel", artifact\)\)/);
  assert.match(script, /button\.title = l10n\.t\("files\.open", artifact\)/);
  assert.match(script, /vscode\.postMessage\(\{ type: "openFile", path: artifact \}\)/);
  assert.match(script, /entry\.append\(createLinkedFileAction\(artifact\)\)/);
  assert.match(css, /\.artifact-open\s*\{[^}]*display:\s*grid;[^}]*background:\s*transparent;[^}]*color:\s*var\(--vscode-textLink-foreground\);/s);
  assert.match(css, /\.artifact-list\s*\{[^}]*gap:\s*\.2rem;/s);
  assert.match(css, /\.detail-section \.artifact-open\s*\{[^}]*box-sizing:\s*border-box;[^}]*min-width:\s*0;[^}]*max-width:\s*100%;[^}]*width:\s*100%;[^}]*min-height:\s*1\.35rem;[^}]*padding:\s*\.1rem 0;[^}]*background:\s*transparent;/s);
  assert.match(css, /\.detail-section \.artifact-open:hover\s*\{[^}]*background:\s*transparent;/s);
  assert.match(css, /\.artifact-open > span\s*\{[^}]*min-width:\s*0;[^}]*overflow:\s*hidden;[^}]*text-overflow:\s*ellipsis;[^}]*white-space:\s*nowrap;/s);
  assert.match(css, /\.artifact-open:hover\s*\{[^}]*color:\s*var\(--vscode-textLink-activeForeground/s);
  assert.match(css, /\.artifact-open:focus-visible\s*\{[^}]*outline:\s*1px solid var\(--vscode-focusBorder\);/s);
});

test("board presentation matches the reference column and card anatomy", () => {
  assert.match(css, /\.toolbar\s*{[^}]*display:\s*grid;[^}]*grid-template-columns:\s*minmax\(14rem,\s*auto\) minmax\(20rem,\s*1fr\) auto;[^}]*min-height:\s*3\.75rem;/s);
  assert.match(css, /\.project-breadcrumb\s*{[^}]*display:\s*flex;[^}]*align-items:\s*center;/s);
  assert.match(css, /\.brand-mark\s*{[^}]*width:\s*1\.5rem;[^}]*height:\s*1\.5rem;/s);
  assert.match(css, /\.search-shortcut\s*{[^}]*position:\s*absolute;[^}]*right:/s);
  assert.match(css, /\.filter-button\s*{[^}]*display:\s*inline-flex;[^}]*gap:/s);
  assert.match(css, /\.filter-count\s*{[^}]*border-radius:\s*999px;/s);
  assert.match(css, /body\s*{[^}]*grid-template-rows:\s*auto minmax\(0,\s*1fr\);/s);
  assert.doesNotMatch(css, /#notice/);
  assert.match(css, /\.board\s*{[^}]*grid-template-columns:\s*repeat\(6,\s*minmax\(calc\(13\.25rem \+ 10px\),\s*20rem\)\);/s);
  assert.match(css, /\.column::before\s*{[^}]*background:\s*var\(--status-accent\);/s);
  for (const status of ["Ready", "InProgress", "Blocked", "Review", "Done", "Cancelled"]) {
    assert.match(css, new RegExp(`\\.column\\[data-status="${status}"\\]`));
  }
  assert.doesNotMatch(css, /\.column\[data-status="Backlog"\]/);
  assert.doesNotMatch(css, /\.column\[data-status="AwaitingAcceptance"\]/);
  assert.match(css, /\.column-header\s*{[^}]*display:\s*grid;[^}]*grid-template-columns:/s);
  assert.match(css, /\.column-title h2\s*{[^}]*font-size:\s*\.78rem;[^}]*white-space:\s*nowrap;/s);
  assert.match(css, /\.column-action\s*{[^}]*width:\s*1\.35rem;[^}]*height:\s*1\.55rem;/s);
  assert.match(css, /\.column-count\s*{[^}]*border-radius:\s*999px;/s);
  assert.match(css, /\.card-footer\s*{[^}]*display:\s*flex;[^}]*justify-content:\s*space-between;/s);
  assert.match(css, /\.priority-badge\s*{[^}]*border-radius:/s);
  assert.match(css, /\.card-indicators\s*{[^}]*display:\s*flex;/s);
  assert.match(css, /\.show-more\s*{[^}]*color:\s*var\(--vscode-descriptionForeground\);/s);

  assert.match(script, /section\.dataset\.status = column\.status/);
  assert.match(script, /header\.className = "column-header"/);
  assert.match(script, /count\.className = "column-count"/);
  assert.match(script, /collapse\.setAttribute\("aria-expanded", "true"\)/);
  assert.match(script, /columnBody\.append\(createTaskList\(column\.status, undefined, tasks\)\)/);
  assert.doesNotMatch(script, /function createLane\(/);
  assert.doesNotMatch(css, /\.swimlane|\.lane-summary|\.milestone-lane/);
  assert.match(script, /task\.acceptanceState === "Submitted"/);
  assert.match(script, /l10n\.t\("indicator\.awaitingDecision"\)/);
  assert.match(script, /footer\.className = "card-footer"/);
  assert.match(script, /priority\.className = "priority-badge"/);
  assert.match(script, /indicators\.className = "card-indicators"/);
  assert.match(script, /const CARD_PREVIEW_LIMIT = 6/);
  assert.match(script, /more\.textContent = l10n\.t\("board\.moreTasks", tasks\.length - visible\.length\)/);
});

test("column count badge centers filtered task totals without glyph-specific offsets", () => {
  const countRule = css.match(/\.column-count\s*{[^}]*}/s);
  assert.ok(countRule, "column count rule must be present");
  assert.match(css, /\.column-count\s*{[^}]*display:\s*inline-grid;[^}]*place-items:\s*center;/s);
  assert.match(css, /\.column-count\s*{[^}]*min-width:\s*1\.15rem;[^}]*padding:\s*\.05rem \.3rem;[^}]*border-radius:\s*999px;/s);
  assert.match(css, /\.column-count\s*{[^}]*line-height:\s*1;/s);
  assert.doesNotMatch(countRule[0], /(?:^|[;\n])\s*(?:transform|translate|margin-top|top|height):/);
  assert.match(script, /count\.textContent = String\(tasks\.length\)/);
  assert.doesNotMatch(script, /count\.textContent[\s\S]{0,120}(?:padStart|Math\.|===\s*["']?(?:0|4|29|913))/);
});

test("blocked card indicator uses an aligned themed SVG instead of a Unicode glyph", () => {
  assert.doesNotMatch(script, /createCardIndicator\("△"/);
  assert.match(script, /createCardIndicator\(createBlockerIndicatorIcon\(\), task\.dependencies\.length \|\| 1, "warning"/);
  const helper = script.match(/function createBlockerIndicatorIcon\(\): SVGSVGElement \{[\s\S]+?\n\}/);
  assert.ok(helper, "blocker SVG helper must be present");
  assert.match(helper[0], /document\.createElementNS\(SVG_NAMESPACE, "svg"\)/);
  assert.match(helper[0], /icon\.setAttribute\("viewBox", "0 0 16 16"\)/);
  assert.match(helper[0], /icon\.setAttribute\("aria-hidden", "true"\)/);
  assert.doesNotMatch(helper[0], /innerHTML|textContent|△/);
  assert.match(script, /indicator\.setAttribute\("aria-label", title\)/);
  assert.match(css, /\.card-indicator\s*\{[^}]*display:\s*inline-flex;[^}]*align-items:\s*center;[^}]*gap:/s);
  assert.match(css, /\.card-indicator svg\s*\{[^}]*width:\s*\.75rem;[^}]*height:\s*\.75rem;[^}]*stroke:\s*currentColor;/s);
});

test("toolbar spans the webview while the board keeps its own scrolling inset", () => {
  assert.match(css, /body\s*{[^}]*margin:\s*0;[^}]*padding:\s*0;[^}]*overflow:\s*hidden;/s);
  assert.match(css, /\.toolbar\s*{[^}]*box-sizing:\s*border-box;/s);
  assert.match(css, /\.board\s*{[^}]*padding:\s*1rem;[^}]*overflow:\s*auto;/s);
});

test("cards render only available global tags, deadline, checklist progress and real attachment count", () => {
  assert.match(script, /resolveTaskTags\(task, currentView\?\.tags \?\? \[\]\)/);
  assert.match(script, /className = "card-tags"/);
  assert.match(script, /className = "card-tag"/);
  assert.match(script, /applyTagAccent\(chip, tag\.color\)/);
  assert.match(script, /task\.deadline/);
  assert.match(script, /progress\.passed/);
  assert.match(script, /attachmentCount/);
  assert.doesNotMatch(script, /createCardIndicator\("⌗", task\.labels\.length/);
  assert.match(css, /\.card-tags\s*\{[^}]*display:\s*flex;[^}]*flex-wrap:\s*wrap;/s);
  assert.match(css, /\.card-tag\s*\{[^}]*border-radius:\s*999px;/s);
  assert.match(css, /\.card-metadata\s*\{[^}]*display:\s*flex;/s);
});

test("card tag and priority badges center their labels without font baseline drift", () => {
  for (const selector of ["card-tag", "priority-badge"]) {
    const rule = new RegExp(`\\.${selector}\\s*\\{[^}]*display:\\s*inline-flex;[^}]*align-items:\\s*center;[^}]*justify-content:\\s*center;[^}]*box-sizing:\\s*border-box;[^}]*min-height:\\s*1rem;[^}]*line-height:\\s*1;`, "s");
    assert.match(css, rule);
  }

  assert.match(css, /\.card-footer\s*\{[^}]*display:\s*flex;[^}]*align-items:\s*center;/s);
});

test("tag accents share one restrained oklch tonal scale", () => {
  assert.match(
    css,
    /\.card-tag\s*\{[^}]*--tag-accent:\s*var\(--vscode-descriptionForeground\);[^}]*--tag-foreground:\s*color-mix\(in oklch,\s*var\(--vscode-foreground\) 52%,\s*var\(--tag-accent\) 48%\);[^}]*--tag-border:\s*color-mix\(in oklch,\s*var\(--vscode-panel-border\) 56%,\s*var\(--tag-accent\) 44%\);[^}]*--tag-surface:\s*color-mix\(in oklch,\s*var\(--vscode-editor-background\) 86%,\s*var\(--tag-accent\) 14%\);/s);
  assert.match(css, /\.card-tag\s*\{[^}]*border:\s*1px solid var\(--tag-border\);[^}]*color:\s*var\(--tag-foreground\);[^}]*background:\s*var\(--tag-surface\);/s);
  assert.doesNotMatch(css, /details-chip/);
  assert.doesNotMatch(css, /color-mix\(in srgb,\s*var\(--tag-accent\)/);
});

test("task number and priority form a compact left group in the bottom card row", () => {
  const renderer = script.match(/function createTaskCard\(task: BoardTask\): HTMLButtonElement \{[\s\S]+?\n\}/);
  assert.ok(renderer, "task card renderer must be present");
  assert.doesNotMatch(renderer[0], /body\.append\(id\)/);
  assert.match(renderer[0], /identity\.className = "card-identity"/);
  assert.match(renderer[0], /identity\.append\(id,\s*priority\)/);
  assert.match(renderer[0], /footer\.append\(identity,\s*metadata,\s*indicators\)/);
  assert.match(renderer[0], /body\.append\(title,\s*footer\)/);
  assert.match(css, /\.card-identity\s*\{[^}]*display:\s*inline-flex;[^}]*flex:\s*0 0 auto;[^}]*align-items:\s*center;[^}]*gap:\s*\.35rem;/s);
});

test("cards fit the complete raster cover to their width and task files can select the preview image", () => {
  assert.match(script, /task\.cardPreview\?\.previewUri/);
  assert.match(script, /cover\.className = "task-card-cover"/);
  assert.match(script, /cover\.alt = task\.cardPreview\.displayName/);
  assert.match(script, /body\.className = "task-card-body"/);
  assert.match(script, /type: "setAttachmentPreview"/);
  assert.match(script, /type: "clearAttachmentPreview"/);
  const attachments = script.match(/function attachmentArtifactsList\([\s\S]+?\n}\n\nfunction createLinkedFileAction/);
  assert.ok(attachments, "cover controls must be rendered in the attached files section");
  assert.match(attachments[0], /type: "setAttachmentPreview"/);
  assert.match(attachments[0], /type: "clearAttachmentPreview"/);
  assert.match(script, /l10n\.t\("files\.useAsCover"\)/);
  assert.match(script, /l10n\.t\("files\.automaticCover"\)/);
  assert.match(css, /\.card\s*\{[^}]*padding:\s*0;/s);
  assert.match(css, /\.task-card-cover\s*\{[^}]*width:\s*100%;[^}]*height:\s*auto;[^}]*object-fit:\s*contain;/s);
  assert.doesNotMatch(css, /\.task-card-cover\s*\{[^}]*height:\s*8rem;/s);
  assert.doesNotMatch(css, /\.task-card-cover\s*\{[^}]*object-fit:\s*cover;/s);
  assert.match(css, /\.task-card-body\s*\{[^}]*width:\s*100%;[^}]*padding:\s*\.55rem \.6rem;/s);
});

test("board settings manage the global tag catalog without a task-details footer action", () => {
  assert.match(html, /id="tag-settings"[^>]*aria-label=/);
  assert.match(html, /<svg class="settings-icon"/);
  assert.match(script, /type: "manageTags"/);
  assert.doesNotMatch(script, /type: "addTaskTag"/);
  assert.match(css, /\.settings-icon\s*\{[^}]*stroke:\s*currentColor;/s);
  assert.match(css, /\.toolbar-actions\s*\{[^}]*grid-template-columns:\s*repeat\(5,\s*auto\);/s);
});

test("board and modal accents come from the VS Code theme without a literal palette", () => {
  const expectedThemeVariables = {
    Ready: "charts-purple",
    InProgress: "charts-blue",
    Blocked: "charts-red",
    Review: "editorWarning-foreground",
    Done: "testing-iconPassed",
    Cancelled: "disabledForeground"
  } as const;
  for (const [status, variable] of Object.entries(expectedThemeVariables)) {
    assert.match(css, new RegExp(`\\.column\\[data-status="${status}"\\] \\{ --status-accent: var\\(--vscode-${variable}`));
  }
  assert.doesNotMatch(css, /\.column\[data-status="[^"]+"\][^\n]*#[0-9a-f]{3,8}/i);
  assert.doesNotMatch(css, /var\(--vscode-testing-icon(?:Failed|Passed),\s*#[0-9a-f]{3,8}/i);
});

test("reference toolbar combines keyboard search priority and accessible catalog tag multi-select", () => {
  assert.match(script, /const filterToggle = requiredElement\("filter-toggle"\)/);
  assert.match(script, /filterToggle\.addEventListener\("click"/);
  assert.match(script, /const selectedTagIds = new Set<string>\(\)/);
  assert.match(script, /const tagFilterTrigger = requiredElement\("tag-filter-trigger"\) as HTMLButtonElement/);
  assert.match(script, /const tagFilterPopup = requiredElement\("tag-filter-popup"\)/);
  assert.match(script, /function updateFilterCount\(\): void/);
  assert.match(script, /Number\(priorityFilter\.value !== ""\) \+ Number\(selectedTagIds\.size > 0\)/);
  assert.match(script, /filterCount\.hidden = activeCount === 0/);
  assert.match(script, /priorityFilter\.addEventListener\("change", \(\) => \{[\s\S]*updateFilterCount\(\);[\s\S]*renderBoard\(\);[\s\S]*\}\)/);
  assert.match(script, /function toggleTagFilterSelection\(tagId: string\): void/);
  assert.match(script, /selectedTagIds\.has\(tagId\)[\s\S]*selectedTagIds\.delete\(tagId\)[\s\S]*selectedTagIds\.add\(tagId\)/);
  assert.match(script, /function synchronizeTagFilter\(tags: readonly TaskBoardTagSnapshot\[\]\): void/);
  assert.match(script, /const availableTagIds = new Set\(tags\.map\(tag => tag\.tagId\)\)/);
  assert.match(script, /selectedTagIds\.delete\(tagId\)/);
  assert.match(script, /option\.setAttribute\("role", "option"\)/);
  assert.match(script, /option\.setAttribute\("aria-selected", String\(selectedTagIds\.has\(tag\.tagId\)\)\)/);
  assert.match(script, /optionLabel\.textContent = tag\.name/);
  assert.match(script, /synchronizeTagFilter\(currentView\.tags\)/);
  assert.match(script, /event\.ctrlKey && event\.key\.toLocaleLowerCase\(\) === "f"/);
  assert.match(script, /matchesTaskFilters\(task, query, priorityFilter\.value, selectedTagIds\)/);
  assert.match(script, /document\.addEventListener\("pointerdown", closeFilterOnOutsidePointer, true\)/);
  assert.match(script, /function closeFilterOnOutsidePointer\(event: PointerEvent\): void/);
  assert.match(script, /!filterToggle\.contains\(event\.target\) && !filterPanel\.contains\(event\.target\)/);
  assert.match(script, /closeTagFilter\(false\);[\s\S]*closePopover\("filter-toggle", "filter-panel"\)/);
  for (const key of ["ArrowDown", "ArrowUp", "Home", "End", "Enter", "Escape"]) {
    assert.match(script, new RegExp(`case "${key}"`));
  }
  assert.match(script, /case " ":/);
  assert.match(css, /\.filter-panel select \+ label\s*\{[^}]*margin-top:\s*\.65rem;/s);
  assert.match(css, /\.tag-filter-popup\s*\{[^}]*max-height:\s*15rem;[^}]*overflow:\s*auto;/s);
  assert.match(css, /\.tag-filter-option\[aria-selected="true"\]\s*\{[^}]*background:\s*var\(--vscode-list-activeSelectionBackground/s);
});

test("filter popover resets priority and tags without closing or changing independent toolbar controls", () => {
  assert.match(html, /<button id="reset-filters" class="secondary-control filter-reset" type="button" disabled>\$\{text\("board\.resetFilters"\)\}<\/button>/);
  assert.match(script, /const resetFilters = requiredElement\("reset-filters"\) as HTMLButtonElement/);
  assert.match(script, /resetFilters\.addEventListener\("click", resetPanelFilters\)/);
  assert.match(script, /resetFilters\.disabled = activeCount === 0/);

  const start = script.indexOf("function resetPanelFilters(): void");
  const end = script.indexOf("\n}", start);
  assert.notEqual(start, -1, "resetPanelFilters must exist");
  assert.notEqual(end, -1, "resetPanelFilters must have a complete body");
  const resetFunction = script.slice(start, end + 2);
  assert.match(resetFunction, /priorityFilter\.value = ""/);
  assert.match(resetFunction, /selectedTagIds\.clear\(\)/);
  assert.match(resetFunction, /option\.setAttribute\("aria-selected", "false"\)/);
  assert.match(resetFunction, /updateTagFilterSummary\(currentView\?\.tags \?\? \[\]\)/);
  assert.match(resetFunction, /updateFilterCount\(\)/);
  assert.match(resetFunction, /renderBoard\(\)/);
  assert.doesNotMatch(resetFunction, /closePopover|filter\.value|showArchived/);

  assert.match(css, /\.filter-reset\s*\{[^}]*width:\s*100%;[^}]*margin-top:\s*\.75rem;/s);
  assert.match(css, /\.filter-reset:disabled\s*\{[^}]*cursor:\s*default;/s);
});
