/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import test from "node:test";
import { buildBoardView, moveTask, taskStatuses, taskStatusPresentationFor } from "../../src/model.js";
import { createLocalizer } from "../../src/localization.js";
import * as model from "../../src/model.js";

test("task status presentation provides localized labels and lifecycle criteria", () => {
  const presentation = (model as unknown as {
    taskStatusPresentation?: Record<string, { label: string; description: string }>;
  }).taskStatusPresentation;

  assert.ok(presentation, "model must expose localized status presentation");
  assert.deepEqual(
    taskStatuses.map(status => presentation[status]?.label),
    [
      "Готово к работе",
      "В работе",
      "Заблокировано",
      "На проверке",
      "Завершено",
      "Отменено"
    ]
  );
  assert.match(presentation.Ready?.description ?? "", /по умолчанию/i);
  assert.match(presentation.Blocked?.description ?? "", /зависимост|ручн/i);
  assert.equal(Object.hasOwn(presentation, "Backlog"), false);
  assert.match(presentation.Done?.description ?? "", /доверенн/i);
  const english = taskStatusPresentationFor(createLocalizer("en"));
  assert.equal(english.Ready.label, "Ready");
  assert.match(english.Blocked.description, /dependencies|manual/i);
});

test("buildBoardView always returns the fixed six status columns", () => {
  const view = buildBoardView({ board: { revision: 4, groups: [], placements: [] }, tasks: [] });

  assert.deepEqual(view.columns.map(column => column.status), taskStatuses);
  assert.equal(view.boardRevision, 4);
});

test("buildBoardView uses effective board status without changing canonical task status", () => {
  const view = buildBoardView({
    board: { revision: 1, groups: [], placements: [{ taskId: "T-1", groupId: null, rank: "00001000" }] },
    tasks: [{
      taskId: "T-1",
      revision: 1,
      title: "Waiting for dependency",
      status: "Ready",
      boardStatus: "Blocked",
      readiness: "BlockedByDependencies",
      priority: "P1",
      labels: [],
      dependencies: ["T-0"]
    }]
  });

  const task = view.columns.find(column => column.status === "Blocked")?.tasks[0];
  assert.equal(task?.taskId, "T-1");
  assert.equal(task?.status, "Ready");
  assert.equal(task?.boardStatus, "Blocked");
  assert.equal(view.columns.find(column => column.status === "Ready")?.tasks.length, 0);
});

test("buildBoardView keeps group and rank ordering without duplicating task status", () => {
  const view = buildBoardView({
    board: {
      revision: 5,
      groups: [{ groupId: "epoch-1", kind: "Epoch", title: "Epoch One", rank: "00001000", parentGroupId: null }],
      placements: [
        { taskId: "T-2", groupId: "epoch-1", rank: "00002000" },
        { taskId: "T-1", groupId: "epoch-1", rank: "00001000" }
      ]
    },
    tasks: [
      { taskId: "T-2", revision: 2, title: "Second", status: "Ready", priority: "P2", labels: [], dependencies: [] },
      { taskId: "T-1", revision: 3, title: "First", status: "Ready", priority: "P1", labels: [], dependencies: [] }
    ]
  });

  assert.deepEqual(view.columns.find(column => column.status === "Ready")?.tasks.map(task => task.taskId), ["T-1", "T-2"]);
  assert.equal(Object.hasOwn(view.columns[0]!, "statusByTask"), false);
});

test("moveTask creates a board-revision-aware placement intent without lifecycle state", () => {
  const intent = moveTask({ taskId: "T-7" }, "milestone-1", "00003000", 12);

  assert.deepEqual(intent, {
    command: "move",
    taskId: "T-7",
    groupId: "milestone-1",
    rank: "00003000",
    expectedBoardRevision: 12
  });
});

