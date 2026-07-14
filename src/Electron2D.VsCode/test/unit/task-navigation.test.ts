/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import test from "node:test";
import * as model from "../../src/model.js";

interface NavigationHistory {
  readonly currentTaskId: string | undefined;
  readonly canGoBack: boolean;
  openDirect(taskId: string): void;
  openInternal(taskId: string): void;
  back(): string | undefined;
  clear(): void;
}

type NavigationHistoryConstructor = new () => NavigationHistory;

function navigationHistoryConstructor(): NavigationHistoryConstructor {
  const constructor = (model as unknown as { TaskNavigationHistory?: NavigationHistoryConstructor })
    .TaskNavigationHistory;
  assert.equal(typeof constructor, "function", "model must expose TaskNavigationHistory");
  return constructor!;
}

test("task navigation history traverses three internal selections in browser order", () => {
  const History = navigationHistoryConstructor();
  const history = new History();

  history.openDirect("T-A");
  history.openInternal("T-B");
  history.openInternal("T-C");
  assert.equal(history.currentTaskId, "T-C");
  assert.equal(history.canGoBack, true);

  assert.equal(history.back(), "T-B");
  assert.equal(history.currentTaskId, "T-B");
  assert.equal(history.back(), "T-A");
  assert.equal(history.currentTaskId, "T-A");
  assert.equal(history.canGoBack, false);
});

test("direct selection duplicate internal selection and close keep history bounded", () => {
  const History = navigationHistoryConstructor();
  const history = new History();

  history.openDirect("T-A");
  history.openInternal("T-A");
  assert.equal(history.canGoBack, false, "opening the current task must not add a duplicate");
  history.openInternal("T-B");
  history.openDirect("T-C");
  assert.equal(history.currentTaskId, "T-C");
  assert.equal(history.canGoBack, false, "a board or sidebar selection starts a new root");
  history.openInternal("T-D");
  history.clear();
  assert.equal(history.currentTaskId, undefined);
  assert.equal(history.canGoBack, false);
  assert.equal(history.back(), undefined);
});

test("closing navigation rejects hydration that finishes after the modal is gone", async () => {
  const History = navigationHistoryConstructor();
  const history = new History();
  history.openDirect("T-A");
  let finishLoad: ((value: string) => void) | undefined;
  const loaded = new Promise<string>(resolve => { finishLoad = resolve; });
  const rendered: string[] = [];
  const remembered: string[] = [];

  const operation = model.renderTaskSelection({
    compact: "compact-A",
    cached: undefined,
    render: async task => { rendered.push(task); },
    load: async () => await loaded,
    isCurrent: () => history.currentTaskId === "T-A",
    remember: task => { remembered.push(task); }
  });
  await new Promise(resolve => setImmediate(resolve));
  history.clear();
  finishLoad!("hydrated-A");
  await operation;

  assert.deepEqual(rendered, ["compact-A"]);
  assert.deepEqual(remembered, []);
});
