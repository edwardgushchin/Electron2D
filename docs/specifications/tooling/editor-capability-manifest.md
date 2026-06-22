# Editor Capability Manifest

Статус: целевая спецификация `0.1.0 Preview`.
Задача: `T-0142`.
Связанные документы: [Agent-native workflow](../architecture/agent-native-workflow.md); [Electron2D.Tooling service boundary](tooling-service-boundary.md); [Локальный MCP-сервер](../mcp/mcp-server.md); [Machine-readable API manifest](../documentation/api-manifest.md).

`Editor Capability Manifest` — машиночитаемый список семантически значимых возможностей редактора. Семантически значимая возможность меняет или наблюдает проектный смысл: сцену, узлы, Inspector properties, ресурсы, signals, groups, Input Map, Project Settings, SpriteFrames, AnimationPlayer, TileMap, UI themes, import settings, main scene, export presets, tests, diagnostics или runtime control.

Manifest нужен для проверяемого AI-паритета: если Editor объявляет возможность поддержанной, Tooling и MCP должны иметь не более слабую поддержку, а CLI должен иметь явную binding policy.

## Canonical artifact

Canonical tracked artifact:

```text
data/editor/electron2d-editor-capabilities.json
```

Файл не является ручным backlog. Его canonical content создаётся из `EditorCapabilityManifestFactory.CreateDefault()` и проверяется тестом. Если добавляется новая project operation, нужно обновить factory, tracked JSON, verifier expectations, документацию и MCP/CLI surface.

## JSON contract

Root object:

- `schemaVersion`: `1`;
- `manifestVersion`: `0.1.0-preview`;
- `apiManifest.path`: `data/api/electron2d-api-manifest.json`;
- `apiManifest.references`: stable API identifiers, на которые ссылаются Inspector, scene/resource, UI и runtime capabilities;
- `capabilities`: отсортированный список capability records.

Capability record:

- `capability`: stable identifier, например `scene.node.set_property`;
- `title`: человекочитаемое название;
- `categories`: непустой список категорий;
- `kind`: одно из `projectMutation`, `editorSessionAction`, `runtimeAction`, `backgroundJob`, `readOnlyQuery`;
- `releaseRequired`: `true`, если capability входит в текущий обязательный release gate и не может оставаться partial;
- `editor.command`;
- `editor.status`: `supported`, `partial`, `experimental`, `not_applicable`;
- `editor.explanation`;
- `tooling.command`;
- `tooling.status`;
- `tooling.explanation`;
- `mcp.toolOrResource`;
- `mcp.status`;
- `mcp.explanation`;
- `cli.kind`: `dedicatedCommand`, `genericTransaction`, `notApplicable`;
- `cli.command`, если `cli.kind = dedicatedCommand`;
- `cli.explanation`.

Все текстовые значения должны быть непустыми, кроме `cli.command`, когда binding не является dedicated command.

## Required categories

Manifest обязан покрывать категории:

- `scene`;
- `node`;
- `inspector`;
- `resources`;
- `signals`;
- `groups`;
- `input-map`;
- `project-settings`;
- `spriteframes`;
- `animationplayer`;
- `tilemap`;
- `ui-themes`;
- `import-settings`;
- `main-scene`;
- `export-presets`;
- `tests`;
- `diagnostics`;
- `runtime-control`.

Категория может быть покрыта capability со статусом `partial`, если сама возможность ещё не реализована как release gate. Это не означает готовность конечного workflow; это означает, что capability уже видима как обязательная строка будущего parity contract.

## Verifier rules

Verifier должен возвращать structured diagnostics и `Succeeded = false`, если:

1. Supported Editor capability имеет Tooling или MCP status ниже `supported`.
2. `partial` или `experimental` у Tooling/MCP используется для `releaseRequired = true`.
3. `releaseRequired` capability с kind `projectMutation`, `runtimeAction` или `backgroundJob` имеет `cli.kind = notApplicable`.
4. CLI binding имеет неизвестный kind или dedicated binding без команды.
5. Manifest не покрывает обязательную категорию.
6. Manifest не ссылается на существующий JSON API manifest.
7. Capability ссылается на Tooling command или MCP tool/resource, которого нет в опубликованных списках.
8. Есть дублирующиеся capability identifiers.

Diagnostic codes:

- `E2D-CAPABILITY-0001`: нарушен parity между supported Editor capability и Tooling/MCP binding.
- `E2D-CAPABILITY-0002`: некорректная CLI binding policy для release-required capability.
- `E2D-CAPABILITY-0003`: manifest shape, API manifest reference, duplicate id или required category некорректны.
- `E2D-CAPABILITY-0004`: capability ссылается на отсутствующий Tooling command или MCP tool/resource.

## MCP and CLI exposure

`electron2d://editor/capabilities` должен возвращать этот manifest как MCP resource.

`e2d mcp serve --format json` должен включать summary:

- `editorCapabilityManifest.path`;
- `editorCapabilityManifest.capabilities`;
- `editorCapabilityManifest.releaseRequired`;
- `editorCapabilityManifest.diagnostics`;
- `editorCapabilityManifest.succeeded`.

Само наличие capability в manifest не даёт права обходить Tooling или `ProjectWorkspace`. Узкие tools, чья production semantics ещё не реализована, должны оставаться fail-closed и возвращать structured diagnostics.

## Acceptance criteria

- Тест подтверждает, что canonical factory output совпадает с tracked JSON.
- Тест подтверждает покрытие всех required categories.
- Тест подтверждает, что default manifest проходит verifier.
- Тест подтверждает, что verifier падает при supported Editor capability без supported Tooling/MCP.
- Тест подтверждает, что verifier падает при `notApplicable` CLI binding для release-required project/runtime/background operation.
- MCP resource `electron2d://editor/capabilities` возвращает manifest.
- Документация реализации описывает artifact, verifier, команды проверки и текущие ограничения.
