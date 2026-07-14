/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  agentSessionKey,
  createAgentChatState,
  reduceAgentChatEvent,
  sanitizedAgentDiagnostic,
  type AgentPresentationEvent
} from "../../src/agentChat.js";

test("agent session keys isolate tasks inside the same workspace", () => {
  const first = agentSessionKey("file:///G:/Projects/Electron2D", "task-uid-1");
  const second = agentSessionKey("file:///G:/Projects/Electron2D", "task-uid-2");
  assert.notEqual(first, second);
  assert.equal(first, agentSessionKey("file:///G:/Projects/Electron2D", "task-uid-1"));
  assert.notEqual(first, agentSessionKey("file:///G:/Projects/Other", "task-uid-1"));
});

test("stream reducer keeps transient progress separate from the final answer", () => {
  let state = createAgentChatState("T-1222");
  const events: AgentPresentationEvent[] = [
    { kind: "status", runId: "run-1", status: "running", text: "Agent is working" },
    { kind: "reasoning", runId: "run-1", text: "Inspecting context" },
    { kind: "tool", runId: "run-1", tool: "read", status: "running", text: "docs/spec.md" },
    { kind: "final", runId: "run-1", text: "Готово." }
  ];
  for (const event of events) {
    state = reduceAgentChatEvent(state, event);
  }

  assert.equal(state.running, false);
  assert.equal(state.finalText, "Готово.");
  assert.deepEqual(state.transient.map(item => item.kind), ["status", "reasoning", "tool"]);
});

test("a new connecting run clears terminal presentation and ignores late events from the previous run", () => {
  let state = createAgentChatState("T-1222");
  state = reduceAgentChatEvent(state, {
    kind: "error",
    runId: "run-failed",
    text: "OpenCode transport is unavailable."
  });
  state = reduceAgentChatEvent(state, {
    kind: "status",
    runId: "run-retry",
    status: "connecting",
    text: "Connecting to OpenCode…"
  });

  assert.equal(state.runId, "run-retry");
  assert.equal(state.running, true);
  assert.equal(state.status, "connecting");
  assert.equal(state.finalText, undefined);
  assert.deepEqual(state.transient.map(item => item.runId), ["run-retry"]);

  const afterLateEvent = reduceAgentChatEvent(state, {
    kind: "reasoning",
    runId: "run-failed",
    text: "late stale event"
  });
  assert.strictEqual(afterLateEvent, state);
});

test("diagnostics redact credentials and authorization material", () => {
  assert.equal(
    sanitizedAgentDiagnostic("LM Studio unavailable; Authorization: Bearer abc123; api_key=secret"),
    "LM Studio unavailable; Authorization: [redacted]; api_key=[redacted]");
});
