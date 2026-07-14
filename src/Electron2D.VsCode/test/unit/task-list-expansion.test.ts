/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import test from "node:test";
import type { TaskStatus } from "../../src/model.js";

interface TaskListExpansionState {
  expand(status: TaskStatus): void;
  visibleTasks<T>(status: TaskStatus, tasks: readonly T[], previewLimit: number): readonly T[];
}

test("expanded task list survives repeated board renders and remains independent by status", async () => {
  const expansionModule = await import("../../src/taskListExpansion.js").catch(() => undefined) as {
    createTaskListExpansionState?: () => TaskListExpansionState;
  } | undefined;

  assert.ok(expansionModule, "task-list expansion state module must exist");
  assert.equal(
    typeof expansionModule.createTaskListExpansionState,
    "function",
    "task-list expansion state factory must exist");

  const state = expansionModule.createTaskListExpansionState!();
  const initialReadyTasks = ["T-1", "T-2", "T-3", "T-4", "T-5", "T-6", "T-7"];
  assert.deepEqual(state.visibleTasks("Ready", initialReadyTasks, 6), initialReadyTasks.slice(0, 6));

  state.expand("Ready");
  const refreshedReadyTasks = [...initialReadyTasks, "T-8"];
  assert.deepEqual(
    state.visibleTasks("Ready", refreshedReadyTasks, 6),
    refreshedReadyTasks,
    "a repeated state render must use the current complete task collection");

  const blockedTasks = ["B-1", "B-2", "B-3", "B-4", "B-5", "B-6", "B-7"];
  assert.deepEqual(
    state.visibleTasks("Blocked", blockedTasks, 6),
    blockedTasks.slice(0, 6),
    "expanding Ready must not expand another status column");

  const freshWebviewState = expansionModule.createTaskListExpansionState!();
  assert.deepEqual(
    freshWebviewState.visibleTasks("Ready", refreshedReadyTasks, 6),
    refreshedReadyTasks.slice(0, 6),
    "a new webview must start with the compact preview");
});

test("webview reuses one local expansion state across task-list reconstruction", () => {
  const script = readFileSync(resolve(process.cwd(), "src/webview.ts"), "utf8");

  assert.match(script, /import \{ createTaskListExpansionState \} from "\.\/taskListExpansion\.js"/);
  assert.match(script, /const taskListExpansionState = createTaskListExpansionState\(\)/);
  assert.match(
    script,
    /const visible = taskListExpansionState\.visibleTasks\(status, tasks, CARD_PREVIEW_LIMIT\)/);
  assert.match(
    script,
    /more\.addEventListener\("click", \(\) => \{\s*taskListExpansionState\.expand\(status\);\s*renderCards\(\);\s*\}\)/s);
  assert.match(script, /renderCards\(\);/);
  assert.doesNotMatch(script, /renderCards\(false\)/);
});
