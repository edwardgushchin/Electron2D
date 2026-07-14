/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import test from "node:test";
import { buildContentSecurityPolicy, parseWebviewMessage } from "../../src/security.js";

test("webview CSP denies ambient content and uses a script nonce", () => {
  const csp = buildContentSecurityPolicy("vscode-webview://fixture", "nonce-123");

  assert.match(csp, /default-src 'none'/);
  assert.match(csp, /script-src 'nonce-nonce-123'/);
  assert.doesNotMatch(csp, /unsafe-inline|unsafe-eval|https?:/);
});

test("message parser accepts allowlisted shapes and rejects prototype or command injection", () => {
  assert.deepEqual(parseWebviewMessage({ type: "refresh" }), { type: "refresh" });
  assert.deepEqual(
    parseWebviewMessage({ type: "openTask", taskId: "T-42", navigation: "direct" }),
    { type: "openTask", taskId: "T-42", navigation: "direct" });
  assert.deepEqual(
    parseWebviewMessage({ type: "openTask", taskId: "T-42", navigation: "internal" }),
    { type: "openTask", taskId: "T-42", navigation: "internal" });
  assert.deepEqual(parseWebviewMessage({ type: "navigateBack" }), { type: "navigateBack" });
  assert.deepEqual(parseWebviewMessage({ type: "closeTaskDetails" }), { type: "closeTaskDetails" });
  assert.equal(parseWebviewMessage({ type: "openTask", taskId: "T-42" }), undefined);
  assert.equal(parseWebviewMessage({ type: "run", command: "rm -rf" }), undefined);
  assert.equal(parseWebviewMessage(Object.create({ type: "refresh" })), undefined);
});

test("message parser rejects the removed workspace reveal action", () => {
  assert.equal(parseWebviewMessage({ type: "revealWorkspace" }), undefined);
  assert.equal(parseWebviewMessage({ type: "revealWorkspace", path: "C:/secret" }), undefined);
});

test("message parser accepts only canonical project-relative file paths", () => {
  assert.deepEqual(
    parseWebviewMessage({ type: "openFile", path: "docs/editor/project-tasks-board.md" }),
    { type: "openFile", path: "docs/editor/project-tasks-board.md" });
  for (const unsafePath of [
    "C:/Windows/System32/drivers/etc/hosts",
    "/etc/passwd",
    "../outside.txt",
    "docs/../outside.txt",
    "file:///C:/secret.txt",
    "docs\\secret.txt",
    "docs/",
    ""
  ]) {
    assert.equal(parseWebviewMessage({ type: "openFile", path: unsafePath }), undefined, unsafePath);
  }
  assert.equal(parseWebviewMessage({ type: "openFile", path: "README.md", viewColumn: 2 }), undefined);
});

test("message parser validates revision-aware mutation payloads", () => {
  assert.deepEqual(
    parseWebviewMessage({ type: "comment", taskId: "T-42", text: "Checked", expectedTaskRevision: 7 }),
    { type: "comment", taskId: "T-42", text: "Checked", expectedTaskRevision: 7 }
  );
  assert.equal(parseWebviewMessage({ type: "comment", taskId: "T-42", text: "", expectedTaskRevision: 7 }), undefined);
  assert.equal(parseWebviewMessage({ type: "comment", taskId: "T-42", text: "Checked", expectedTaskRevision: -1 }), undefined);
  assert.equal(parseWebviewMessage({ type: "comment", taskId: "T-42", text: "Checked", expectedTaskRevision: 7, command: "anything" }), undefined);
});

