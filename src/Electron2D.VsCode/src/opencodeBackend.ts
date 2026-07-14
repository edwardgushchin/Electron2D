/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { createServer } from "node:net";
import type {
  AssistantMessage,
  Event,
  Config,
  OpencodeClient,
  Part
} from "@opencode-ai/sdk";
import {
  agentSessionKey,
  sanitizedAgentDiagnostic,
  type AgentPresentationEvent
} from "./agentChat.js";

const SESSION_TITLE_PREFIX = "Electron2D TaskBoard";
const OPEN_CODE_CHAT_MODEL = { providerID: "openai", modelID: "gpt-5.6-sol" } as const;

export interface OpenCodeRunRequest {
  readonly taskId: string;
  readonly taskUid: string;
  readonly runId: string;
  readonly context: string;
  readonly prompt: string;
  readonly emit: (event: AgentPresentationEvent) => void;
}

interface ActiveRun {
  readonly taskId: string;
  readonly taskUid: string;
  readonly runId: string;
  readonly emit: (event: AgentPresentationEvent) => void;
}

interface OpenCodeRuntime {
  readonly client: OpencodeClient;
  readonly close: () => void;
  readonly signal: AbortSignal;
}

type CreateOpencode = typeof import("@opencode-ai/sdk").createOpencode;

export class OpenCodeBackend {
  private runtimePromise: Promise<OpenCodeRuntime> | undefined;
  private runtime: OpenCodeRuntime | undefined;
  private readonly sessionIds = new Map<string, string>();
  private readonly activeBySession = new Map<string, ActiveRun>();
  private disposed = false;

  public constructor(
    private readonly workspacePath: string,
    private readonly workspaceUri: string) {
  }

  public async run(request: OpenCodeRunRequest): Promise<string> {
    if (this.disposed) {
      throw new Error("OpenCode backend is disposed.");
    }

    request.emit({ kind: "status", runId: request.runId, status: "connecting", text: "Connecting to OpenCode…" });
    return await runWithOpenCodeTransportRecovery(
      async () => await this.runWithRuntime(await this.ensureRuntime(), request),
      () => this.resetRuntime());
  }

  private async runWithRuntime(runtime: OpenCodeRuntime, request: OpenCodeRunRequest): Promise<string> {
    const sessionID = await this.sessionFor(runtime.client, request.taskUid);
    if (this.activeBySession.has(sessionID)) {
      throw new Error(`Task '${request.taskId}' already has an active agent run.`);
    }

    this.activeBySession.set(sessionID, request);
    request.emit({ kind: "status", runId: request.runId, status: "running", text: "Agent is working…" });
    try {
      await requireData(runtime.client.session.prompt({
        path: { id: sessionID },
        query: { directory: this.workspacePath },
        body: openCodePromptBody(request.context, true)
      }), "OpenCode rejected the canonical task context.");
      const response = await requireData(runtime.client.session.prompt({
        path: { id: sessionID },
        query: { directory: this.workspacePath },
        body: openCodePromptBody(request.prompt, false)
      }), "OpenCode failed to produce an agent response.");
      return finalTextFromOpenCodeResponse(response);
    } finally {
      this.activeBySession.delete(sessionID);
    }
  }

  public async cancel(taskUid: string): Promise<void> {
    const runtime = await this.runtimePromise;
    if (!runtime) {
      return;
    }
    const sessionID = this.sessionIds.get(agentSessionKey(this.workspaceUri, taskUid));
    if (!sessionID) {
      return;
    }
    await requireData(runtime.client.session.abort({
      path: { id: sessionID },
      query: { directory: this.workspacePath }
    }), "OpenCode could not cancel the agent run.");
  }

  public async respondPermission(
    taskUid: string,
    permissionID: string,
    response: "once" | "session" | "reject"): Promise<void> {
    const runtime = await this.ensureRuntime();
    const sessionID = await this.sessionFor(runtime.client, taskUid);
    await requireData(runtime.client.postSessionIdPermissionsPermissionId({
      path: { id: sessionID, permissionID },
      query: { directory: this.workspacePath },
      body: { response: response === "session" ? "always" : response }
    }), "OpenCode could not apply the permission response.");
  }

  public dispose(): void {
    this.disposed = true;
    const pending = this.runtimePromise;
    const activeRuntime = this.runtime;
    this.resetRuntime();
    if (!activeRuntime && pending) {
      void pending.then(runtime => runtime.close(), () => undefined);
    }
    this.sessionIds.clear();
    this.activeBySession.clear();
  }

