/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import MarkdownIt, { type MarkdownItToken } from "markdown-it";
import { createLocalizer, type Localizer } from "./localization.js";

export type CommentMarkdownTag =
  | "a" | "blockquote" | "br" | "code" | "em" | "h1" | "h2" | "h3"
  | "h4" | "h5" | "h6" | "hr" | "li" | "ol" | "p" | "pre" | "s"
  | "span" | "strong" | "ul";

export interface CommentMarkdownTextNode {
  readonly kind: "text";
  readonly text: string;
}

export interface CommentMarkdownElementNode {
  readonly kind: "element";
  readonly tag: CommentMarkdownTag;
  readonly attributes?: Readonly<Record<string, string>>;
  readonly children: readonly CommentMarkdownNode[];
}

export type CommentMarkdownNode = CommentMarkdownTextNode | CommentMarkdownElementNode;
export type CommentMarkdownEditAction = "strong" | "emphasis" | "list" | "code" | "link" | "mention";

export interface CommentMarkdownEdit {
  readonly value: string;
  readonly selectionStart: number;
  readonly selectionEnd: number;
}

interface MutableElementNode {
  readonly kind: "element";
  readonly tag: CommentMarkdownTag;
  readonly attributes?: Record<string, string>;
  readonly children: CommentMarkdownNode[];
}

const markdown = new MarkdownIt({ html: false, linkify: true, breaks: true, typographer: false });
const blockTags = new Map<string, CommentMarkdownTag>([
  ["paragraph_open", "p"],
  ["blockquote_open", "blockquote"],
  ["bullet_list_open", "ul"],
  ["ordered_list_open", "ol"],
  ["list_item_open", "li"]
]);
const inlineTags = new Map<string, CommentMarkdownTag>([
  ["strong_open", "strong"],
  ["em_open", "em"],
  ["s_open", "s"]
]);

export function parseCommentMarkdown(
  source: string,
  l10n: Localizer = createLocalizer("ru")): readonly CommentMarkdownNode[] {
  const roots: CommentMarkdownNode[] = [];
  const stack: MutableElementNode[] = [];
  const append = (node: CommentMarkdownNode): void => {
    const parent = stack.at(-1);
    if (parent) {
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  };
  const open = (tag: CommentMarkdownTag, attributes?: Record<string, string>): void => {
    const node: MutableElementNode = attributes
      ? { kind: "element", tag, attributes, children: [] }
      : { kind: "element", tag, children: [] };
    append(node);
    stack.push(node);
  };
  const close = (): void => {
    stack.pop();
  };

  for (const token of markdown.parse(source, {})) {
    if (token.type === "inline") {
      appendInlineTokens(token.children ?? [], append, open, close, l10n);
      continue;
    }
    if (token.type === "heading_open" && isHeadingTag(token.tag)) {
      open(token.tag);
      continue;
    }
    if (token.type === "heading_close") {
      close();
      continue;
    }
    const blockTag = blockTags.get(token.type);
    if (blockTag) {
      open(blockTag);
      continue;
    }
    if (token.type.endsWith("_close")) {
      close();
      continue;
    }
    if (token.type === "fence" || token.type === "code_block") {
      const code: CommentMarkdownElementNode = {
        kind: "element",
        tag: "code",
        children: [{ kind: "text", text: token.content }]
      };
      append({ kind: "element", tag: "pre", children: [code] });
      continue;
    }
    if (token.type === "hr") {
      append({ kind: "element", tag: "hr", children: [] });
      continue;
    }
    if (token.content.length > 0) {
      append({ kind: "text", text: token.content });
    }
  }
  return roots;
}

export function applyCommentMarkdownEdit(
  source: string,
  selectionStart: number,
  selectionEnd: number,
  action: CommentMarkdownEditAction,
  l10n: Localizer = createLocalizer("ru")): CommentMarkdownEdit {
  const start = Math.max(0, Math.min(selectionStart, source.length));
  const end = Math.max(start, Math.min(selectionEnd, source.length));
  const selected = source.slice(start, end);
  if (action === "list") {
    const replacement = (selected || l10n.t("markdown.listItem"))
      .split("\n")
      .map(line => `- ${line}`)
      .join("\n");
    return replaceSelection(source, start, end, replacement, 0, replacement.length);
  }
  if (action === "link") {
    const label = selected || l10n.t("markdown.linkText");
    const replacement = `[${label}](https://)`;
    const urlStart = label.length + 3;
    return replaceSelection(source, start, end, replacement, urlStart, urlStart + 8);
  }
  if (action === "mention") {
    const content = selected || l10n.t("markdown.mentionName");
    return replaceSelection(source, start, end, `@${content}`, 1, content.length + 1);
  }

  const [prefix, suffix, placeholder] = action === "strong"
    ? ["**", "**", l10n.t("markdown.text")]
    : action === "emphasis"
      ? ["_", "_", l10n.t("markdown.text")]
      : ["`", "`", l10n.t("markdown.code")];
  const content = selected || placeholder;
  return replaceSelection(
    source,
    start,
    end,
    `${prefix}${content}${suffix}`,
    prefix.length,
    prefix.length + content.length);
}

function appendInlineTokens(
  tokens: readonly MarkdownItToken[],
  append: (node: CommentMarkdownNode) => void,
  open: (tag: CommentMarkdownTag, attributes?: Record<string, string>) => void,
  close: () => void,
  l10n: Localizer): void {
  for (const token of tokens) {
    if (token.type === "text" || token.type === "html_inline") {
      append({ kind: "text", text: token.content });
      continue;
    }
    if (token.type === "softbreak" || token.type === "hardbreak") {
      append({ kind: "element", tag: "br", children: [] });
      continue;
    }
    if (token.type === "code_inline") {
      append({
        kind: "element",
        tag: "code",
        children: [{ kind: "text", text: token.content }]
      });
      continue;
    }
    const inlineTag = inlineTags.get(token.type);
    if (inlineTag) {
      open(inlineTag);
      continue;
    }
    if (token.type === "link_open") {
      const href = safeMarkdownHref(token.attrGet("href"));
      if (href) {
        open("a", { href, target: "_blank", rel: "noopener noreferrer" });
      } else {
        open("span");
      }
      continue;
    }
    if (token.type === "image") {
      append({ kind: "text", text: token.content || l10n.t("markdown.image") });
      continue;
    }
    if (token.type.endsWith("_close")) {
      close();
    }
  }
}

function safeMarkdownHref(value: string | null): string | null {
  if (!value) {
    return null;
  }
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:" || url.protocol === "mailto:"
      ? value
      : null;
  } catch {
    return null;
  }
}

function isHeadingTag(tag: string): tag is "h1" | "h2" | "h3" | "h4" | "h5" | "h6" {
  return /^h[1-6]$/.test(tag);
}

function replaceSelection(
  source: string,
  start: number,
  end: number,
  replacement: string,
  relativeSelectionStart: number,
  relativeSelectionEnd: number): CommentMarkdownEdit {
  return {
    value: `${source.slice(0, start)}${replacement}${source.slice(end)}`,
    selectionStart: start + relativeSelectionStart,
    selectionEnd: start + relativeSelectionEnd
  };
}
