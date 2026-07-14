/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { spawn } from "node:child_process";
import { randomBytes } from "node:crypto";

export function buildTaskArguments(projectRoot: string, taskArguments: readonly string[]): string[] {
  return ["tasks", ...taskArguments, "--project", projectRoot, "--format", "json"];
}

export function boardReadArguments(): string[] {
  return ["board", "--compact", "true", "--include-archived", "true"];
}

export function taskReadArguments(taskId: string): string[] {
  return ["get", taskId];
}

export function reopenTaskArguments(taskId: string, taskRevision: number, reason: string): string[] {
  return ["reopen", taskId, "--reason", reason, "--expected-revision", String(taskRevision)];
}

export function archiveTaskArguments(taskId: string, taskRevision: number, boardRevision: number): string[] {
  return [
    "archive", taskId,
    "--expected-revision", String(taskRevision),
    "--expected-board-revision", String(boardRevision)
  ];
}

export function unarchiveTaskArguments(taskId: string, taskRevision: number, boardRevision: number): string[] {
  return [
    "unarchive", taskId,
    "--expected-revision", String(taskRevision),
    "--expected-board-revision", String(boardRevision)
  ];
}

export function isConfirmedDialogResult(result: string | undefined, actionLabel: string): boolean {
  return result === actionLabel;
}

export function isCompactBoardRead(arguments_: readonly string[]): boolean {
  const expected = boardReadArguments();
  return arguments_.length === expected.length &&
    arguments_.every((argument, index) => argument === expected[index]);
}

export class InFlightRequestBroker {
  private readonly requests = new Map<string, Promise<unknown>>();

  public run<T>(key: string, start: () => Promise<T>): Promise<T> {
    const existing = this.requests.get(key) as Promise<T> | undefined;
    if (existing) {
      return existing;
    }

    const request = start();
    this.requests.set(key, request);
    void request.then(
      () => this.release(key, request),
      () => this.release(key, request));
    return request;
  }

  private release(key: string, request: Promise<unknown>): void {
    if (this.requests.get(key) === request) {
      this.requests.delete(key);
    }
  }
}

export class WorkspaceCommandScheduler {
  private readonly tails = new Map<string, Promise<void>>();

  public run<T>(key: string, start: () => Promise<T>): Promise<T> {
    const previous = this.tails.get(key) ?? Promise.resolve();
    const result = previous.then(start);
    const tail = result.then(
      () => undefined,
      () => undefined);
    this.tails.set(key, tail);
    void tail.then(() => {
      if (this.tails.get(key) === tail) {
        this.tails.delete(key);
      }
    });
    return result;
  }
}

export class TrailingRefreshCoordinator {
  private active: Promise<void> | undefined;
  private requested = false;

  public constructor(private readonly refresh: () => Promise<void>) {}

  public request(): Promise<void> {
    this.requested = true;
    if (this.active) {
      return this.active;
    }

    const active = this.drain();
    this.active = active;
    void active.then(
      () => this.release(active),
      () => this.release(active));
    return active;
  }

  private async drain(): Promise<void> {
    let latestError: unknown;
    while (this.requested) {
      this.requested = false;
      try {
        await this.refresh();
        latestError = undefined;
      } catch (error) {
        latestError = error;
      }
    }

    if (latestError !== undefined) {
      throw latestError;
    }
  }

  private release(active: Promise<void>): void {
    if (this.active === active) {
      this.active = undefined;
    }
  }
}

export async function runWithTransientTaskboardLockRetry<T>(
  start: () => Promise<T>,
  pause: (failedAttempt: number) => Promise<void>,
  maxAttempts = 3): Promise<T> {
  for (let attempt = 1; ; attempt++) {
    try {
      return await start();
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      if (attempt >= maxAttempts || !/Taskboard is locked by another writer/i.test(message)) {
        throw error;
      }
      await pause(attempt);
    }
  }
}

export async function runWithBoardRevisionConflictRetry<T>(
  initialBoardRevision: number,
  start: (expectedBoardRevision: number) => Promise<T>,
  refresh: () => Promise<number | undefined>,
  maxAttempts = 3,
  inapplicableMessage = "The move is no longer applicable after refreshing the task board."): Promise<T> {
  let expectedBoardRevision = initialBoardRevision;
  for (let attempt = 1; ; attempt++) {
    try {
      return await start(expectedBoardRevision);
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      if (attempt >= maxAttempts ||
          !/Taskboard revision conflict:\s*expected \d+,\s*actual \d+\./i.test(message)) {
        throw error;
      }

      const refreshedBoardRevision = await refresh();
      if (refreshedBoardRevision === undefined) {
        throw new Error(inapplicableMessage);
      }
      expectedBoardRevision = refreshedBoardRevision;
    }
  }
}

export interface RefreshedTaskRevision {
  readonly taskUid: string;
  readonly revision: number;
}

export function isTaskRevisionConflict(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error);
  return /Task revision conflict:\s*expected \d+,\s*actual \d+\./i.test(message);
}

