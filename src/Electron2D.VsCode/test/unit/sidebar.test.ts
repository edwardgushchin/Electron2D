/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import test from "node:test";
import * as model from "../../src/model.js";

test("sidebar rows list active tasks in canonical status and placement order", () => {
  const buildSidebarRows = (model as unknown as {
    buildSidebarRows?: (snapshot: model.TaskBoardSnapshot) => Array<{
      taskId: string;
      label: string;
      description: string;
      tooltip: string;
    }>;
  }).buildSidebarRows;
  assert.equal(typeof buildSidebarRows, "function", "model must expose sidebar task rows");

  const rows = buildSidebarRows!({
    board: {
      revision: 4,
      groups: [],
      placements: [
        { taskId: "T-0002", groupId: null, rank: "00002000" },
        { taskId: "T-0001", groupId: null, rank: "00001000" },
        { taskId: "T-0003", groupId: null, rank: "00000500" }
      ]
    },
    tasks: [
      { taskId: "T-0002", revision: 1, title: "Second ready", status: "Ready", priority: "P2", labels: [], dependencies: [] },
      { taskId: "T-0003", revision: 1, title: "In progress", status: "InProgress", priority: "P0", labels: [], dependencies: [] },
      { taskId: "T-0001", revision: 1, title: "First ready", status: "Ready", priority: "P1", labels: [], dependencies: [] },
      { taskId: "T-0004", revision: 1, title: "Archived", status: "Done", priority: "P3", labels: [], dependencies: [], archivedAt: "2026-07-01T00:00:00Z" }
    ]
  });

  assert.deepEqual(rows.map(row => row.taskId), ["T-0001", "T-0002", "T-0003"]);
  assert.deepEqual(rows.map(row => row.label), [
    "T-0001 · First ready",
    "T-0002 · Second ready",
    "T-0003 · In progress"
  ]);
  assert.equal(rows[2]?.description, "В работе · P0");
  assert.equal(rows[2]?.tooltip, "T-0003 · In progress\nВ работе · P0");
});

test("sidebar selection renders a loading compact task and ignores stale hydration", async () => {
  const renderTaskSelection = (model as unknown as {
    renderTaskSelection?: <T>(options: {
      compact: T;
      cached?: T;
      render(task: T, phase: "loading" | "ready"): Promise<void>;
      load(): Promise<T>;
      isCurrent(): boolean;
      remember(task: T): void;
    }) => Promise<void>;
  }).renderTaskSelection;
  assert.equal(typeof renderTaskSelection, "function", "model must expose the immediate selection pipeline");

  let finishLoad: ((value: string) => void) | undefined;
  const loaded = new Promise<string>(resolve => { finishLoad = resolve; });
  const rendered: Array<{ task: string; phase: string }> = [];
  const remembered: string[] = [];
  let current = true;
  const operation = renderTaskSelection!({
    compact: "compact",
    render: async (task, phase) => { rendered.push({ task, phase }); },
    load: async () => await loaded,
    isCurrent: () => current,
    remember: task => { remembered.push(task); }
  });

  await new Promise(resolve => setImmediate(resolve));
  assert.deepEqual(rendered, [{ task: "compact", phase: "loading" }]);
  current = false;
  finishLoad!("full");
  await operation;
  assert.deepEqual(rendered, [{ task: "compact", phase: "loading" }]);
  assert.deepEqual(remembered, []);
});

test("sidebar selection renders cached details while canonical hydration refreshes them", async () => {
  const renderTaskSelection = (model as unknown as {
    renderTaskSelection?: <T>(options: {
      compact: T;
      cached?: T;
      render(task: T, phase: "loading" | "ready"): Promise<void>;
      load(): Promise<T>;
      isCurrent(): boolean;
      remember(task: T): void;
    }) => Promise<void>;
  }).renderTaskSelection!;
  const rendered: Array<{ task: string; phase: string }> = [];
  const remembered: string[] = [];

  await renderTaskSelection({
    compact: "compact",
    cached: "cached-full",
    render: async (task, phase) => { rendered.push({ task, phase }); },
    load: async () => "fresh-full",
    isCurrent: () => true,
    remember: task => { remembered.push(task); }
  });

  assert.deepEqual(rendered, [
    { task: "cached-full", phase: "ready" },
    { task: "fresh-full", phase: "ready" }
  ]);
  assert.deepEqual(remembered, ["fresh-full"]);
});
