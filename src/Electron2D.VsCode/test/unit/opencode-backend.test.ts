/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  finalTextFromOpenCodeResponse,
  openCodePromptBody,
  openCodeServerConfig,
  openCodeTransportDiagnostic,
  runWithOpenCodeTransportRecovery
} from "../../src/opencodeBackend.js";

test("OpenCode security overlay uses ordinary provider auth with GPT-5.6 Sol", () => {
  const config = openCodeServerConfig();

  assert.equal(config.share, "disabled");
  assert.equal("enabled_providers" in config, false);
  assert.equal("provider" in config, false);
  assert.equal("model" in config, false);
  assert.equal(config.permission?.edit, "ask");
  assert.equal(config.permission?.bash, "ask");
  assert.equal(config.permission?.webfetch, "ask");
  assert.equal(config.permission?.external_directory, "deny");
  assert.equal(config.tools?.task, false);
  assert.equal(config.tools?.websearch, false);

  const context = openCodePromptBody("canonical context", true);
  const prompt = openCodePromptBody("Ты тут?", false);
  assert.deepEqual(context, {
    model: { providerID: "openai", modelID: "gpt-5.6-sol" },
    noReply: true,
    parts: [{ type: "text", text: "canonical context" }]
  });
  assert.deepEqual(prompt, {
    model: { providerID: "openai", modelID: "gpt-5.6-sol" },
    parts: [{ type: "text", text: "Ты тут?" }]
  });
});

test("OpenCode transport recovery replaces one refused owned runtime and never retries again", async () => {
  let attempts = 0;
  let resets = 0;
  const result = await runWithOpenCodeTransportRecovery(
    async () => {
      attempts++;
      if (attempts === 1) {
        const cause = Object.assign(new Error("connect refused; token=secret"), { code: "ECONNREFUSED" });
        throw new TypeError("fetch failed", { cause });
      }
      return "Ready";
    },
    () => { resets++; });

  assert.equal(result, "Ready");
  assert.equal(attempts, 2);
  assert.equal(resets, 1);

  attempts = 0;
  resets = 0;
  await assert.rejects(
    runWithOpenCodeTransportRecovery(
      async () => {
        attempts++;
        const error = Object.assign(new Error("refused"), { code: "ECONNREFUSED" });
        throw error;
      },
      () => { resets++; }),
    /OpenCode transport is unavailable/i);
  assert.equal(attempts, 2);
  assert.equal(resets, 1);
});

test("ambiguous transport failures are not replayed and raw fetch errors become actionable", async () => {
  let attempts = 0;
  let resets = 0;
  await assert.rejects(
    runWithOpenCodeTransportRecovery(
      async () => {
        attempts++;
        throw new TypeError("fetch failed; api_key=secret");
      },
      () => { resets++; }),
    error => error instanceof Error &&
      /OpenCode transport request failed/i.test(error.message) &&
      !/fetch failed/i.test(error.message) &&
      !error.message.includes("secret"));
  assert.equal(attempts, 1);
  assert.equal(resets, 0);

  const diagnostic = openCodeTransportDiagnostic(new TypeError("fetch failed; password=secret"));
  assert.match(diagnostic, /OpenCode transport request failed/i);
  assert.doesNotMatch(diagnostic, /fetch failed|secret/i);
});

test("OpenCode assistant errors keep the safe provider cause instead of looking like an empty final", () => {
  assert.throws(
    () => finalTextFromOpenCodeResponse({
      info: {
        error: {
          name: "UnknownError",
          data: {
            message: "n_keep: 9296 >= n_ctx: 8192; api_key=secret"
          }
        }
      },
      parts: [{ type: "step-start" }]
    } as never),
    error => error instanceof Error &&
      /n_keep: 9296 >= n_ctx: 8192/.test(error.message) &&
      !error.message.includes("secret") &&
      !error.message.includes("empty final"));
});
