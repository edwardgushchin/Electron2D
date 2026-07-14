/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import test from "node:test";
import * as cliModule from "../../src/cli.js";
import {
  boardReadArguments,
  buildSpawnRequest,
  buildTaskArguments,
  createHumanDecisionBridge,
  createHumanMessageBridge,
  parseCliEnvelope,
  resolveCliExecutable,
  taskReadArguments
} from "../../src/cli.js";

test("buildTaskArguments uses an argv array with project and JSON envelope", () => {
  assert.deepEqual(buildTaskArguments("C:/repo with spaces", ["board"]), [
    "tasks", "board", "--project", "C:/repo with spaces", "--format", "json"
  ]);
});

test("task board uses compact refresh and lazy full task reads", () => {
  assert.deepEqual(boardReadArguments(), ["board", "--compact", "true", "--include-archived", "true"]);
  assert.deepEqual(taskReadArguments("T-1147"), ["get", "T-1147"]);
});

test("task revision conflict detection excludes board conflicts and unrelated diagnostics", () => {
  const api = cliModule as unknown as {
    isTaskRevisionConflict?: (error: unknown) => boolean;
  };
  assert.equal(typeof api.isTaskRevisionConflict, "function");
  if (!api.isTaskRevisionConflict) {
    return;
  }

  assert.equal(
    api.isTaskRevisionConflict(new Error("E2D-TASK-0006: Task revision conflict: expected 12, actual 14.")),
    true);
  assert.equal(
    api.isTaskRevisionConflict(new Error("E2D-TASK-0006: Taskboard revision conflict: expected 12, actual 14.")),
    false);
  assert.equal(api.isTaskRevisionConflict(new Error("Taskboard is locked by another writer.")), false);
});

test("cancelled task lifecycle builds revision-aware CLI arguments and requires exact confirmation", () => {
  const api = cliModule as unknown as {
    reopenTaskArguments?: (taskId: string, taskRevision: number, reason: string) => readonly string[];
    archiveTaskArguments?: (taskId: string, taskRevision: number, boardRevision: number) => readonly string[];
    unarchiveTaskArguments?: (taskId: string, taskRevision: number, boardRevision: number) => readonly string[];
    isConfirmedDialogResult?: (result: string | undefined, actionLabel: string) => boolean;
  };
  assert.equal(typeof api.reopenTaskArguments, "function");
  assert.equal(typeof api.archiveTaskArguments, "function");
  assert.equal(typeof api.unarchiveTaskArguments, "function");
  assert.equal(typeof api.isConfirmedDialogResult, "function");
  if (!api.reopenTaskArguments || !api.archiveTaskArguments ||
      !api.unarchiveTaskArguments || !api.isConfirmedDialogResult) {
    return;
  }

  assert.deepEqual(api.reopenTaskArguments("T-1220", 7, "Resume"), [
    "reopen", "T-1220", "--reason", "Resume", "--expected-revision", "7"
  ]);
  assert.deepEqual(api.archiveTaskArguments("T-1220", 7, 19), [
    "archive", "T-1220", "--expected-revision", "7", "--expected-board-revision", "19"
  ]);
  assert.deepEqual(api.unarchiveTaskArguments("T-1220", 8, 20), [
    "unarchive", "T-1220", "--expected-revision", "8", "--expected-board-revision", "20"
  ]);
  assert.equal(api.isConfirmedDialogResult("В архив", "В архив"), true);
  assert.equal(api.isConfirmedDialogResult(undefined, "В архив"), false);
  assert.equal(api.isConfirmedDialogResult("Отмена", "В архив"), false);
});

test("resolveCliExecutable prefers configured path and otherwise uses PATH command", () => {
  assert.equal(resolveCliExecutable(" C:/Tools/e2d.exe ", "win32"), "C:/Tools/e2d.exe");
  assert.equal(resolveCliExecutable("", "win32"), "e2d.exe");
  assert.equal(resolveCliExecutable(undefined, "linux"), "e2d");
});

test("buildSpawnRequest disables shell execution and hides Windows helper windows", () => {
  assert.deepEqual(buildSpawnRequest("e2d.exe", ["tasks", "board"], "C:/repo"), {
    file: "e2d.exe",
    args: ["tasks", "board"],
    options: { cwd: "C:/repo", shell: false, windowsHide: true }
  });
});