test("overview presentation extracts the real migrated self-contained section and file paths", () => {
  const api = model as unknown as {
    overviewGoalText?: (task: { taskId: string; title: string; description?: string }) => string;
    linkedFileArtifacts?: (artifacts: readonly string[] | undefined) => readonly string[];
  };
  assert.equal(typeof api.overviewGoalText, "function");
  assert.equal(typeof api.linkedFileArtifacts, "function");
  const migratedDescription = [
    "## T-0983 [ ] P1: Нормализовать method signatures в generated API manifest",
    "",
    "- Создана: 2026-07-06T03:26:00+03:00",
    "### Execution contract",
    "- Task type: production",
    "### Самодостаточное описание",
    "",
    "Control audit нашёл follow-up в generated API manifest.",
    "",
    "Ожидаемый результат: generator пишет один канонический signature format.",
    "",
    "### Internal substrate acceptance contract",
    "- Must not change: public API"
  ].join("\n");

  assert.equal(
    api.overviewGoalText!({ taskId: "T-0983", title: "Нормализовать signatures", description: migratedDescription }),
    "Control audit нашёл follow-up в generated API manifest.\n\nОжидаемый результат: generator пишет один канонический signature format.");
  assert.equal(
    api.overviewGoalText!({ taskId: "T-2000", title: "Обычная задача", description: "Короткое canonical описание." }),
    "Короткое canonical описание.");
  assert.deepEqual(api.linkedFileArtifacts!([
    "data/api/electron2d-api-manifest.json",
    "docs/documentation/api-manifest.md",
    "dotnet build eng/Electron2D.Build/Electron2D.Build.csproj --no-restore",
    "update api-manifest --wiki-path .github/wiki --check",
    "eng/Electron2D.ApiManifestGenerator/Program.cs",
    "tests/Electron2D.Tests.Integration/ApiManifestTests.cs"
  ]), [
    "data/api/electron2d-api-manifest.json",
    "docs/documentation/api-manifest.md",
    "eng/Electron2D.ApiManifestGenerator/Program.cs",
    "tests/Electron2D.Tests.Integration/ApiManifestTests.cs"
  ]);
});

test("linked artifacts use Explorer-like icon kinds from names, extensions and media types", () => {
  const iconKind = (model as unknown as {
    artifactIconKind?: (path: string, mediaType?: string) => string;
  }).artifactIconKind;
  assert.equal(typeof iconKind, "function", "model must expose deterministic file icon semantics");

  assert.equal(iconKind!("data/api/"), "folder");
  assert.equal(iconKind!("README.md"), "readme");
  assert.equal(iconKind!("docs/AGENTS.md"), "markdown");
  assert.equal(iconKind!("src/Electron2D/Program.cs"), "csharp");
  assert.equal(iconKind!("src/Electron2D/Electron2D.csproj"), "project");
  assert.equal(iconKind!("src/Electron2D.VsCode/src/webview.ts"), "typescript");
  assert.equal(iconKind!("src/Electron2D.VsCode/src/component.TSX"), "typescript");
  assert.equal(iconKind!("src/Electron2D.VsCode/src/module.mts"), "typescript");
  assert.equal(iconKind!("src/Electron2D.VsCode/src/common.CTS"), "typescript");
  assert.equal(iconKind!("src/Electron2D.VsCode/src/webview.CSS"), "css");
  assert.equal(iconKind!("artifacts/electron2d-taskboard.VSIX"), "vsix");
  assert.equal(iconKind!("project.e2d.json"), "json");
  assert.equal(iconKind!(".taskboard/tasks/T-1179.e2task"), "json");
  assert.equal(iconKind!("assets/logo.png"), "image");
  assert.equal(iconKind!("artifact-without-extension", "application/zip"), "archive");
  assert.equal(iconKind!("LICENSE"), "file");
});

