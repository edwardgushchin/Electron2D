/* Electron2D — MIT License — SPDX-License-Identifier: MIT */
import assert from "node:assert/strict";
import test from "node:test";
import { createLocalizer } from "../../src/localization.js";

interface MarkdownNode {
  readonly kind: "element" | "text";
  readonly tag?: string;
  readonly text?: string;
  readonly attributes?: Readonly<Record<string, string>>;
  readonly children?: readonly MarkdownNode[];
}

interface MarkdownModule {
  readonly parseCommentMarkdown: (source: string, localizer?: ReturnType<typeof createLocalizer>) => readonly MarkdownNode[];
  readonly applyCommentMarkdownEdit: (
    source: string,
    selectionStart: number,
    selectionEnd: number,
    action: "strong" | "emphasis" | "list" | "code" | "link" | "mention",
    localizer?: ReturnType<typeof createLocalizer>) => {
      readonly value: string;
      readonly selectionStart: number;
      readonly selectionEnd: number;
    };
}

async function loadMarkdownModule(): Promise<MarkdownModule> {
  try {
    return await import("../../src/commentMarkdown.js") as MarkdownModule;
  } catch (error) {
    assert.fail(`comment Markdown module is unavailable: ${String(error)}`);
  }
}

function descendants(nodes: readonly MarkdownNode[]): MarkdownNode[] {
  return nodes.flatMap(node => [node, ...descendants(node.children ?? [])]);
}

test("comment Markdown parses block and inline syntax into a safe presentation tree", async () => {
  const { parseCommentMarkdown } = await loadMarkdownModule();
  const nodes = parseCommentMarkdown([
    "# Заголовок",
    "",
    "Абзац с **жирным**, _курсивом_, ~~зачёркнутым~~ и `code`.",
    "",
    "- первый",
    "- второй",
    "",
    "> цитата",
    "",
    "---",
    "",
    "```ts",
    "const value = 1;",
    "```",
    "",
    "[безопасная ссылка](https://example.com) и [опасная](javascript:alert(1)).",
    "",
    "<img src=x onerror=alert(1)>"
  ].join("\n"));

  const all = descendants(nodes);
  const tags = new Set(all.flatMap(node => node.tag ? [node.tag] : []));
  for (const tag of ["h1", "p", "strong", "em", "s", "code", "ul", "li", "blockquote", "hr", "pre", "a"]) {
    assert.ok(tags.has(tag), `expected ${tag}`);
  }

  const links = all.filter(node => node.tag === "a");
  assert.deepEqual(links.map(link => link.attributes?.href), ["https://example.com"]);
  assert.equal(links[0]?.attributes?.target, "_blank");
  assert.equal(links[0]?.attributes?.rel, "noopener noreferrer");
  assert.equal(all.some(node => node.tag === "img" || node.tag === "script"), false);
  assert.ok(all.some(node => node.kind === "text" && node.text?.includes("<img src=x onerror=alert(1)>")));
  assert.ok(all.some(node => node.kind === "text" && node.text?.includes("javascript:alert(1)")));
});

test("comment Markdown editor wraps selections and creates useful empty templates", async () => {
  const { applyCommentMarkdownEdit } = await loadMarkdownModule();
  assert.deepEqual(
    applyCommentMarkdownEdit("Привет, мир", 8, 11, "strong"),
    { value: "Привет, **мир**", selectionStart: 10, selectionEnd: 13 });
  assert.equal(applyCommentMarkdownEdit("", 0, 0, "link").value, "[текст](https://)");
  assert.equal(applyCommentMarkdownEdit("", 0, 0, "link", createLocalizer("en")).value, "[text](https://)");
  assert.equal(applyCommentMarkdownEdit("один\nдва", 0, 8, "list").value, "- один\n- два");
});