test("parseCliEnvelope returns data only for a successful matching JSON envelope", () => {
  const data = parseCliEnvelope(JSON.stringify({ schemaVersion: 1, succeeded: true, exitCode: 0, data: { mode: "tasks.board" } }));
  assert.deepEqual(data, { mode: "tasks.board" });
  assert.throws(() => parseCliEnvelope(JSON.stringify({ schemaVersion: 1, succeeded: false, exitCode: 2, message: "conflict", diagnostics: [] })), /conflict/);
  assert.throws(() => parseCliEnvelope(JSON.stringify({
    schemaVersion: 1,
    succeeded: false,
    exitCode: 2,
    message: "Task command failed.",
    diagnostics: [{ code: "E2D-CLI-0002", message: "Task revision conflict: expected 4, actual 5." }]
  })), /E2D-CLI-0002.*revision conflict/i);
  assert.throws(() => parseCliEnvelope("not json"), /valid JSON/);
});

test("human decision bridge keeps the capability out of argv and binds stdin to the environment", () => {
  const bridge = createHumanDecisionBridge("T-9", 4, "accept", "Approved", Buffer.alloc(32, 7));

  assert.deepEqual(bridge.taskArguments, ["__human-decision", "T-9", "--expected-revision", "4"]);
  assert.equal(bridge.environmentCapability.length >= 32, true);
  assert.equal(bridge.taskArguments.includes(bridge.environmentCapability), false);
  assert.deepEqual(JSON.parse(bridge.stdin), {
    protocol: "Electron2D.TaskHumanDecision/1",
    capability: bridge.environmentCapability,
    decision: "accept",
    reason: "Approved"
  });
});

test("human message bridge keeps trusted identity material out of argv and webview payload", () => {
  const bridge = createHumanMessageBridge("T-1222", 4, "Проверь задачу", Buffer.alloc(32, 9));

  assert.deepEqual(bridge.taskArguments, ["__human-message", "T-1222", "--expected-revision", "4"]);
  assert.equal(bridge.environmentCapability.length >= 32, true);
  assert.equal(bridge.taskArguments.includes(bridge.environmentCapability), false);
  assert.deepEqual(JSON.parse(bridge.stdin), {
    protocol: "Electron2D.TaskHumanMessage/1",
    capability: bridge.environmentCapability,
    text: "Проверь задачу"
  });
});

test("human message append refreshes and retries only an exact task revision conflict", async () => {
  const api = cliModule as unknown as {
    runWithTaskRevisionConflictRetry?: <T>(
      initialTaskRevision: number,
      expectedTaskUid: string,
      start: (expectedTaskRevision: number) => Promise<T>,
      refresh: () => Promise<{ readonly taskUid: string; readonly revision: number } | undefined>,
      maxAttempts?: number) => Promise<T>;
  };
  assert.equal(
    typeof api.runWithTaskRevisionConflictRetry,
    "function",
    "trusted human append must safely recover a stale task snapshot");

  const events: string[] = [];
  const result = await api.runWithTaskRevisionConflictRetry!(
    27,
    "task-uid-1222",
    async revision => {
      events.push(`append:${revision}`);
      if (revision === 27) {
        throw new Error("E2D-TASK-0006: Task revision conflict: expected 27, actual 29.");
      }
      return "stored";
    },
    async () => {
      events.push("refresh");
      return { taskUid: "task-uid-1222", revision: 29 };
    });

  assert.equal(result, "stored");
  assert.deepEqual(events, ["append:27", "refresh", "append:29"]);
});

test("human message revision recovery fails closed for board, identity and repeated conflicts", async () => {
  const api = cliModule as unknown as {
    runWithTaskRevisionConflictRetry?: <T>(
      initialTaskRevision: number,
      expectedTaskUid: string,
      start: (expectedTaskRevision: number) => Promise<T>,
      refresh: () => Promise<{ readonly taskUid: string; readonly revision: number } | undefined>,
      maxAttempts?: number) => Promise<T>;
  };
  assert.equal(typeof api.runWithTaskRevisionConflictRetry, "function");

  let attempts = 0;
  let refreshes = 0;
  await assert.rejects(
    api.runWithTaskRevisionConflictRetry!(
      27,
      "task-uid-1222",
      async () => {
        attempts++;
        throw new Error("Taskboard revision conflict: expected 27, actual 29.");
      },
      async () => {
        refreshes++;
        return { taskUid: "task-uid-1222", revision: 29 };
      }),
    /Taskboard revision conflict/);
  assert.equal(attempts, 1);
  assert.equal(refreshes, 0);

  await assert.rejects(
    api.runWithTaskRevisionConflictRetry!(
      27,
      "task-uid-1222",
      async () => { throw new Error("Task revision conflict: expected 27, actual 29."); },
      async () => ({ taskUid: "different-task-uid", revision: 29 })),
    /no longer applies/i);

  attempts = 0;
  refreshes = 0;
  await assert.rejects(
    api.runWithTaskRevisionConflictRetry!(
      27,
      "task-uid-1222",
      async revision => {
        attempts++;
        throw new Error(`Task revision conflict: expected ${revision}, actual ${revision + 1}.`);
      },
      async () => {
        refreshes++;
        return { taskUid: "task-uid-1222", revision: 27 + refreshes };
      }),
    /Task revision conflict/);
  assert.equal(attempts, 2);
  assert.equal(refreshes, 1);
});