test("linked artifact presentation separates files, directories and ignored values", () => {
  const classify = (model as unknown as {
    classifyLinkedArtifacts?: (artifacts: readonly string[] | undefined) => {
      files: readonly string[];
      directories: readonly string[];
      ignored: readonly string[];
    };
  }).classifyLinkedArtifacts;
  assert.equal(typeof classify, "function", "model must classify linked artifacts before rendering");

  assert.deepEqual(classify!([
    "docs/testing/harness.md",
    "data/api/",
    "src/Electron2D/",
    "dotnet run --project eng/Electron2D.Build -- verify docs",
    "artifact://screenshots/frame.png",
    "not classified"
  ]), {
    files: ["docs/testing/harness.md"],
    directories: ["data/api/", "src/Electron2D/"],
    ignored: [
      "dotnet run --project eng/Electron2D.Build -- verify docs",
      "artifact://screenshots/frame.png",
      "not classified"
    ]
  });
});

test("group ordering keeps each milestone beneath its epoch", () => {
  const view = buildBoardView({
    board: {
      revision: 1,
      groups: [
        { groupId: "milestone-b", kind: "Milestone", title: "Milestone B", rank: "00000100", parentGroupId: "epoch-a" },
        { groupId: "epoch-a", kind: "Epoch", title: "Epoch A", rank: "00001000", parentGroupId: null },
        { groupId: "epoch-c", kind: "Epoch", title: "Epoch C", rank: "00002000", parentGroupId: null },
        { groupId: "milestone-a", kind: "Milestone", title: "Milestone A", rank: "00000200", parentGroupId: "epoch-a" }
      ],
      placements: []
    },
    tasks: []
  });

  assert.deepEqual(view.groups.map(group => group.groupId), ["epoch-a", "milestone-b", "milestone-a", "epoch-c"]);
});

test("task filtering intersects canonical tag ids and combines them with text and priority", () => {
  const matchesTaskFilters = (model as unknown as {
    matchesTaskFilters?: (
      task: model.TaskSnapshot,
      query: string,
      priority: string,
      tagIds: ReadonlySet<string>) => boolean;
  }).matchesTaskFilters;
  assert.equal(typeof matchesTaskFilters, "function", "model must expose task filter matching");
  const task: model.TaskSnapshot = {
    taskId: "T-42",
    revision: 1,
    title: "Проверить reference layout",
    status: "InProgress",
    priority: "P1",
    labels: ["ui", "vscode"],
    dependencies: []
  };

  assert.equal(matchesTaskFilters!(task, "reference", "P1", new Set()), true);
  assert.equal(matchesTaskFilters!(task, "VSCODE", "P1", new Set()), true);
  assert.equal(matchesTaskFilters!(task, "reference", "P1", new Set(["vscode"])), true);
  assert.equal(matchesTaskFilters!(task, "reference", "P1", new Set(["ui", "vscode"])), true);
  assert.equal(matchesTaskFilters!(task, "reference", "P1", new Set(["vscode", "missing-tag"])), false);
  assert.equal(matchesTaskFilters!(task, "missing", "P1", new Set(["vscode"])), false);
  assert.equal(matchesTaskFilters!(task, "reference", "P0", new Set(["vscode"])), false);
  assert.equal(task.status, "InProgress");
});

test("global tag references resolve through the board catalog", () => {
  const resolveTaskTags = (model as unknown as {
    resolveTaskTags?: (
      task: model.TaskSnapshot,
      tags: readonly model.TaskBoardTagSnapshot[]) => readonly model.TaskBoardTagSnapshot[];
  }).resolveTaskTags;
  assert.equal(typeof resolveTaskTags, "function");
  const task: model.TaskSnapshot = {
    taskId: "T-42",
    revision: 1,
    title: "Карточка",
    status: "Ready",
    priority: "P1",
    labels: ["tag-ui", "tag-missing"],
    dependencies: []
  };
  const tags: readonly model.TaskBoardTagSnapshot[] = [
    { tagId: "tag-ui", name: "Интерфейс", color: "Blue" },
    { tagId: "tag-docs", name: "Документация", color: "Purple" }
  ];

  assert.deepEqual(resolveTaskTags!(task, tags), [tags[0]]);
});