export async function runWithTaskRevisionConflictRetry<T>(
  initialTaskRevision: number,
  expectedTaskUid: string,
  start: (expectedTaskRevision: number) => Promise<T>,
  refresh: () => Promise<RefreshedTaskRevision | undefined>,
  maxAttempts = 2,
  inapplicableMessage = "The human message no longer applies after refreshing the task."): Promise<T> {
  let expectedTaskRevision = initialTaskRevision;
  const boundedAttempts = Math.min(Math.max(maxAttempts, 1), 2);
  for (let attempt = 1; ; attempt++) {
    try {
      return await start(expectedTaskRevision);
    } catch (error) {
      if (attempt >= boundedAttempts ||
          !isTaskRevisionConflict(error)) {
        throw error;
      }

      const refreshed = await refresh();
      if (!refreshed || refreshed.taskUid !== expectedTaskUid ||
          !Number.isSafeInteger(refreshed.revision) || refreshed.revision < 1) {
        throw new Error(inapplicableMessage);
      }
      expectedTaskRevision = refreshed.revision;
    }
  }
}

export function resolveCliExecutable(configuredPath: string | undefined, platform: NodeJS.Platform): string {
  const trimmed = configuredPath?.trim();
  if (trimmed) {
    return trimmed;
  }

  return platform === "win32" ? "e2d.exe" : "e2d";
}

export interface SpawnRequest {
  readonly file: string;
  readonly args: readonly string[];
  readonly options: {
    readonly cwd: string;
    readonly shell: false;
    readonly windowsHide: true;
  };
}

export function buildSpawnRequest(file: string, args: readonly string[], cwd: string): SpawnRequest {
  return {
    file,
    args: [...args],
    options: { cwd, shell: false, windowsHide: true }
  };
}

