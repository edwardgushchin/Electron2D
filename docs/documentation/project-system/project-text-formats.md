# Stable project text formats

`T-0117` добавляет текущий formatter/schema/migration layer в `src/Electron2D.ProjectSystem`. Он работает поверх canonical document model и остаётся внутренним механизмом для тестов, будущего Editor, Tooling, CLI и MCP.

Этот слой не является live `ProjectWorkspace`: он не отслеживает открытые документы, не сохраняет файлы сам и не выполняет merge. Он только приводит project source JSON к стабильному виду, мигрирует legacy version `0` к version `1`, проверяет опасные поля и публикует JSON Schema files.

## Formatter

`ProjectTextFormatter.FormatText(path, text)` сейчас покрывает:

- `Electron2D.SceneFile`;
- `Electron2D.ResourceFile`;
- `Electron2D.ProjectSettings`;
- generic JSON fallback;
- plain text as LF-normalized text.

Formatter пишет два пробела indentation и LF endings. Для known formats он сохраняет фиксированный top-level порядок, сортирует property maps ordinal-сравнением и не добавляет Editor UI state вроде selection, expanded tree nodes, viewport scroll или dock layout.

Generated files и binary assets не считаются canonical source text. Попытка форматировать их завершается ошибкой.

## Migration

`ProjectTextMigrationPipeline.MigrateText(path, text)` добавляет `version = 1` для known JSON formats, если версия отсутствует или равна `0`. Результат содержит:

- migrated text;
- `Changed`;
- список `AppliedMigrationIds`.

Pipeline не пишет на диск. Будущая save-команда должна отдельно выполнять temporary file write и atomic replace.

## Validator

`ProjectTextValidator.ValidateText(path, text)` возвращает stable errors:

- `E2D-TEXT-GENERATED-SOURCE`;
- `E2D-TEXT-MALFORMED-JSON`;
- `E2D-TEXT-UNKNOWN-FORMAT`;
- `E2D-TEXT-VERSION`;
- `E2D-TEXT-EDITOR-STATE`;
- `E2D-TEXT-SECRET-FIELD`;
- `E2D-TEXT-ABSOLUTE-PATH`.

Secret-like fields are rejected unless their value is an explicit non-secret reference such as `env:NAME`. Absolute paths are rejected while `res://`, `uid://` and `env:` values remain allowed.

## JSON Schema

Published schemas live in `schemas/project-system/`:

- `scene-file.schema.json`;
- `resource-file.schema.json`;
- `project-settings.schema.json`.
- `electron2d-lock.schema.json`.

`ProjectTextSchemaRegistry.GetSchemaText(kind)` reads scene/resource/project settings schemas from the repository root. The reproducibility lock schema is published in the same folder and verified by `ReproducibilityLockDoctorTests`; it uses JSON Schema Draft 2020-12 and requires the fixed top-level fields from `electron2d.lock.json`.

## Current limits

Typed schemas for input map, translations, animation data, sprite frames, themes and export presets are not implemented yet. These files currently use generic JSON formatting and validation until their domain tasks add typed models.

The validator does not execute a full JSON Schema engine. It performs the safety checks required by `T-0117`; schema files are published and smoke-checked by integration tests.

## Проверка

Focused check:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~ProjectTextFormatTests
```

Standard source checks:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
