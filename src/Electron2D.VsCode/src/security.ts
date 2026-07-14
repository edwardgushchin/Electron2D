/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { isProjectRelativeFilePath } from "./workspace.js";

export type WebviewMessage =
  | { readonly type: "refresh" }
  | { readonly type: "openFile"; readonly path: string }
  | { readonly type: "openTask"; readonly taskId: string; readonly navigation: "direct" | "internal" }
  | { readonly type: "navigateBack" }
  | { readonly type: "closeTaskDetails" }
  | { readonly type: "manageTags" }
  | { readonly type: "addTaskTag"; readonly taskId: string; readonly expectedTaskRevision: number; readonly expectedBoardRevision: number }
  | { readonly type: "comment"; readonly taskId: string; readonly text: string; readonly expectedTaskRevision: number }
  | { readonly type: "sendAgentMessage"; readonly taskId: string; readonly text: string; readonly expectedTaskRevision: number }
  | { readonly type: "cancelAgentRun"; readonly taskId: string }
  | { readonly type: "respondAgentPermission"; readonly taskId: string; readonly permissionId: string; readonly response: "once" | "session" | "reject" }
  | { readonly type: "create"; readonly title: string; readonly description: string; readonly priority: string; readonly deadline?: string }
  | { readonly type: "edit"; readonly taskId: string; readonly title: string; readonly description: string; readonly priority: string; readonly deadline?: string | null; readonly expectedTaskRevision: number }
  | { readonly type: "move"; readonly taskId: string; readonly groupId: string | null; readonly rank: string; readonly expectedBoardRevision: number }
  | { readonly type: "attach"; readonly taskId: string; readonly expectedTaskRevision: number }
  | { readonly type: "archive"; readonly taskId: string; readonly expectedTaskRevision: number; readonly expectedBoardRevision: number }
  | { readonly type: "unarchive"; readonly taskId: string; readonly expectedTaskRevision: number; readonly expectedBoardRevision: number }
  | { readonly type: "removeAttachment"; readonly taskId: string; readonly attachmentId: string; readonly expectedTaskRevision: number }
  | { readonly type: "setAttachmentPreview"; readonly taskId: string; readonly attachmentId: string; readonly expectedTaskRevision: number }
  | { readonly type: "clearAttachmentPreview"; readonly taskId: string; readonly expectedTaskRevision: number }
  | { readonly type: "submit"; readonly taskId: string; readonly reason: string; readonly expectedTaskRevision: number }
  | { readonly type: "reopen"; readonly taskId: string; readonly reason: string; readonly expectedTaskRevision: number }
  | { readonly type: "accept"; readonly taskId: string; readonly reason: string; readonly expectedTaskRevision: number }
  | { readonly type: "requestChanges"; readonly taskId: string; readonly reason: string; readonly expectedTaskRevision: number };

export function buildContentSecurityPolicy(cspSource: string, nonce: string): string {
  return [
    "default-src 'none'",
    `style-src ${cspSource}`,
    `img-src ${cspSource} data:`,
    `script-src 'nonce-${nonce}'`,
    "font-src 'none'",
    "connect-src 'none'",
    "frame-src 'none'",
    "object-src 'none'",
    "base-uri 'none'",
    "form-action 'none'"
  ].join("; ");
}

