/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import { buildContentSecurityPolicy } from "./security.js";
import { createLocalizer } from "./localization.js";

export interface WebviewHtmlOptions {
  readonly cspSource: string;
  readonly nonce: string;
  readonly scriptUri: string;
  readonly styleUri: string;
  readonly locale?: string;
}

export function buildWebviewHtml(options: WebviewHtmlOptions): string {
  const csp = buildContentSecurityPolicy(options.cspSource, options.nonce);
  const l10n = createLocalizer(options.locale);
  const text = (key: Parameters<typeof l10n.t>[0]): string => escapeAttribute(l10n.t(key));
  return `<!doctype html>
<html lang="${l10n.locale}">
<head>
  <meta charset="UTF-8">
  <meta http-equiv="Content-Security-Policy" content="${escapeAttribute(csp)}">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <link rel="stylesheet" href="${escapeAttribute(options.styleUri)}">
  <title>${text("board.title")}</title>
</head>
<body>
  <header class="toolbar">
    <div class="project-breadcrumb">
      <svg class="brand-mark" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <path class="brand-mark-left" d="M9.5 4 3 12l6.5 8 2.5-3-4-5 4-5z"></path>
        <path class="brand-mark-right" d="m14.5 4-2.5 3 4 5-4 5 2.5 3 6.5-8z"></path>
      </svg>
      <h1 id="project"></h1>
      <span class="breadcrumb-separator">/</span>
      <span class="board-name">${text("board.ariaLabel")}</span>
    </div>
    <label class="search-box" aria-label="${text("board.search")}">
      <svg class="search-icon" viewBox="0 0 16 16" aria-hidden="true" focusable="false"><circle cx="7" cy="7" r="4.25"></circle><path d="m10.25 10.25 3 3"></path></svg>
      <input id="filter" type="search" autocomplete="off" placeholder="${text("board.searchPlaceholder")}" aria-keyshortcuts="Control+F">
      <kbd class="search-shortcut">Ctrl+F</kbd>
    </label>
    <div class="toolbar-actions">
      <div class="toolbar-popover">
        <button id="filter-toggle" class="secondary-control filter-button" type="button" aria-label="${text("board.filters")}" aria-controls="filter-panel" aria-expanded="false" title="${text("board.filters")}">
          <svg class="filter-icon" viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M2.5 3h11L9.25 7.75v4l-2.5 1.25V7.75z"></path></svg>
          <span class="filter-label">${text("board.filters")}</span>
          <span id="filter-count" class="filter-count" hidden>0</span>
        </button>
        <div id="filter-panel" class="popover-panel filter-panel" hidden>
          <label for="priority-filter">${text("board.priority")}</label>
          <select id="priority-filter">
            <option value="">${text("board.allPriorities")}</option>
            <option value="P0">P0</option>
            <option value="P1">P1</option>
            <option value="P2">P2</option>
            <option value="P3">P3</option>
          </select>
          <label for="tag-filter-trigger">${text("board.tags")}</label>
          <div id="tag-filter" class="tag-filter">
            <button id="tag-filter-trigger" class="tag-filter-trigger" type="button" role="combobox" aria-haspopup="listbox" aria-expanded="false" aria-controls="tag-filter-popup">
              <span id="tag-filter-value">${text("board.allTags")}</span>
              <svg class="tag-filter-chevron" viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M4 6l4 4 4-4"></path></svg>
            </button>
            <div id="tag-filter-popup" class="tag-filter-popup" role="listbox" aria-multiselectable="true" hidden></div>
          </div>
          <button id="reset-filters" class="secondary-control filter-reset" type="button" disabled>${text("board.resetFilters")}</button>
        </div>
      </div>
      <label class="archive-toggle secondary-control"><input id="show-archived" type="checkbox"><span>${text("board.archive")}</span></label>
      <button id="tag-settings" class="icon-button" type="button" aria-label="${text("board.tagSettings")}" title="${text("board.tagSettings")}">
        <svg class="settings-icon" viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M6.7 2.2h2.6l.4 1.5 1.3.7 1.5-.5 1.3 2.2-1.1 1.1v1.6l1.1 1.1-1.3 2.2-1.5-.5-1.3.7-.4 1.5H6.7l-.4-1.5-1.3-.7-1.5.5-1.3-2.2 1.1-1.1V7.2L2.2 6.1l1.3-2.2 1.5.5 1.3-.7z"></path><circle cx="8" cy="8" r="2"></circle></svg>
      </button>
      <button id="refresh" class="icon-button" type="button" aria-label="${text("board.refresh")}" title="${text("board.refresh")}"><svg class="refresh-icon" viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="M12.5 5.5A5 5 0 1 0 13 10"></path><path d="M12.5 2.5v3h-3"></path></svg></button>
      <button id="create" class="primary-button" type="button">${text("board.createTask")}</button>
    </div>
  </header>
  <main>
    <section id="board" class="board" aria-label="${text("board.ariaLabel")}"></section>
    <aside id="details" class="details" role="dialog" aria-modal="true" aria-labelledby="task-details-title" aria-hidden="true" inert hidden></aside>
  </main>
  <div id="create-dialog" class="create-dialog" aria-hidden="true" inert hidden>
    <section class="create-task-window" role="dialog" aria-modal="true" aria-labelledby="create-task-heading" tabindex="-1" aria-busy="false">
      <header class="create-task-header">
        <h2 id="create-task-heading">${text("task.createDialogTitle")}</h2>
        <button id="create-task-close" class="create-task-close" type="button" aria-label="${text("task.closeCreate")}" title="${text("task.closeCreate")}">
          <svg viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="m4 4 8 8M12 4l-8 8"></path></svg>
        </button>
      </header>
      <form id="create-task-form" class="create-task-form" novalidate>
        <label for="create-task-title">${text("task.formTitle")}</label>
        <input id="create-task-title" type="text" autocomplete="off" required maxlength="512" aria-describedby="create-task-title-hint">
        <span id="create-task-title-hint" class="create-task-hint">${text("task.titleRequired")}</span>

        <label for="create-task-description">${text("task.formDescription")}</label>
        <textarea id="create-task-description" rows="8" maxlength="262144"></textarea>

        <div class="create-task-fields">
          <label>
            <span>${text("task.formPriority")}</span>
            <select id="create-task-priority">
              <option value="P0">P0</option>
              <option value="P1">P1</option>
              <option value="P2" selected>P2</option>
              <option value="P3">P3</option>
            </select>
          </label>
          <label>
            <span>${text("task.formDeadlineOptional")}</span>
            <input id="create-task-deadline" type="date" aria-describedby="create-task-deadline-error">
            <span id="create-task-deadline-error" class="create-task-field-error" role="alert" hidden></span>
          </label>
        </div>

        <p id="create-task-error" class="create-task-error" role="alert" hidden></p>
        <footer class="create-task-actions">
          <button id="create-task-cancel" class="create-task-secondary" type="button">${text("task.cancelCreate")}</button>
          <button id="create-task-submit" class="create-task-primary" type="submit" disabled>${text("task.create")}</button>
        </footer>
      </form>
    </section>
  </div>
  <script nonce="${escapeAttribute(options.nonce)}" src="${escapeAttribute(options.scriptUri)}"></script>
</body>
</html>`;
}

function escapeAttribute(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll('"', "&quot;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}
