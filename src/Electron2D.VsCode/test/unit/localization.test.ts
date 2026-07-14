/*
 * Electron2D
 * MIT License
 * Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
 * SPDX-License-Identifier: MIT
 */
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import path from "node:path";
import test from "node:test";
import { buildWebviewHtml } from "../../src/webviewHtml.js";

test("runtime localization resolves VS Code languages with English fallback", async () => {
  const api = await import("../../src/localization.js").catch(() => undefined) as undefined | {
    resolveLocale(language: string | undefined): "en" | "ru";
    createLocalizer(language: string | undefined): {
      locale: "en" | "ru";
      t(key: string, ...values: readonly unknown[]): string;
    };
    localizationResources: Readonly<Record<"en" | "ru", Readonly<Record<string, string>>>>;
  };
  assert.ok(api, "a shared runtime localization module must exist");

  assert.equal(api.resolveLocale("ru"), "ru");
  assert.equal(api.resolveLocale("ru-RU"), "ru");
  assert.equal(api.resolveLocale("en"), "en");
  assert.equal(api.resolveLocale("en-US"), "en");
  assert.equal(api.resolveLocale("de"), "en");
  assert.equal(api.resolveLocale(undefined), "en");

  assert.deepEqual(
    Object.keys(api.localizationResources.ru).sort(),
    Object.keys(api.localizationResources.en).sort(),
    "English and Russian dictionaries must have identical keys");
  assert.equal(Object.values(api.localizationResources.en).every(value => value.length > 0), true);
  assert.equal(Object.values(api.localizationResources.ru).every(value => value.length > 0), true);

  const english = api.createLocalizer("en-US");
  const russian = api.createLocalizer("ru-RU");
  assert.equal(english.t("extension.panelTitle"), "Task Board");
  assert.equal(russian.t("extension.panelTitle"), "Доска задач");
  assert.equal(english.t("board.createTask"), "Create task");
  assert.equal(russian.t("board.createTask"), "Создать задачу");
  assert.doesNotThrow(() => {
    assert.equal(russian.t("task.createDialogTitle"), "Создание задачи");
    assert.equal(russian.t("task.create"), "Создать");
    assert.equal(russian.t("task.creating"), "Создание…");
    assert.equal(russian.t("task.cancelCreate"), "Отмена");
    assert.equal(russian.t("task.deadlineInvalid"), "Укажите существующую дату в формате ГГГГ-ММ-ДД.");
  });
  assert.equal(english.t("board.tags"), "Tags");
  assert.equal(russian.t("board.tags"), "Теги");
  assert.equal(english.t("board.allTags"), "All tags");
  assert.equal(russian.t("board.allTags"), "Все теги");
  assert.equal(english.t("board.selectedTags", 3), "3 tags selected");
  assert.equal(russian.t("board.selectedTags", 3), "Выбрано тегов: 3");
  assert.equal(english.t("board.resetFilters"), "Reset filters");
  assert.equal(russian.t("board.resetFilters"), "Сбросить фильтры");
  assert.equal(english.t("task.reopen"), "Return to work");
  assert.equal(russian.t("task.reopen"), "Вернуть в работу");
  assert.equal(english.t("task.reopenReason"), "Reopened by the user in VS Code Task Board.");
  assert.equal(russian.t("task.reopenReason"), "Задача возвращена в работу пользователем в VS Code Task Board.");
  assert.equal(english.t("task.archive"), "Archive");
  assert.equal(russian.t("task.archive"), "В архив");
  assert.equal(english.t("task.unarchive"), "Restore from archive");
  assert.equal(russian.t("task.unarchive"), "Вернуть из архива");
  assert.equal(english.t("files.open", "README.md"), "Open README.md");
  assert.equal(russian.t("files.open", "README.md"), "Открыть README.md");
  assert.equal(english.t("activity.taskPatched", "title, description"), "Task updated: title, description");
  assert.equal(russian.t("activity.taskPatched", "заголовок, описание"), "Задача обновлена: заголовок, описание");
  assert.equal(english.t("activity.taskPatchedEmpty"), "Task updated");
  assert.equal(russian.t("activity.taskPatchedEmpty"), "Задача обновлена");
  assert.equal(
    english.t("activity.statusChanged", "Review", "In progress", "Requested by the user"),
    "Status changed: Review → In progress. Reason: Requested by the user");
  assert.equal(
    russian.t("activity.statusChanged", "На проверке", "В работе", "Пользователь вернул задачу"),
    "Статус изменён: На проверке → В работе. Причина: Пользователь вернул задачу");
});

test("webview HTML uses the selected locale before the first board snapshot", () => {
  const common = {
    cspSource: "vscode-webview://test",
    nonce: "nonce",
    scriptUri: "vscode-webview://test/webview.js",
    styleUri: "vscode-webview://test/webview.css"
  };
  const english = buildWebviewHtml({ ...common, locale: "en" });
  const russian = buildWebviewHtml({ ...common, locale: "ru" });

  assert.match(english, /<html lang="en">/);
  assert.match(english, /placeholder="Search tasks…"/);
  assert.match(english, /class="board-name">Task board<\/span>/);
  assert.match(english, />Create task</);
  assert.match(russian, /<html lang="ru">/);
  assert.match(russian, /placeholder="Поиск задач…"/);
  assert.match(russian, /class="board-name">Доска задач<\/span>/);
  assert.match(russian, />Создать задачу</);
});

test("extension manifest uses matching English and Russian VS Code NLS resources", async () => {
  const root = process.cwd();
  const packageJson = JSON.parse(await readFile(path.join(root, "package.json"), "utf8")) as Record<string, unknown>;
  const englishText = await readFile(path.join(root, "package.nls.json"), "utf8").catch(() => undefined);
  const russianText = await readFile(path.join(root, "package.nls.ru.json"), "utf8").catch(() => undefined);
  assert.ok(englishText, "package.nls.json must provide the English default");
  assert.ok(russianText, "package.nls.ru.json must provide Russian strings");
  const english = JSON.parse(englishText) as Record<string, string>;
  const russian = JSON.parse(russianText) as Record<string, string>;
  assert.deepEqual(Object.keys(russian).sort(), Object.keys(english).sort());
  assert.equal(packageJson.displayName, "%extension.displayName%");
  assert.equal(packageJson.description, "%extension.description%");
  assert.equal(english["command.openTaskBoard"], "Open Task Board");
  assert.equal(russian["command.openTaskBoard"], "Открыть доску задач");
});

test("user-facing runtime surfaces do not embed one fixed UI language", async () => {
  const sourceRoot = path.join(process.cwd(), "src");
  for (const file of ["extension.ts", "webview.ts", "webviewHtml.ts", "commentMarkdown.ts"]) {
    const source = await readFile(path.join(sourceRoot, file), "utf8");
    assert.doesNotMatch(source, /[А-Яа-яЁё]/, `${file} must obtain Russian UI text from localization resources`);
  }
});
