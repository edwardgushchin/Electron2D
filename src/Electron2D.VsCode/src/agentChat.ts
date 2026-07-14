/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */

export type AgentRunStatus = "connecting" | "running" | "waiting-permission" | "cancelled" | "completed" | "error";

export type AgentPresentationEvent =
  | { readonly kind: "status"; readonly runId: string; readonly status: AgentRunStatus; readonly text: string }
  | { readonly kind: "reasoning" | "commentary"; readonly runId: string; readonly text: string }
  | { readonly kind: "answer"; readonly runId: string; readonly text: string }
  | { readonly kind: "tool"; readonly runId: string; readonly tool: string; readonly status: string; readonly text: string }
  | { readonly kind: "permission"; readonly runId: string; readonly permissionId: string; readonly text: string }
  | { readonly kind: "final"; readonly runId: string; readonly text: string }
  | { readonly kind: "error"; readonly runId: string; readonly text: string };

export interface AgentChatState {
  readonly taskId: string;
  readonly runId?: string;
  readonly running: boolean;
  readonly status?: AgentRunStatus;
  readonly finalText?: string;
  readonly transient: readonly AgentPresentationEvent[];
  readonly permission?: Extract<AgentPresentationEvent, { readonly kind: "permission" }>;
}

export function agentSessionKey(workspaceUri: string, taskUid: string): string {
  return `${workspaceUri.length}:${workspaceUri}|${taskUid.length}:${taskUid}`;
}

export function createAgentChatState(taskId: string): AgentChatState {
  return { taskId, running: false, transient: [] };
}

export function reduceAgentChatEvent(state: AgentChatState, event: AgentPresentationEvent): AgentChatState {
  if (state.runId && state.runId !== event.runId) {
    if (state.running || event.kind !== "status" || event.status !== "connecting") {
      return state;
    }
    state = createAgentChatState(state.taskId);
  }

  if (event.kind === "final") {
    const { permission: _permission, ...withoutPermission } = state;
    return {
      ...withoutPermission,
      runId: event.runId,
      running: false,
      status: "completed",
      finalText: event.text,
    };
  }
  if (event.kind === "answer") {
    return {
      ...state,
      runId: event.runId,
      running: true,
      status: "running",
      finalText: event.text
    };
  }
  if (event.kind === "error") {
    const { permission: _permission, ...withoutPermission } = state;
    return {
      ...withoutPermission,
      runId: event.runId,
      running: false,
      status: "error",
      transient: [...state.transient, event]
    };
  }
  if (event.kind === "status") {
    const permission = event.status === "waiting-permission" ? state.permission : undefined;
    return {
      ...state,
      runId: event.runId,
      running: event.status === "connecting" || event.status === "running" || event.status === "waiting-permission",
      status: event.status,
      ...(permission ? { permission } : {}),
      transient: [...state.transient, event]
    };
  }
  if (event.kind === "permission") {
    return {
      ...state,
      runId: event.runId,
      running: true,
      status: "waiting-permission",
      permission: event,
      transient: [...state.transient, event]
    };
  }
  return {
    ...state,
    runId: event.runId,
    running: true,
    status: "running",
    transient: [...state.transient, event]
  };
}

export function sanitizedAgentDiagnostic(value: string): string {
  return value
    .replace(/(authorization\s*:\s*)(?:bearer\s+)?[^;\r\n]+/gi, "$1[redacted]")
    .replace(/((?:api[_-]?key|token|password)\s*[=:]\s*)[^;\s]+/gi, "$1[redacted]");
}
