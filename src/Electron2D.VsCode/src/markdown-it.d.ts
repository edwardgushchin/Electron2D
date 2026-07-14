/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
declare module "markdown-it" {
  export interface MarkdownItToken {
    readonly type: string;
    readonly tag: string;
    readonly content: string;
    readonly info: string;
    readonly children: readonly MarkdownItToken[] | null;
    attrGet(name: string): string | null;
  }

  export interface MarkdownItOptions {
    readonly html?: boolean;
    readonly linkify?: boolean;
    readonly breaks?: boolean;
    readonly typographer?: boolean;
  }

  export default class MarkdownIt {
    public constructor(options?: MarkdownItOptions);
    public parse(source: string, environment: object): readonly MarkdownItToken[];
  }
}