test("message parser allowlists chat intents without backend endpoints or capabilities", () => {
  assert.deepEqual(parseWebviewMessage({
    type: "sendAgentMessage",
    taskId: "T-1222",
    text: "Проверь контекст",
    expectedTaskRevision: 4
  }), {
    type: "sendAgentMessage",
    taskId: "T-1222",
    text: "Проверь контекст",
    expectedTaskRevision: 4
  });
  assert.deepEqual(parseWebviewMessage({ type: "cancelAgentRun", taskId: "T-1222" }), {
    type: "cancelAgentRun",
    taskId: "T-1222"
  });
  assert.deepEqual(parseWebviewMessage({
    type: "respondAgentPermission",
    taskId: "T-1222",
    permissionId: "permission-1",
    response: "once"
  }), {
    type: "respondAgentPermission",
    taskId: "T-1222",
    permissionId: "permission-1",
    response: "once"
  });
  assert.equal(parseWebviewMessage({
    type: "sendAgentMessage",
    taskId: "T-1222",
    text: "Проверь",
    expectedTaskRevision: 4,
    endpoint: "http://127.0.0.1:4096"
  }), undefined);
  assert.equal(parseWebviewMessage({
    type: "respondAgentPermission",
    taskId: "T-1222",
    permissionId: "permission-1",
    response: "always",
    capability: "secret"
  }), undefined);
});

test("message parser allowlists create, edit, move, attach and archive intents", () => {
  assert.deepEqual(parseWebviewMessage({ type: "create", title: "New task", description: "Details", priority: "P1" }), {
    type: "create", title: "New task", description: "Details", priority: "P1"
  });
  assert.deepEqual(parseWebviewMessage({ type: "create", title: "New task", description: "Details", priority: "P1", deadline: "2026-08-26" }), {
    type: "create", title: "New task", description: "Details", priority: "P1", deadline: "2026-08-26"
  });
  assert.deepEqual(parseWebviewMessage({ type: "create", title: "  New task  ", description: "  exact Markdown  ", priority: "P2" }), {
    type: "create", title: "New task", description: "  exact Markdown  ", priority: "P2"
  });
  assert.deepEqual(parseWebviewMessage({ type: "edit", taskId: "T-2", title: "Changed", description: "Text", priority: "P2", expectedTaskRevision: 3 }), {
    type: "edit", taskId: "T-2", title: "Changed", description: "Text", priority: "P2", expectedTaskRevision: 3
  });
  assert.equal(parseWebviewMessage({ type: "edit", taskId: "T-2", title: "Changed", description: "Text", priority: "P2", assignee: null, expectedTaskRevision: 3 }), undefined);
  assert.deepEqual(parseWebviewMessage({ type: "edit", taskId: "T-2", title: "Changed", description: "Text", priority: "P2", deadline: null, expectedTaskRevision: 3 }), {
    type: "edit", taskId: "T-2", title: "Changed", description: "Text", priority: "P2", deadline: null, expectedTaskRevision: 3
  });
  assert.equal(parseWebviewMessage({ type: "create", title: "New task", description: "Details", priority: "P1", deadline: "26.08.2026" }), undefined);
  assert.equal(parseWebviewMessage({ type: "create", title: "New task", description: "Details", priority: "P4" }), undefined);
  assert.equal(parseWebviewMessage({ type: "create", title: "New task", description: "Details", priority: "P2", deadline: "2026-02-30" }), undefined);
  assert.deepEqual(parseWebviewMessage({ type: "move", taskId: "T-2", groupId: "epoch-1", rank: "00004000", expectedBoardRevision: 8 }), {
    type: "move", taskId: "T-2", groupId: "epoch-1", rank: "00004000", expectedBoardRevision: 8
  });
  assert.equal(parseWebviewMessage({ type: "move", taskId: "T-2", status: "Review", groupId: null, rank: "00004000", expectedBoardRevision: 8 }), undefined);
  assert.deepEqual(parseWebviewMessage({ type: "attach", taskId: "T-2", expectedTaskRevision: 3 }), {
    type: "attach", taskId: "T-2", expectedTaskRevision: 3
  });
  assert.deepEqual(parseWebviewMessage({ type: "archive", taskId: "T-2", expectedTaskRevision: 3, expectedBoardRevision: 8 }), {
    type: "archive", taskId: "T-2", expectedTaskRevision: 3, expectedBoardRevision: 8
  });
  assert.deepEqual(parseWebviewMessage({ type: "unarchive", taskId: "T-2", expectedTaskRevision: 4, expectedBoardRevision: 9 }), {
    type: "unarchive", taskId: "T-2", expectedTaskRevision: 4, expectedBoardRevision: 9
  });
  assert.equal(parseWebviewMessage({ type: "archive", taskId: "T-2", expectedTaskRevision: 3 }), undefined);
  assert.equal(parseWebviewMessage({ type: "unarchive", taskId: "T-2", expectedTaskRevision: 4 }), undefined);
  assert.deepEqual(parseWebviewMessage({ type: "removeAttachment", taskId: "T-2", attachmentId: "A-0001", expectedTaskRevision: 4 }), {
    type: "removeAttachment", taskId: "T-2", attachmentId: "A-0001", expectedTaskRevision: 4
  });
  assert.deepEqual(parseWebviewMessage({ type: "setAttachmentPreview", taskId: "T-2", attachmentId: "A-0001", expectedTaskRevision: 4 }), {
    type: "setAttachmentPreview", taskId: "T-2", attachmentId: "A-0001", expectedTaskRevision: 4
  });
  assert.deepEqual(parseWebviewMessage({ type: "clearAttachmentPreview", taskId: "T-2", expectedTaskRevision: 4 }), {
    type: "clearAttachmentPreview", taskId: "T-2", expectedTaskRevision: 4
  });
  assert.equal(parseWebviewMessage({ type: "setAttachmentPreview", taskId: "T-2", attachmentId: "../../secret", expectedTaskRevision: 4 }), undefined);
  assert.equal(parseWebviewMessage({ type: "move", taskId: "T-2", groupId: null, rank: "", expectedBoardRevision: 8 }), undefined);
  assert.equal(parseWebviewMessage({ type: "attach", taskId: "T-2", expectedTaskRevision: 3, file: "C:/secret" }), undefined);
});