  private async ensureRuntime(): Promise<OpenCodeRuntime> {
    if (!this.runtimePromise) {
      this.runtimePromise = this.startRuntime();
    }
    const pending = this.runtimePromise;
    try {
      const runtime = await pending;
      if (this.runtimePromise !== pending || this.disposed) {
        runtime.close();
        throw new Error("OpenCode runtime was replaced before it became ready.");
      }
      if (!this.runtime) {
        this.runtime = runtime;
        void this.consumeEvents(runtime);
      }
      return runtime;
    } catch (error) {
      if (this.runtimePromise === pending) {
        this.runtimePromise = undefined;
      }
      throw error;
    }
  }

  private async startRuntime(): Promise<OpenCodeRuntime> {
    const port = await reserveLoopbackPort();
    const abortController = new AbortController();
    let created: Awaited<ReturnType<CreateOpencode>>;
    try {
      const { createOpencode } = await import("@opencode-ai/sdk");
      created = await createOpencode({
        hostname: "127.0.0.1",
        port,
        signal: abortController.signal,
        timeout: 10_000,
        config: openCodeServerConfig()
      });
    } catch (error) {
      abortController.abort();
      throw new Error(sanitizedAgentDiagnostic(
        `OpenCode server could not start. Verify that 'opencode' is installed and available in PATH. ${errorMessage(error)}`));
    }

    return {
      client: created.client,
      signal: abortController.signal,
      close: () => {
        abortController.abort();
        created.server.close();
      }
    };
  }

  private resetRuntime(expected?: OpenCodeRuntime): void {
    if (expected && this.runtime !== expected) {
      return;
    }
    const runtime = this.runtime;
    this.runtime = undefined;
    this.runtimePromise = undefined;
    this.sessionIds.clear();
    runtime?.close();
  }

  private async sessionFor(client: OpencodeClient, taskUid: string): Promise<string> {
    const key = agentSessionKey(this.workspaceUri, taskUid);
    const known = this.sessionIds.get(key);
    if (known) {
      return known;
    }

    const title = `${SESSION_TITLE_PREFIX} ${taskUid}`;
    const listed = await requireData(client.session.list({ query: { directory: this.workspacePath } }), "OpenCode session discovery failed.");
    const existing = listed.find(session => session.title === title);
    const sessionID = existing?.id ?? (await requireData(client.session.create({
      query: { directory: this.workspacePath },
      body: { title }
    }), "OpenCode session creation failed.")).id;
    this.sessionIds.set(key, sessionID);
    return sessionID;
  }

  private async consumeEvents(runtime: OpenCodeRuntime): Promise<void> {
    try {
      const subscription = await runtime.client.event.subscribe({
        query: { directory: this.workspacePath },
        signal: runtime.signal
      });
      for await (const event of subscription.stream) {
        this.consumeEvent(event);
      }
    } catch (error) {
      if (runtime.signal.aborted) {
        return;
      }
      const activeRuns = [...this.activeBySession.values()];
      this.resetRuntime(runtime);
      for (const active of activeRuns) {
        active.emit({
          kind: "error",
          runId: active.runId,
          text: openCodeTransportDiagnostic(error)
        });
      }
    }
  }

  private consumeEvent(event: Event): void {
    const sessionID = eventSessionID(event);
    const active = sessionID ? this.activeBySession.get(sessionID) : undefined;
    if (!active) {
      return;
    }

    if (event.type === "message.part.updated") {
      const { part, delta } = event.properties;
      if (part.type === "reasoning") {
        active.emit({ kind: "reasoning", runId: active.runId, text: delta ?? part.text });
      } else if (part.type === "text") {
        active.emit({ kind: "answer", runId: active.runId, text: part.text });
      } else if (part.type === "tool") {
        const title = "title" in part.state && typeof part.state.title === "string"
          ? part.state.title
          : part.tool;
        active.emit({
          kind: "tool",
          runId: active.runId,
          tool: part.tool,
          status: part.state.status,
          text: title
        });
      }
    } else if (event.type === "permission.updated") {
      active.emit({
        kind: "permission",
        runId: active.runId,
        permissionId: event.properties.id,
        text: event.properties.pattern
          ? `${event.properties.title}: ${Array.isArray(event.properties.pattern) ? event.properties.pattern.join(", ") : event.properties.pattern}`
          : event.properties.title
      });
    } else if (event.type === "session.status") {
      active.emit({
        kind: "status",
        runId: active.runId,
        status: "running",
        text: event.properties.status.type === "retry" ? event.properties.status.message : `OpenCode: ${event.properties.status.type}`
      });
    } else if (event.type === "session.error") {
      active.emit({
        kind: "error",
        runId: active.runId,
        text: event.properties.error
          ? openCodeAssistantDiagnostic(event.properties.error)
          : "OpenCode session failed."
      });
    }
  }
}