test("human acceptance workflow submits an internal review before accepting the returned revision", async () => {
  const api = cliModule as unknown as {
    runHumanAcceptanceWorkflow?: <T>(
      taskId: string,
      acceptanceState: string | undefined,
      expectedTaskRevision: number,
      submit: (revision: number) => Promise<unknown>,
      accept: (revision: number) => Promise<T>) => Promise<T>;
  };
  assert.equal(typeof api.runHumanAcceptanceWorkflow, "function", "acceptance orchestration must be reusable and testable");

  const events: string[] = [];
  const result = await api.runHumanAcceptanceWorkflow!(
    "T-1149",
    "InternalReview",
    7,
    async revision => {
      events.push(`submit:${revision}`);
      return { mode: "tasks.submit", task: { taskId: "T-1149", revision: 8 } };
    },
    async revision => {
      events.push(`accept:${revision}`);
      return "accepted";
    });

  assert.equal(result, "accepted");
  assert.deepEqual(events, ["submit:7", "accept:8"]);
});

test("human acceptance workflow skips duplicate submit for an already submitted review", async () => {
  const api = cliModule as unknown as {
    runHumanAcceptanceWorkflow?: <T>(
      taskId: string,
      acceptanceState: string | undefined,
      expectedTaskRevision: number,
      submit: (revision: number) => Promise<unknown>,
      accept: (revision: number) => Promise<T>) => Promise<T>;
  };
  assert.equal(typeof api.runHumanAcceptanceWorkflow, "function");

  let submissions = 0;
  const acceptedRevision = await api.runHumanAcceptanceWorkflow!(
    "T-1207",
    "Submitted",
    9,
    async () => {
      submissions++;
      return { mode: "tasks.submit", task: { taskId: "T-1207", revision: 10 } };
    },
    async revision => revision);

  assert.equal(submissions, 0);
  assert.equal(acceptedRevision, 9);
});

test("compact board reads share one in-flight request and release it after settlement", async () => {
  const api = cliModule as unknown as {
    InFlightRequestBroker?: new () => {
      run<T>(key: string, start: () => Promise<T>): Promise<T>;
    };
    isCompactBoardRead?: (arguments_: readonly string[]) => boolean;
    runWithTransientTaskboardLockRetry?: <T>(
      start: () => Promise<T>,
      pause: () => Promise<void>,
      maxAttempts?: number) => Promise<T>;
  };
  assert.equal(typeof api.InFlightRequestBroker, "function", "CLI layer must expose an in-flight request broker");
  assert.equal(typeof api.isCompactBoardRead, "function", "CLI layer must identify only compact board reads");
  assert.equal(typeof api.runWithTransientTaskboardLockRetry, "function", "CLI layer must retry transient taskboard locks");
  assert.equal(api.isCompactBoardRead!(boardReadArguments()), true);
  assert.equal(api.isCompactBoardRead!(taskReadArguments("T-1")), false);
  assert.equal(api.isCompactBoardRead!(["set-status", "T-1", "--status", "Ready"]), false);

  const broker = new api.InFlightRequestBroker!();
  let starts = 0;
  let finish: ((value: string) => void) | undefined;
  const pending = new Promise<string>(resolve => { finish = resolve; });
  const first = broker.run("workspace|e2d", () => { starts++; return pending; });
  const second = broker.run("workspace|e2d", () => { starts++; return Promise.resolve("unexpected"); });
  assert.strictEqual(second, first);
  assert.equal(starts, 1);
  finish!("snapshot");
  assert.equal(await first, "snapshot");
  assert.equal(await second, "snapshot");

  const next = broker.run("workspace|e2d", () => { starts++; return Promise.resolve("next"); });
  assert.equal(await next, "next");
  assert.equal(starts, 2);

  let attempts = 0;
  let pauses = 0;
  const retried = await api.runWithTransientTaskboardLockRetry!(
    async () => {
      attempts++;
      if (attempts === 1) {
        throw new Error("E2D-CLI-0002: Taskboard is locked by another writer.");
      }
      return "recovered";
    },
    async () => { pauses++; });
  assert.equal(retried, "recovered");
  assert.equal(attempts, 2);
  assert.equal(pauses, 1);

  attempts = 0;
  await assert.rejects(
    api.runWithTransientTaskboardLockRetry!(async () => {
      attempts++;
      throw new Error("E2D-CLI-0002: malformed board");
    }, async () => { pauses++; }),
    /malformed board/);
  assert.equal(attempts, 1);
});

