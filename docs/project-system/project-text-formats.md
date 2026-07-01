# Stable project text formats, migrations и JSON Schema

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0117`.
Обновлено: 2026-06-22.
Связанные документы: [Canonical document model, revision model и structural diff](canonical-document-model.md), [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md), [Сериализация сцен, ресурсов и переносимых property values](../resources/scene-resource-serialization.md), [Resource file baseline, stable UID и ссылки ресурсов](../resources/resource-file-baseline.md).

## Назначение

`T-0117` добавляет слой стабильных текстовых форматов поверх canonical document model. Этот слой отвечает за deterministic formatting, schema version, validation, migrations и опубликованные JSON Schema files. Он не реализует live `ProjectWorkspace` и не заменяет будущий external change synchronizer, но даёт ему предсказуемые входные документы.

`deterministic formatting` здесь означает, что один и тот же документ всегда записывается одинаковым текстом: фиксированный порядок top-level полей, стабильная сортировка properties, LF line endings и отсутствие случайных timestamp/Editor state.

## Scope первой реализации

Первая реализация обязана покрыть форматы, которые уже существуют или нужны для ближайшего project-system слоя:

- scene file JSON с format `Electron2D.SceneFile`;
- resource file JSON с format `Electron2D.ResourceFile`;
- project settings JSON с format `Electron2D.ProjectSettings`;
- generic JSON source files как fallback для future input map, translations, animation data, sprite frames, themes и export presets.

Специализированные модели input map, translations, animation data, sprite frames, themes и export presets могут получить собственные typed schemas позже. В `T-0117` они должны проходить generic JSON formatting/validation без секретов, абсолютных путей и editor-specific шума.

## Canonical formatter

Formatter должен:

- сохранять schema `version`;
- писать LF endings;
- использовать два пробела indentation;
- располагать top-level поля известных форматов в фиксированном порядке;
- сортировать unordered property maps ordinal-сравнением;
- не добавлять timestamp, active selection, viewport scroll, expanded tree nodes или другие Editor UI state fields;
- не писать абсолютные пути;
- сохранять resource UID при rename/move, когда меняется только fallback `path`.

Top-level порядок для scene:

1. `format`;
2. `version`;
3. `external`;
4. `internal`;
5. `nodes`.

Top-level порядок для resource:

1. `format`;
2. `version`;
3. `uid`;
4. `type`;
5. `path`;
6. `external`;
7. `internal`;
8. `properties`.

Top-level порядок для project settings:

1. `format`;
2. `version`;
3. `engineVersion`;
4. `name`;
5. `mainScene`;
6. `rendererProfile`;
7. `physicsTickRate`;
8. остальные поля по ordinal имени.

## Validator

Validator должен fail closed для source files:

- missing или unsupported `version` даёт validation error;
- unknown special `format` не принимается как known scene/resource/settings;
- абсолютные paths вроде `C:\...`, `/home/...` или `\\server\...` запрещены;
- secret-like fields (`password`, `token`, `secret`, `key`, `credential`) запрещены в source JSON, кроме явного non-secret reference form `env:NAME`;
- Editor UI state fields (`selection`, `expanded`, `scroll`, `viewport`, `inspector`, `dockLayout`) запрещены в project source files;
- generated files и import cache не должны форматироваться как canonical source files.

Validation result должен быть structured enough for tests: stable code, path внутри JSON и message.

## Migration pipeline

Migration pipeline должен принимать legacy version `0` или отсутствующий `version` у known JSON formats и возвращать version `1` с canonical formatting. Миграция должна:

- сохранять UID;
- сохранять known fields;
- не удалять unknown source fields;
- записывать список применённых migration ids;
- не писать файл напрямую: запись через temporary files и atomic replace относится к будущему save layer.

Если формат неизвестен или версия выше текущей, migration fails closed.

## JSON Schema

JSON Schema Draft 2020-12 files должны публиковаться в `data/schemas/project-system/`:

- `scene-file.schema.json`;
- `resource-file.schema.json`;
- `project-settings.schema.json`.

Schemas должны описывать минимальный проверяемый shape: `format`, `version`, обязательные массивы/objects, UID/path/type поля, nodes/resources/properties maps и запрет дополнительных top-level fields там, где формат уже фиксирован. Generic JSON fallback не получает отдельную schema в `T-0117`.

## External change synchronizer

External change synchronizer может без полного rescanning проекта обработать:

- property-only changes внутри known property maps;
- resource fallback `path` rename при сохранённом `uid`;
- scene node rename/move по stable `id`;
- добавление/удаление scene node по `id`;
- migration from version `0` to `1`, если document identity не меняется.

Он должен делать full rescan или конфликт для:

- изменения `format`;
- изменения resource top-level `uid`;
- отсутствующего или неподдержанного `version`;
- binary/generated file changes;
- source file с secret-like fields, absolute paths или Editor UI state fields.

## Критерии приёмки

- Automated tests проверяют stable formatting scene/resource/project settings JSON.
- Automated tests проверяют, что one-property edit меняет небольшой локальный diff.
- Automated tests проверяют migration version `0` или missing `version` to current `1` без потери UID.
- Automated tests проверяют validation errors для absolute paths, secret-like fields и Editor UI state fields.
- Automated tests проверяют, что resource rename/move сохраняет `DocumentId` через UID и остаётся пригодным для structural diff.
- JSON Schema Draft 2020-12 files опубликованы и покрыты tests.
- Implementation documentation описывает команды focused verification и текущие ограничения.

## Фактическое состояние, ограничения и проверки

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

Published schemas live in `data/schemas/project-system/`:

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