test("message parser allowlists global tag settings and revision-aware task tag selection", () => {
  assert.deepEqual(parseWebviewMessage({ type: "manageTags" }), { type: "manageTags" });
  assert.deepEqual(parseWebviewMessage({
    type: "addTaskTag",
    taskId: "T-2",
    expectedTaskRevision: 4,
    expectedBoardRevision: 9
  }), {
    type: "addTaskTag",
    taskId: "T-2",
    expectedTaskRevision: 4,
    expectedBoardRevision: 9
  });
  assert.equal(parseWebviewMessage({ type: "manageTags", command: "delete" }), undefined);
  assert.equal(parseWebviewMessage({ type: "addTaskTag", taskId: "T-2", expectedTaskRevision: 4 }), undefined);
});

test("message parser allowlists submit and human decisions without accepting capability data", () => {
  assert.deepEqual(parseWebviewMessage({ type: "submit", taskId: "T-2", reason: "Ready", expectedTaskRevision: 3 }), {
    type: "submit", taskId: "T-2", reason: "Ready", expectedTaskRevision: 3
  });
  assert.deepEqual(parseWebviewMessage({ type: "accept", taskId: "T-2", reason: "Approved", expectedTaskRevision: 4 }), {
    type: "accept", taskId: "T-2", reason: "Approved", expectedTaskRevision: 4
  });
  assert.deepEqual(parseWebviewMessage({ type: "requestChanges", taskId: "T-2", reason: "Add evidence", expectedTaskRevision: 4 }), {
    type: "requestChanges", taskId: "T-2", reason: "Add evidence", expectedTaskRevision: 4
  });
  assert.deepEqual(parseWebviewMessage({ type: "reopen", taskId: "T-2", reason: "Resume", expectedTaskRevision: 5 }), {
    type: "reopen", taskId: "T-2", reason: "Resume", expectedTaskRevision: 5
  });
  assert.equal(parseWebviewMessage({ type: "reopen", taskId: "T-2", reason: "", expectedTaskRevision: 5 }), undefined);
  assert.equal(parseWebviewMessage({ type: "accept", taskId: "T-2", reason: "Approved", expectedTaskRevision: 4, capability: "spoof" }), undefined);
});