test("workspace command scheduler preserves FIFO order, survives failures and isolates keys", async () => {
  const api = cliModule as unknown as {
    WorkspaceCommandScheduler?: new () => {
      run<T>(key: string, start: () => Promise<T>): Promise<T>;
    };
  };
  assert.equal(
    typeof api.WorkspaceCommandScheduler,
    "function",
    "CLI layer must expose one serial scheduler for every workspace command path");

  const scheduler = new api.WorkspaceCommandScheduler!();
  const events: string[] = [];
  let releaseFirst: (() => void) | undefined;
  const firstGate = new Promise<void>(resolve => { releaseFirst = resolve; });
  const first = scheduler.run("workspace-a|e2d", async () => {
    events.push("first:start");
    await firstGate;
    events.push("first:end");
    return "first";
  });
  const second = scheduler.run("workspace-a|e2d", async () => {
    events.push("second:start");
    return "second";
  });
  const independent = scheduler.run("workspace-b|e2d", async () => {
    events.push("independent:start");
    return "independent";
  });

  await Promise.resolve();
  assert.deepEqual(events, ["first:start", "independent:start"]);
  assert.equal(await independent, "independent");
  releaseFirst!();
  assert.deepEqual(await Promise.all([first, second]), ["first", "second"]);
  assert.deepEqual(events, ["first:start", "independent:start", "first:end", "second:start"]);

  await assert.rejects(
    scheduler.run("workspace-a|e2d", async () => { throw new Error("semantic failure"); }),
    /semantic failure/);
  assert.equal(
    await scheduler.run("workspace-a|e2d", async () => "recovered queue"),
    "recovered queue",
    "a rejected command must not poison the workspace queue");
});

test("transient lock retry keeps its FIFO position until the command settles", async () => {
  const api = cliModule as unknown as {
    WorkspaceCommandScheduler?: new () => {
      run<T>(key: string, start: () => Promise<T>): Promise<T>;
    };
    runWithTransientTaskboardLockRetry?: <T>(
      start: () => Promise<T>,
      pause: (failedAttempt: number) => Promise<void>,
      maxAttempts?: number) => Promise<T>;
  };
  assert.equal(typeof api.WorkspaceCommandScheduler, "function");
  assert.equal(typeof api.runWithTransientTaskboardLockRetry, "function");

  const scheduler = new api.WorkspaceCommandScheduler!();
  const events: string[] = [];
  let attempts = 0;
  let releaseRetry: (() => void) | undefined;
  const retryGate = new Promise<void>(resolve => { releaseRetry = resolve; });
  const first = scheduler.run("workspace|e2d", async () =>
    await api.runWithTransientTaskboardLockRetry!(
      async () => {
        attempts++;
        events.push(`first:${attempts}`);
        if (attempts === 1) {
          throw new Error("E2D-CLI-0002: Taskboard is locked by another writer.");
        }
        return "retried";
      },
      async failedAttempt => {
        events.push(`pause:${failedAttempt}`);
        await retryGate;
      },
      3));
  const second = scheduler.run("workspace|e2d", async () => {
    events.push("second:start");
    return "second";
  });

  await Promise.resolve();
  await Promise.resolve();
  assert.deepEqual(events, ["first:1", "pause:1"]);
  releaseRetry!();
  assert.deepEqual(await Promise.all([first, second]), ["retried", "second"]);
  assert.deepEqual(events, ["first:1", "pause:1", "first:2", "second:start"]);
});

test("board revision conflict refreshes and retries with the current revision", async () => {
  const api = cliModule as unknown as {
    runWithBoardRevisionConflictRetry?: <T>(
      initialBoardRevision: number,
      start: (expectedBoardRevision: number) => Promise<T>,
      refresh: () => Promise<number | undefined>,
      maxAttempts?: number,
      inapplicableMessage?: string) => Promise<T>;
  };
  assert.equal(
    typeof api.runWithBoardRevisionConflictRetry,
    "function",
    "CLI orchestration must recover safe placement from an optimistic board conflict");

  const events: string[] = [];
  const result = await api.runWithBoardRevisionConflictRetry!(
    41,
    async revision => {
      events.push(`move:${revision}`);
      if (revision === 41) {
        throw new Error("E2D-CLI-0002: Taskboard revision conflict: expected 41, actual 42.");
      }
      return "moved";
    },
    async () => {
      events.push("refresh");
      return 42;
    });

  assert.equal(result, "moved");
  assert.deepEqual(events, ["move:41", "refresh", "move:42"]);
});

