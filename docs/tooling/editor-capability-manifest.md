# Editor Capability Manifest

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация `0.1-preview`.
Задача: `T-0142`.
Связанные документы: [Agent-native workflow](../architecture/agent-native-workflow.md); [Electron2D.Tooling service boundary](tooling-service-boundary.md); [Локальный MCP-сервер](../mcp/mcp-server.md); [Machine-readable API manifest](../api-manifest.md).

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
- `manifestVersion`: `0.1-preview`;
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
3. `releaseRequired` capability с kind `projectMutation` или `backgroundJob` имеет `cli.kind = notApplicable`.
4. `releaseRequired` capability с kind `runtimeAction` имеет `cli.kind = notApplicable` без supported Tooling/MCP parity. Visible Editor-only runtime actions могут не иметь dedicated CLI route, если они доступны через active Editor Tooling/MCP session.
5. CLI binding имеет неизвестный kind или dedicated binding без команды.
6. Manifest не покрывает обязательную категорию.
7. Manifest не ссылается на существующий JSON API manifest.
8. Capability ссылается на Tooling command или MCP tool/resource, которого нет в опубликованных списках.
9. Есть дублирующиеся capability identifiers.

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
- Тест подтверждает, что verifier падает при `notApplicable` CLI binding для release-required project/background operation.
- Тест подтверждает, что fine-grained visible runtime controls могут быть release-required с supported Tooling/MCP и CLI `notApplicable`.
- MCP resource `electron2d://editor/capabilities` возвращает manifest.
- Документация реализации описывает artifact, verifier, команды проверки и текущие ограничения.

## Фактическое состояние, ограничения и проверки

Статус: реализованный internal contract для `T-0142`.
Обновлено: 2026-06-23.
Связанные документы: [Editor Capability Manifest specification](editor-capability-manifest.md); [Electron2D.Tooling service boundary](tooling-service-boundary.md); [Локальный MCP adapter](../mcp/mcp-server.md); [Machine-readable API manifest](../api-manifest.md).

`Editor Capability Manifest` — машиночитаемый список возможностей Editor, которые имеют проектный смысл и должны быть доступны AI через Tooling/MCP без GUI automation. Он не заменяет реализацию Editor UI и не делает частично готовые workflows завершёнными; manifest фиксирует текущий статус каждой capability и проверяет, что заявленный support не расходится между Editor, Tooling, MCP и CLI.

## Artifact

Canonical tracked file:

```text
data/editor/electron2d-editor-capabilities.json
```

Файл создаётся тем же порядком полей, что и `EditorCapabilityManifestSerializer.Serialize(EditorCapabilityManifestFactory.CreateDefault())`. Integration test сравнивает generated output с tracked JSON byte-for-byte после нормализации line endings.

Manifest ссылается на public API manifest:

```text
data/api/electron2d-api-manifest.json
```

Stable API identifiers используются для связи capability с runtime/API профилем, Inspector properties, scene/resource operations и runtime control.

## Current coverage

Текущий manifest покрывает обязательные категории:

- scene, node, Inspector properties, signals и groups;
- resources и import settings;
- Input Map, Project Settings, main scene и export presets;
- SpriteFrames, AnimationPlayer, TileMap и UI themes;
- tests, diagnostics, runtime control, script workspace и managed debugger control.

Часть строк уже `supported`: shared workspace transaction для text project documents, resource import job, test/export/run job, diagnostics query, fine-grained visible runtime controls, script document mutations, live C# IDE queries и managed debugger control. Часть строк помечена `partial`: специализированные SpriteFrames/AnimationPlayer/TileMap/UI theme workflows. Такой статус означает, что строка входит в parity map, но её production workflow закрывается последующими задачами.

Новые release-required строки для `T-0161`:

- `script.workspace.mutate` связывает visible Script workspace, Tooling command `script_apply_text_edits` и MCP tool `script_apply_text_edits`;
- `script.workspace.ide` связывает visible Script workspace, Tooling command `script_get_diagnostics` и MCP tool `script_get_diagnostics`;
- `debugger.managed.control` связывает visible debugger workspace, Tooling command `debug_start` и MCP tool `debug_start`.

## Verifier

`EditorCapabilityManifestVerifier.Verify(...)` принимает manifest, опубликованные MCP tool/resource names, опубликованные Tooling command names и repository root. Он возвращает `EditorCapabilityManifestVerificationResult` со structured diagnostics.

Проверяются:

- schema/version;
- существование `data/api/electron2d-api-manifest.json`;
- дубли capability identifiers;
- покрытие required categories;
- обязательные поля endpoint-ов;
- `supported` Editor capability не имеет более слабых Tooling/MCP binding;
- `releaseRequired` capability не использует partial/experimental Tooling или MCP;
- `projectMutation` и `backgroundJob` release capability имеют `dedicatedCommand` или `genericTransaction`, а не `notApplicable`;
- release-required `runtimeAction` может иметь CLI `notApplicable`, если действие имеет смысл только для активной visible Editor session и покрыто Tooling/MCP parity;
- Tooling command и MCP tool/resource опубликованы в catalog.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `E2D-CAPABILITY-0001` | Editor/Tooling/MCP parity нарушен: поддержанная Editor capability не имеет полной Tooling/MCP поддержки или release-required строка использует partial/experimental binding |
| `E2D-CAPABILITY-0002` | CLI binding policy некорректна: release-required mutation/job capability не имеет dedicated или generic CLI path |
| `E2D-CAPABILITY-0003` | Manifest shape некорректен: версия, API manifest reference, duplicate id, required category или mandatory field |
| `E2D-CAPABILITY-0004` | Capability ссылается на Tooling command или MCP tool/resource, которого нет в опубликованных списках |

## MCP exposure

MCP resource:

```text
electron2d://editor/capabilities
```

возвращает canonical manifest JSON.

`e2d mcp serve --format json` добавляет `data.editorCapabilityManifest`:

- `path`;
- `capabilities`;
- `releaseRequired`;
- `succeeded`;
- `diagnostics`.

Это краткое поле нужно AI-клиентам для fail-closed проверки manifest без отдельного resource read. Полный список capabilities остаётся в resource `electron2d://editor/capabilities`.

## Checks

Focused check:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~EditorCapabilityManifestTests
```

Regression slice:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorCapabilityManifestTests|FullyQualifiedName~Electron2DMcpServerTests|FullyQualifiedName~ToolingServiceBoundaryTests|FullyQualifiedName~DiagnosticsCoreTests|FullyQualifiedName~Electron2DCliWorkflowTests"
```

Source/header and documentation checks remain required before commit when files in `src/`, `tests/`, `tools/` or `docs/` changed.