export function openCodeServerConfig(): Config {
  return {
    share: "disabled",
    permission: {
      edit: "ask",
      bash: "ask",
      webfetch: "ask",
      doom_loop: "ask",
      external_directory: "deny"
    },
    tools: {
      task: false,
      websearch: false
    }
  };
}

export function openCodePromptBody(text: string, noReply: boolean) {
  const parts = [{ type: "text" as const, text }];
  return noReply
    ? { model: OPEN_CODE_CHAT_MODEL, noReply: true as const, parts }
    : { model: OPEN_CODE_CHAT_MODEL, parts };
}

export async function runWithOpenCodeTransportRecovery<T>(
  start: () => Promise<T>,
  reset: () => void | Promise<void>): Promise<T> {
  try {
    return await start();
  } catch (error) {
    if (!isOpenCodeConnectionRefused(error)) {
      throw new Error(openCodeTransportDiagnostic(error));
    }
  }

  await reset();
  try {
    return await start();
  } catch (error) {
    throw new Error(openCodeTransportDiagnostic(error));
  }
}

export function openCodeTransportDiagnostic(error: unknown): string {
  if (isOpenCodeConnectionRefused(error)) {
    return "OpenCode transport is unavailable after one automatic restart. Check the OpenCode installation and configuration, then retry.";
  }

  const detail = errorMessage(error);
  if (/fetch failed/i.test(detail)) {
    return "OpenCode transport request failed before a usable response was received. Retry the message; if the problem continues, restart OpenCode.";
  }
  return sanitizedAgentDiagnostic(detail || "OpenCode request failed without a diagnostic.");
}

export function finalTextFromOpenCodeResponse(response: {
  readonly info: Pick<AssistantMessage, "error">;
  readonly parts: readonly Part[];
}): string {
  if (response.info.error) {
    throw new Error(openCodeAssistantDiagnostic(response.info.error));
  }

  const finalText = response.parts
    .filter((part): part is Extract<Part, { readonly type: "text" }> => part.type === "text")
    .map(part => part.text)
    .join("")
    .trim();
  if (!finalText) {
    throw new Error("OpenCode returned an empty final response.");
  }
  return finalText;
}

function openCodeAssistantDiagnostic(error: NonNullable<AssistantMessage["error"]>): string {
  const detail = error.name === "MessageOutputLengthError"
    ? "The model reached its output token limit."
    : "message" in error.data && typeof error.data.message === "string"
      ? error.data.message
      : "The provider did not return an error description.";
  return sanitizedAgentDiagnostic(`OpenCode model response failed. ${detail}`);
}

function eventSessionID(event: Event): string | undefined {
  if (event.type === "message.part.updated") {
    return event.properties.part.sessionID;
  }
  if (event.type === "permission.updated") {
    return event.properties.sessionID;
  }
  if (event.type === "session.status" || event.type === "session.idle") {
    return event.properties.sessionID;
  }
  if (event.type === "session.error") {
    return event.properties.sessionID;
  }
  return undefined;
}

async function reserveLoopbackPort(): Promise<number> {
  return await new Promise((resolve, reject) => {
    const server = createServer();
    server.unref();
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => {
      const address = server.address();
      const port = typeof address === "object" && address ? address.port : undefined;
      server.close(error => error ? reject(error) : port ? resolve(port) : reject(new Error("Could not reserve a loopback port.")));
    });
  });
}

async function requireData<T>(
  request: Promise<{ readonly data?: T; readonly error?: unknown }>,
  message: string): Promise<NonNullable<T>> {
  const response = await request;
  if (response.data === undefined) {
    throw new Error(`${message} ${errorMessage(response.error)}`.trim());
  }
  return response.data as NonNullable<T>;
}

function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }
  if (isRecord(error) && typeof error.message === "string") {
    return error.message;
  }
  return "";
}

function isOpenCodeConnectionRefused(error: unknown): boolean {
  const visited = new Set<unknown>();
  let current = error;
  for (let depth = 0; depth < 8 && current !== undefined && current !== null && !visited.has(current); depth++) {
    visited.add(current);
    if (isRecord(current)) {
      if (typeof current.code === "string" && current.code.toUpperCase() === "ECONNREFUSED") {
        return true;
      }
      if (typeof current.message === "string" && /\bECONNREFUSED\b|connection refused|connect refused/i.test(current.message)) {
        return true;
      }
      current = current.cause;
      continue;
    }
    break;
  }
  return false;
}

function isRecord(value: unknown): value is Readonly<Record<string, unknown>> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