test("board revision recovery rejects inapplicable semantic and repeated failures", async () => {
  const api = cliModule as unknown as {
    runWithBoardRevisionConflictRetry?: <T>(
      initialBoardRevision: number,
      start: (expectedBoardRevision: number) => Promise<T>,
      refresh: () => Promise<number | undefined>,
      maxAttempts?: number,
      inapplicableMessage?: string) => Promise<T>;
  };
  assert.equal(typeof api.runWithBoardRevisionConflictRetry, "function");

  let attempts = 0;
  let refreshes = 0;
  await assert.rejects(
    api.runWithBoardRevisionConflictRetry!(
      8,
      async () => {
        attempts++;
        throw new Error("E2D-CLI-0002: Task revision conflict: expected 8, actual 9.");
      },
      async () => { refreshes++; return 9; }),
    /Task revision conflict/);
  assert.equal(attempts, 1);
  assert.equal(refreshes, 0);

  await assert.rejects(
    api.runWithBoardRevisionConflictRetry!(
      10,
      async () => { throw new Error("Taskboard revision conflict: expected 10, actual 11."); },
      async () => undefined,
      3,
      "Перемещение больше неприменимо после обновления доски задач."),
    /больше неприменимо/i);

  attempts = 0;
  refreshes = 0;
  await assert.rejects(
    api.runWithBoardRevisionConflictRetry!(
      20,
      async revision => {
        attempts++;
        throw new Error(`Taskboard revision conflict: expected ${revision}, actual ${revision + 1}.`);
      },
      async () => {
        refreshes++;
        return 20 + refreshes;
      },
      3),
    /Taskboard revision conflict/);
  assert.equal(attempts, 3);
  assert.equal(refreshes, 2);
});

test("refresh coordinator preserves a trailing request during an in-flight board read", async () => {
  const api = cliModule as unknown as {
    TrailingRefreshCoordinator?: new (refresh: () => Promise<void>) => {
      request(): Promise<void>;
    };
  };
  assert.equal(
    typeof api.TrailingRefreshCoordinator,
    "function",
    "CLI-backed consumers need a serialized trailing refresh coordinator");

  let runs = 0;
  let active = 0;
  let maximumActive = 0;
  let releaseFirst: (() => void) | undefined;
  const firstGate = new Promise<void>(resolve => { releaseFirst = resolve; });
  const coordinator = new api.TrailingRefreshCoordinator!(async () => {
    runs++;
    active++;
    maximumActive = Math.max(maximumActive, active);
    if (runs === 1) {
      await firstGate;
    }
    active--;
  });

  const first = coordinator.request();
  await Promise.resolve();
  const second = coordinator.request();
  const third = coordinator.request();
  assert.equal(runs, 1, "requests must not start concurrent board reads");
  releaseFirst!();
  await Promise.all([first, second, third]);

  assert.equal(runs, 2, "events during the first read must coalesce into one fresh trailing read");
  assert.equal(maximumActive, 1, "board reads must remain serialized");
});

test("refresh coordinator continues with a queued refresh after an error", async () => {
  const api = cliModule as unknown as {
    TrailingRefreshCoordinator?: new (refresh: () => Promise<void>) => {
      request(): Promise<void>;
    };
  };
  assert.equal(typeof api.TrailingRefreshCoordinator, "function");

  let runs = 0;
  let failOnThirdRun = true;
  let releaseFailure: (() => void) | undefined;
  const failureGate = new Promise<void>(resolve => { releaseFailure = resolve; });
  const coordinator = new api.TrailingRefreshCoordinator!(async () => {
    runs++;
    if (runs === 1) {
      await failureGate;
      throw new Error("first refresh failed");
    }
    if (runs === 3 && failOnThirdRun) {
      failOnThirdRun = false;
      throw new Error("isolated refresh failed");
    }
  });

  const first = coordinator.request();
  await Promise.resolve();
  const trailing = coordinator.request();
  releaseFailure!();
  await Promise.all([first, trailing]);
  assert.equal(runs, 2, "a failed read must not discard the queued watcher refresh");

  await assert.rejects(coordinator.request(), /isolated refresh failed/);
  await coordinator.request();
  assert.equal(runs, 4, "the coordinator must accept later refresh requests after an error");
});