export function parseWebviewMessage(value: unknown): WebviewMessage | undefined {
  if (!isPlainRecord(value) || typeof value.type !== "string") {
    return undefined;
  }

  if (value.type === "refresh" && Object.keys(value).length === 1) {
    return { type: "refresh" };
  }

  if (value.type === "manageTags" && Object.keys(value).length === 1) {
    return { type: "manageTags" };
  }

  if (value.type === "addTaskTag" &&
      Object.keys(value).length === 4 &&
      isTaskId(value.taskId) &&
      isRevision(value.expectedTaskRevision) &&
      isRevision(value.expectedBoardRevision)) {
    return {
      type: "addTaskTag",
      taskId: value.taskId,
      expectedTaskRevision: value.expectedTaskRevision,
      expectedBoardRevision: value.expectedBoardRevision
    };
  }

  if (value.type === "openFile" &&
      Object.keys(value).length === 2 &&
      isProjectRelativeFilePath(value.path)) {
    return { type: "openFile", path: value.path };
  }

  if ((value.type === "navigateBack" || value.type === "closeTaskDetails") &&
      Object.keys(value).length === 1) {
    return { type: value.type };
  }

  if (value.type === "openTask" &&
      Object.keys(value).length === 3 &&
      isTaskId(value.taskId) &&
      (value.navigation === "direct" || value.navigation === "internal")) {
    return { type: "openTask", taskId: value.taskId, navigation: value.navigation };
  }

  if (value.type === "comment" &&
      Object.keys(value).length === 4 &&
      isTaskId(value.taskId) &&
      isNonEmptyText(value.text, 16_384) &&
      isRevision(value.expectedTaskRevision)) {
    return {
      type: "comment",
      taskId: value.taskId,
      text: value.text,
      expectedTaskRevision: value.expectedTaskRevision
    };
  }

  if (value.type === "sendAgentMessage" &&
      Object.keys(value).length === 4 &&
      isTaskId(value.taskId) &&
      isNonEmptyText(value.text, 16_384) &&
      isRevision(value.expectedTaskRevision)) {
    return {
      type: "sendAgentMessage",
      taskId: value.taskId,
      text: value.text,
      expectedTaskRevision: value.expectedTaskRevision
    };
  }

  if (value.type === "cancelAgentRun" &&
      Object.keys(value).length === 2 &&
      isTaskId(value.taskId)) {
    return { type: "cancelAgentRun", taskId: value.taskId };
  }

  if (value.type === "respondAgentPermission" &&
      Object.keys(value).length === 4 &&
      isTaskId(value.taskId) &&
      isTaskId(value.permissionId) &&
      (value.response === "once" || value.response === "session" || value.response === "reject")) {
    return {
      type: "respondAgentPermission",
      taskId: value.taskId,
      permissionId: value.permissionId,
      response: value.response
    };
  }

  if (value.type === "create" &&
      ((Object.keys(value).length === 4 && !Object.hasOwn(value, "deadline")) ||
       (Object.keys(value).length === 5 && Object.hasOwn(value, "deadline"))) &&
      isNonEmptyText(value.title, 512) &&
      isText(value.description, 262_144) &&
      isPriority(value.priority) &&
      (value.deadline === undefined || isDeadline(value.deadline))) {
    return {
      type: "create",
      title: value.title.trim(),
      description: value.description,
      priority: value.priority,
      ...(value.deadline === undefined ? {} : { deadline: value.deadline })
    };
  }

  if (value.type === "edit" &&
      ((Object.keys(value).length === 6 && !Object.hasOwn(value, "deadline")) ||
       (Object.keys(value).length === 7 && Object.hasOwn(value, "deadline"))) &&
      isTaskId(value.taskId) &&
      isNonEmptyText(value.title, 512) &&
      isText(value.description, 262_144) &&
      isPriority(value.priority) &&
      (value.deadline === undefined || value.deadline === null || isDeadline(value.deadline)) &&
      isRevision(value.expectedTaskRevision)) {
    return {
      type: "edit",
      taskId: value.taskId,
      title: value.title,
      description: value.description,
      priority: value.priority,
      ...(value.deadline === undefined ? {} : { deadline: value.deadline }),
      expectedTaskRevision: value.expectedTaskRevision
    };
  }

  if (value.type === "move" &&
      Object.keys(value).length === 5 &&
      isTaskId(value.taskId) &&
      (value.groupId === null || isTaskId(value.groupId)) &&
      typeof value.rank === "string" && /^[A-Za-z0-9._~-]{1,128}$/.test(value.rank) &&
      isRevision(value.expectedBoardRevision)) {
    return {
      type: "move",
      taskId: value.taskId,
      groupId: value.groupId,
      rank: value.rank,
      expectedBoardRevision: value.expectedBoardRevision
    };
  }

  if ((value.type === "attach" || value.type === "clearAttachmentPreview") &&
      Object.keys(value).length === 3 &&
      isTaskId(value.taskId) &&
      isRevision(value.expectedTaskRevision)) {
    return { type: value.type, taskId: value.taskId, expectedTaskRevision: value.expectedTaskRevision };
  }

  if ((value.type === "archive" || value.type === "unarchive") &&
      Object.keys(value).length === 4 &&
      isTaskId(value.taskId) &&
      isRevision(value.expectedTaskRevision) &&
      isRevision(value.expectedBoardRevision)) {
    return {
      type: value.type,
      taskId: value.taskId,
      expectedTaskRevision: value.expectedTaskRevision,
      expectedBoardRevision: value.expectedBoardRevision
    };
  }

  if ((value.type === "submit" || value.type === "reopen" || value.type === "accept" ||
      value.type === "requestChanges") &&
      Object.keys(value).length === 4 &&
      isTaskId(value.taskId) &&
      isNonEmptyText(value.reason, 16_384) &&
      isRevision(value.expectedTaskRevision)) {
    return {
      type: value.type,
      taskId: value.taskId,
      reason: value.reason,
      expectedTaskRevision: value.expectedTaskRevision
    };
  }

  if ((value.type === "removeAttachment" || value.type === "setAttachmentPreview") &&
      Object.keys(value).length === 4 &&
      isTaskId(value.taskId) &&
      isTaskId(value.attachmentId) &&
      isRevision(value.expectedTaskRevision)) {
    return {
      type: value.type,
      taskId: value.taskId,
      attachmentId: value.attachmentId,
      expectedTaskRevision: value.expectedTaskRevision
    };
  }

  return undefined;
}

function isPlainRecord(value: unknown): value is Record<string, unknown> {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const prototype = Object.getPrototypeOf(value) as object | null;
  return prototype === Object.prototype || prototype === null;
}

function isTaskId(value: unknown): value is string {
  return typeof value === "string" && /^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$/.test(value);
}

function isNonEmptyText(value: unknown, maximumLength: number): value is string {
  return typeof value === "string" && value.trim().length > 0 && value.length <= maximumLength;
}

function isText(value: unknown, maximumLength: number): value is string {
  return typeof value === "string" && value.length <= maximumLength;
}

function isPriority(value: unknown): value is string {
  return typeof value === "string" && /^P[0-3]$/.test(value);
}

function isDeadline(value: unknown): value is string {
  if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false;
  }

  const [year, month, day] = value.split("-").map(Number);
  const date = new Date(Date.UTC(year!, month! - 1, day!));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month! - 1 && date.getUTCDate() === day;
}

function isRevision(value: unknown): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value >= 1;
}