export function parseCliEnvelope(stdout: string): unknown {
  let value: unknown;
  try {
    value = JSON.parse(stdout) as unknown;
  } catch {
    throw new Error("e2d did not return valid JSON.");
  }

  if (!isRecord(value) || value.schemaVersion !== 1 || typeof value.succeeded !== "boolean") {
    throw new Error("e2d returned an unsupported JSON envelope.");
  }

  if (!value.succeeded) {
    const message = typeof value.message === "string" && value.message.length > 0
      ? value.message
      : "e2d task command failed.";
    const diagnosticMessages = Array.isArray(value.diagnostics)
      ? value.diagnostics.flatMap(diagnostic => {
        if (!isRecord(diagnostic) || typeof diagnostic.message !== "string") {
          return [];
        }

        return [typeof diagnostic.code === "string"
          ? `${diagnostic.code}: ${diagnostic.message}`
          : diagnostic.message];
      })
      : [];
    throw new Error(diagnosticMessages.length > 0 ? `${message} ${diagnosticMessages.join("; ")}` : message);
  }

  if (value.exitCode !== 0 || !("data" in value)) {
    throw new Error("e2d returned an incomplete successful JSON envelope.");
  }

  return value.data;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export async function runTaskCli(
  executable: string,
  projectRoot: string,
  taskArguments: readonly string[],
  timeoutMilliseconds = 30_000): Promise<unknown> {
  return await runCliProcess(executable, projectRoot, taskArguments, undefined, undefined, timeoutMilliseconds);
}

export interface HumanDecisionBridge {
  readonly taskArguments: readonly string[];
  readonly environmentCapability: string;
  readonly stdin: string;
}

export type HumanMessageBridge = HumanDecisionBridge;

export function createHumanDecisionBridge(
  taskId: string,
  expectedTaskRevision: number,
  decision: "accept" | "request-changes",
  reason: string,
  entropy: Uint8Array): HumanDecisionBridge {
  if (entropy.byteLength < 32) {
    throw new Error("Human decision capability requires at least 256 bits of entropy.");
  }

  const capability = Buffer.from(entropy).toString("base64url");
  return {
    taskArguments: ["__human-decision", taskId, "--expected-revision", String(expectedTaskRevision)],
    environmentCapability: capability,
    stdin: JSON.stringify({
      protocol: "Electron2D.TaskHumanDecision/1",
      capability,
      decision,
      reason
    })
  };
}

export function createHumanMessageBridge(
  taskId: string,
  expectedTaskRevision: number,
  text: string,
  entropy: Uint8Array): HumanMessageBridge {
  if (entropy.byteLength < 32) {
    throw new Error("Human message capability requires at least 256 bits of entropy.");
  }

  const capability = Buffer.from(entropy).toString("base64url");
  return {
    taskArguments: ["__human-message", taskId, "--expected-revision", String(expectedTaskRevision)],
    environmentCapability: capability,
    stdin: JSON.stringify({
      protocol: "Electron2D.TaskHumanMessage/1",
      capability,
      text
    })
  };
}

export async function runHumanDecisionCli(
  executable: string,
  projectRoot: string,
  taskId: string,
  expectedTaskRevision: number,
  decision: "accept" | "request-changes",
  reason: string,
  timeoutMilliseconds = 30_000): Promise<unknown> {
  const bridge = createHumanDecisionBridge(
    taskId,
    expectedTaskRevision,
    decision,
    reason,
    randomBytes(32));
  return await runCliProcess(
    executable,
    projectRoot,
    bridge.taskArguments,
    bridge.stdin,
    { E2D_TASKBOARD_HUMAN_CAPABILITY: bridge.environmentCapability },
    timeoutMilliseconds);
}

export async function runHumanMessageCli(
  executable: string,
  projectRoot: string,
  taskId: string,
  expectedTaskRevision: number,
  text: string,
  timeoutMilliseconds = 30_000): Promise<unknown> {
  const bridge = createHumanMessageBridge(taskId, expectedTaskRevision, text, randomBytes(32));
  return await runCliProcess(
    executable,
    projectRoot,
    bridge.taskArguments,
    bridge.stdin,
    { E2D_TASKBOARD_HUMAN_CAPABILITY: bridge.environmentCapability },
    timeoutMilliseconds);
}

export async function runAgentMessageCli(
  executable: string,
  projectRoot: string,
  taskId: string,
  expectedTaskRevision: number,
  text: string,
  agentRunId: string,
  timeoutMilliseconds = 30_000): Promise<unknown> {
  return await runCliProcess(
    executable,
    projectRoot,
    [
      "comment", "add", taskId,
      "--text", text,
      "--agent-run", agentRunId,
      "--expected-revision", String(expectedTaskRevision)
    ],
    undefined,
    { CODEX_INTERNAL_ORIGINATOR_OVERRIDE: "Codex Desktop" },
    timeoutMilliseconds);
}

export async function runAgentContextCli(
  executable: string,
  projectRoot: string,
  taskId: string,
  expectedTaskRevision: number,
  agentRunId: string,
  timeoutMilliseconds = 30_000): Promise<unknown> {
  return await runCliProcess(
    executable,
    projectRoot,
    [
      "context", "checkpoint", taskId,
      "--agent-run", agentRunId,
      "--expected-revision", String(expectedTaskRevision)
    ],
    undefined,
    { CODEX_INTERNAL_ORIGINATOR_OVERRIDE: "Codex Desktop" },
    timeoutMilliseconds);
}

export async function runHumanAcceptanceWorkflow<T>(
  taskId: string,
  acceptanceState: string | undefined,
  expectedTaskRevision: number,
  submit: (revision: number) => Promise<unknown>,
  accept: (revision: number) => Promise<T>): Promise<T> {
  let acceptanceRevision = expectedTaskRevision;
  if (acceptanceState !== "Submitted") {
    const submitted = await submit(expectedTaskRevision);
    acceptanceRevision = taskRevisionFromMutation(submitted, taskId);
  }

  return await accept(acceptanceRevision);
}

export function taskRevisionFromMutation(value: unknown, taskId: string): number {
  if (!isRecord(value) || !isRecord(value.task) || value.task.taskId !== taskId ||
      !Number.isSafeInteger(value.task.revision) || (value.task.revision as number) < 1) {
    throw new Error(`e2d did not return the updated revision for task '${taskId}'.`);
  }

  return value.task.revision as number;
}

async function runCliProcess(
  executable: string,
  projectRoot: string,
  taskArguments: readonly string[],
  stdin: string | undefined,
  environment: Readonly<Record<string, string>> | undefined,
  timeoutMilliseconds: number): Promise<unknown> {
  const request = buildSpawnRequest(executable, buildTaskArguments(projectRoot, taskArguments), projectRoot);
  return await new Promise((resolve, reject) => {
    const child = spawn(request.file, request.args, environment
      ? { ...request.options, env: { ...process.env, ...environment } }
      : request.options);
    const stdout: Buffer[] = [];
    const stderr: Buffer[] = [];
    let outputBytes = 0;
    let settled = false;
    const timeout = setTimeout(() => {
      child.kill();
      finish(new Error(`e2d task command timed out after ${timeoutMilliseconds}ms.`));
    }, timeoutMilliseconds);

    const collect = (target: Buffer[], chunk: Buffer): void => {
      outputBytes += chunk.byteLength;
      if (outputBytes > 32 * 1024 * 1024) {
        child.kill();
        finish(new Error("e2d task command exceeded the 32 MiB output limit."));
        return;
      }

      target.push(chunk);
    };

    const finish = (error?: Error, value?: unknown): void => {
      if (settled) {
        return;
      }

      settled = true;
      clearTimeout(timeout);
      if (error) {
        reject(error);
      } else {
        resolve(value);
      }
    };

    child.stdout.on("data", (chunk: Buffer) => collect(stdout, chunk));
    child.stderr.on("data", (chunk: Buffer) => collect(stderr, chunk));
    child.stdin.end(stdin);
    child.on("error", error => finish(error));
    child.on("close", exitCode => {
      if (settled) {
        return;
      }

      const stdoutText = Buffer.concat(stdout).toString("utf8");
      try {
        finish(undefined, parseCliEnvelope(stdoutText));
      } catch (error) {
        const stderrText = Buffer.concat(stderr).toString("utf8").trim();
        const message = error instanceof Error ? error.message : String(error);
        finish(new Error(stderrText.length > 0 ? `${message} ${stderrText}` : `${message} (exit ${exitCode ?? "unknown"})`));
      }
    });
  });
}
