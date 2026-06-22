# Stable project text formats, migrations и JSON Schema

Статус: целевая спецификация для `T-0117`.
Обновлено: 2026-06-22.
Связанные документы: [Canonical document model, revision model и structural diff](canonical-document-model.md), [AI-friendly workflow Electron2D 0.1](../architecture/ai-friendly-workflow.md), [Сериализация сцен, ресурсов и переносимых property values](../resources/scene-resource-serialization.md), [Resource file baseline, stable UID и ссылки ресурсов](../resources/resource-file-baseline.md).

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

JSON Schema Draft 2020-12 files должны публиковаться в `schemas/project-system/`:

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
