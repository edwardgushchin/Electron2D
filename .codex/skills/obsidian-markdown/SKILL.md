---
name: obsidian-markdown
description: |
  Work with StellarDown Markdown knowledge artifacts: TASKS.md, dev-diary, completed task archives,
  docs domain documents, and migrated legacy Obsidian Markdown. Use when creating,
  moving, or editing project-facing Markdown records.
---

# Obsidian Markdown

## Purpose

Use this skill for repository Markdown that acts as project memory, task state, domain documentation, or diary history.

The current live layout is:

- `TASKS.md` - active task tracker.
- `dev-diary/YYYY/MM Month/DD-MM-YYYY.md` - development diary.
- `completed-tasks/YYYY/MM Месяц.md` - accepted/closed task archives and historical backlog grouped by completion month.
- `docs/<domain>/` - domain documents that combine expected contract, current implemented state, limits, and verification notes in one place.
- `.temp/` - temporary drafts, probes, and generated scratch artifacts.

## Language

- Write human-facing project Markdown in clear Russian by default.
- Keep exact code symbols, paths, command names, API fields, package names, and external product names in their original form.
- Preserve the terminology of an existing document when editing it.

## Legacy Markdown

- Migrated legacy files under `docs/legacy/`, `docs/legacy-memory/`, `dev-diary/`, and `completed-tasks/` keep their historical internal format unless the user explicitly asks for normalization.
- Legacy memory files are archival context only. New durable context should be captured in `dev-diary/`, `completed-tasks/`, and `TASKS.md`.
- Do not revive old `MEMORY.md`, `TODO.md`, or workflow-preflight processes.

## New Diary Entries

For each session that changes the repository, update the current daily file before final response.

Use the same compact daily diary shape as the project `AGENTS.md`. New daily files start with a date heading. If an older daily file already exists without that heading or with a legacy entry shape, append the new entry to the tail and do not rewrite history just to migrate formatting.

Required shape for new daily files and new entries:

```markdown
# Дневник разработки: DD-MM-YYYY

## HH:MM +03:00 - Agent: Codex

- Задача: T-0001 / ad-hoc / запрос пользователя
- Контекст: кратко, что агент собирался сделать.
- Действия:
  - HH:MM - изучены инструкции, рабочая копия и связанная документация.
  - HH:MM - изменены конкретные файлы.
  - HH:MM - выполнены проверки и зафиксированы результаты.
- Изменения: какие файлы или документы были изменены.
- Решения: важные решения и почему.
- Проверки: какие команды проверки запускались и результат.
- Далее: что осталось сделать или что должен знать следующий агент.
```

Every new entry must include `- Действия:` with nested chronological `HH:MM - ...` bullets. Append new entries after older entries in the same file. Do not rewrite older diary history unless the user explicitly asks.

## Tasks

- Active tasks live only in `TASKS.md`.
- Use sequential IDs such as `T-0001`.
- Only the task currently being worked should be `in progress`.
- Keep imported `todo-...` identifiers as `Legacy anchor` metadata, not as the primary ID for new tasks.
- Move accepted/closed work to `completed-tasks/YYYY/MM Месяц.md` only after explicit user acceptance.
- Completed tasks are appended to the monthly archive for their completion date. Do not create standalone task files such as `completed-tasks/T-0001.md`.

## Domain Documents

- Domain documents describe one concrete thing: expected behavior, invariants, current implemented behavior, limits, and verification surface.
- For new files, choose a concrete domain folder under `docs/<domain>/`.
- Before code changes, update the document's expected behavior. After green tests, update the same document if the implementation changed actual behavior, limits, commands, or evidence.
- Avoid creating placeholder document stubs. A new Markdown artifact should carry useful content immediately.

## Temporary Artifacts

- Put drafts, probes, generated scratch outputs, and one-off analysis files under `.temp/`.
- Do not place temporary artifacts in the repository root, `.codex`, `docs/`, `dev-diary/`, or `completed-tasks/`.
- Promote an artifact out of `.temp/` only when it becomes part of the intended durable repository state.