test("task tag colors normalize legacy names and strict custom hex values", () => {
  const normalizeTaskBoardTagColor = (model as unknown as {
    normalizeTaskBoardTagColor?: (value: string) => model.TaskBoardTagColor | undefined;
  }).normalizeTaskBoardTagColor;
  assert.equal(typeof normalizeTaskBoardTagColor, "function");
  assert.equal(normalizeTaskBoardTagColor!("blue"), "Blue");
  assert.equal(normalizeTaskBoardTagColor!("#a1b2c3"), "#A1B2C3");
  assert.equal(normalizeTaskBoardTagColor!("#ABC"), undefined);
  assert.equal(normalizeTaskBoardTagColor!("#A1B2C3DD"), undefined);
  assert.equal(normalizeTaskBoardTagColor!("rgba(1,2,3,1)"), undefined);
});

test("task patch parser returns unique top-level JSON Pointer fields without values", () => {
  const taskPatchTopLevelFields = (model as unknown as {
    taskPatchTopLevelFields?: (payload: string) => readonly string[] | undefined;
  }).taskPatchTopLevelFields;
  assert.equal(typeof taskPatchTopLevelFields, "function", "model must expose safe TaskPatched parsing");
  if (!taskPatchTopLevelFields) {
    return;
  }

  const payload = JSON.stringify({
    patch: [
      { op: "replace", path: "/title", value: "Новый заголовок" },
      { op: "replace", path: "/acceptanceCriteria/0/description", value: "Секретное значение" },
      { op: "replace", path: "/acceptanceCriteria/0/state", value: "Passed" },
      { op: "replace", path: "/links", value: [{ kind: "File", value: "secret.txt" }] },
      { op: "remove", path: "/executionContract/commands/0" }
    ]
  });

  assert.deepEqual(
    taskPatchTopLevelFields(payload),
    ["title", "acceptanceCriteria", "links", "executionContract"]);
  assert.deepEqual(taskPatchTopLevelFields('{"patch":[]}'), []);
  assert.equal(taskPatchTopLevelFields('{"patch":"invalid"}'), undefined);
  assert.equal(taskPatchTopLevelFields('{"patch":[{"path":"not-a-pointer"}]}'), undefined);
  assert.equal(taskPatchTopLevelFields('<img src=x onerror=alert(1)>'), undefined);
});

test("status change parser decodes a closed canonical payload without exposing JSON", () => {
  const parseStatusChangePayload = (model as unknown as {
    parseStatusChangePayload?: (payload: string) => {
      previous: string;
      next: string;
      reason: string;
    } | undefined;
  }).parseStatusChangePayload;
  assert.equal(typeof parseStatusChangePayload, "function", "model must expose safe StatusChange parsing");
  if (!parseStatusChangePayload) {
    return;
  }

  assert.deepEqual(
    parseStatusChangePayload('{"previous":"Review","next":"InProgress","reason":"\\u041f\\u043e\\u043b\\u044c\\u0437\\u043e\\u0432\\u0430\\u0442\\u0435\\u043b\\u044c \\u0432\\u0435\\u0440\\u043d\\u0443\\u043b \\u0437\\u0430\\u0434\\u0430\\u0447\\u0443"}'),
    { previous: "Review", next: "InProgress", reason: "Пользователь вернул задачу" });
  assert.equal(parseStatusChangePayload('{"previous":"Unknown","next":"Ready","reason":"Причина"}'), undefined);
  assert.equal(parseStatusChangePayload('{"previous":"Ready","next":"Review","reason":""}'), undefined);
  assert.equal(parseStatusChangePayload('{"previous":"Ready","next":"Review","reason":"Причина","html":"<b>unsafe</b>"}'), undefined);
  assert.equal(parseStatusChangePayload('<img src=x onerror=alert(1)>'), undefined);
});
