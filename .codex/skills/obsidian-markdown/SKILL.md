---
name: obsidian-markdown
description: |
  Work with StellarDown Markdown knowledge artifacts: the development diary, domain documents,
  and migrated legacy Obsidian Markdown. Use when creating, moving, or editing project-facing
  Markdown records; task state itself is handled only through e2d tasks.
---

# Obsidian Markdown

## Purpose

Use this skill for repository Markdown that acts as project memory, task state, domain documentation, or diary history.

The current live layout is:

- `.taskboard/board.e2tasks` and `.taskboard/{tasks,completed}/*.e2task` - machine-readable task state; access it only through `e2d tasks`, not through Markdown editing.
- `data/dev-diary/YYYY/MM Month/DD-MM-YYYY.md` - development diary.
- `docs/<domain>/` - domain documents that combine expected contract, current implemented state, limits, and verification notes in one place.
- `.temp/` - temporary drafts, probes, and generated scratch artifacts.

## Language

- Write human-facing project Markdown in clear Russian by default.
- Keep exact code symbols, paths, command names, API fields, package names, and external product names in their original form.
- Preserve the terminology of an existing document when editing it.

## Legacy Markdown

- Migrated legacy files under `docs/legacy/`, `docs/legacy-memory/`, and `data/dev-diary/` keep their historical internal format unless the user explicitly asks for normalization.
- Legacy memory files are archival context only. New durable prose context belongs in `data/dev-diary/` and the relevant domain document; task evidence belongs in task activity through `e2d tasks comment add`.
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

## Taskboard Boundary

- Read task state through `e2d tasks get` or `e2d tasks board` and mutate it only through the relevant `e2d tasks` subcommand.
- Never create or edit `.e2task` or `.e2tasks` JSON through this Markdown skill.
- Record implementation evidence or blocker reports with `e2d tasks comment add`; do not create a parallel Markdown task ledger.
- Human acceptance is required before `Done`, and only accepted or cancelled tasks may be archived through `e2d tasks archive`.

## Domain Documents

- Domain documents describe one concrete thing: expected behavior, invariants, current implemented behavior, limits, and verification surface.
- For new files, choose a concrete domain folder under `docs/<domain>/`.
- Before code changes, update the document's expected behavior. After green tests, update the same document if the implementation changed actual behavior, limits, commands, or evidence.
- Avoid creating placeholder document stubs. A new Markdown artifact should carry useful content immediately.

## Temporary Artifacts

- Put drafts, probes, generated scratch outputs, and one-off analysis files under `.temp/`.
- Do not place temporary artifacts in the repository root, `.codex`, `docs/`, `data/dev-diary/`, or `.taskboard/`.
- Promote an artifact out of `.temp/` only when it becomes part of the intended durable repository state.
