# Архитектура и платформенный стек Electron2D

Статус: актуализированная целевая спецификация.
Источник: исторически перенесено из корневого `GOAL.md`, затем синхронизировано с текущим release contract.
Synchronized with `docs/specifications/releases/0.1.0-preview.md`.
Последнее обновление: 2026-06-23.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md); [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](agent-native-workflow.md); [Live ProjectWorkspace](../project-system/live-project-workspace.md).

## Canonical status

Этот документ больше не является самостоятельной старой целью проекта. Если исторический текст из `GOAL.md`, ранние architecture notes или локальные release drafts расходятся с `0.1.0 Preview`, действующим источником является [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md) и связанные спецификации.

Для `0.1.0 Preview` canonical contract фиксирует:

- поддерживаемые платформы release contract: Windows, Linux, macOS, Android, iOS и WebAssembly browser;
- desktop Editor только на Windows, Linux и macOS;
- Android, iOS и WebAssembly browser как runtime/export platforms, а не платформы редактирования;
- specialized node/resource model — специализированная node/resource модель Electron2D, а не прежняя component model;
- `Node2D` transform принадлежит `Node2D` и его 2D-наследникам; базовый `Node` не получает обязательный transform;
- `scene_attach_script` не добавляет отдельный Script-компонент, а связывает сериализованный node с пользовательским C#-типом, наследующим подходящий Electron2D node type;
- public runtime API строится только из утверждённого 2D-профиля и не возвращает legacy/component API.

## Цель платформенного слоя

Electron2D является C#-first кроссплатформенным 2D-движком с desktop-редактором и headless tooling path. Платформенный слой должен дать одинаковую архитектурную границу для runtime, Editor, import/build/export tools и будущего локального IPC, не раскрывая concrete native handles или backend-specific types в public API.

В публичных документах backend choices описываются как внутренние platform, rendering, audio, text, input, filesystem, physics и export backends. Конкретные библиотеки и wrappers являются implementation detail: они могут меняться без изменения public node/resource API, если сохраняется наблюдаемое поведение утверждённого 2D-профиля.

## Подсистемы

| Подсистема | Canonical boundary для `0.1.0 Preview` | Public API policy |
| --- | --- | --- |
| Window/input/lifecycle | internal platform backend для окна, desktop/mobile lifecycle, keyboard, mouse, gamepad и touch events | public events остаются Electron2D input types без platform handles |
| Rendering | internal rendering backend с `Standard` и `Compatibility` profiles | public API ограничен `RenderingServer`, resources, nodes и documented feature flags |
| Text | internal text backend для glyph layout, fallback fonts, Unicode, IME и mixed direction text | public API не раскрывает native font/layout handles |
| Audio | internal audio backend для voices, buses, streaming/static playback и 2D attenuation | public API остаётся `AudioServer`, `AudioStream*` и playback nodes |
| Physics 2D | internal swappable physics backend за `PhysicsServer2D` boundary | public API раскрывает `Rid`, shapes, bodies и queries, но не backend-specific handles |
| Resources/import | managed resource import pipeline, stable UID, import cache и deterministic metadata | import cache не является source of truth и не редактируется через scene API |
| Export | unified export preset model и platform-specific package planners | signing credentials остаются user-provided references без секретов в source files |
| Project tooling | `ProjectWorkspace`, snapshots, jobs, diagnostics и Tooling command boundary | CLI/MCP/Editor используют один semantic model, а не GUI automation |

## Платформы

| Платформа | Editor | Runtime | Export | Статус `0.1.0 Preview` |
| --- | ---: | ---: | ---: | --- |
| Windows x64 | Да | Да | Да | Tier 1 desktop |
| Linux x64 glibc | Да | Да | Да | Tier 1 desktop |
| macOS arm64 | Да | Да | Да | Tier 1 desktop |
| Android arm64 | Нет | Да | Да | Tier 1 runtime/export target после platform tasks |
| iOS arm64 | Нет | Да | Да | Tier 1 runtime/export target после platform tasks |
| WebAssembly browser | Нет | Да | Да | Tier 1 runtime/export target после web task |

Поддержка платформы означает проверенный запуск, rendering, input, audio, resource loading, scene switch, filesystem behavior, pause/resume where applicable, shutdown и reference project checks. Mobile export не считается release-ready до закрытия соответствующих Android/iOS задач и real-device или simulator smoke. WebAssembly browser export не считается release-ready до закрытия web task, static package layout, browser runtime policy и browser smoke artifact.

## Public runtime API

Публичный runtime API Electron2D `0.1.0` воспроизводит утверждённый 2D-профиль Godot `4.7-stable` .NET/C# API под namespace `Electron2D`. Это техническая совместимость с выбранным API-подмножеством Godot, а не обещание реализовать весь Godot API.

Для каждого типа внутри профиля должны совпадать public inheritance, properties, methods, overloads, events, signals, enum values, constants, parameter and return types, default values и observable behavior. Типы вне профиля не входят в обязательный contract.

Запрещено возвращать прежний public component API ради удобства migration. В частности, public surface не должен снова получать legacy `IComponent`, `SpriteRenderer`, `AudioSource`, старые physics components, compatibility wrappers или aliases из прежней реализации.

## Editor и tooling boundary

`Electron2D.Editor` строится на Electron2D runtime и работает с той же `ProjectWorkspace`, что будущие Tooling, CLI, MCP и CI paths. Editor не должен владеть приватной model сцены, недоступной semantic commands.

Core workflow:

```text
Editor / CLI / MCP / CI
        ↓
ProjectWorkspace
        ↓
documents, revisions, diagnostics, jobs, snapshots
        ↓
runtime/import/build/export subsystems
```

`ProjectWorkspace` и Tooling commands являются internal/product tooling layer, а не public game runtime API. Они должны поддерживать dirty documents, expected revisions, structured diagnostics, operation journal, undo groups и future live event streams.

## Данные проекта

Project source files должны оставаться deterministic и diff-friendly:

- сцены, resources и settings имеют stable text formats;
- resource references используют stable UID;
- generated/import cache files отделены от source documents;
- `.electron2d/tasks/**` является EditorMetadata и не попадает в game runtime asset packs или production packages;
- local-only agent workflow files, такие как repository `TASKS.md`, `dev-diary/`, `completed-tasks/`, `CHANGELOG*` и `RELEASE-NOTES*`, не являются canonical project storage.

## Mobile и export status

Android, iOS и WebAssembly browser входят в release contract как runtime/export platforms, но их готовность подтверждается только после закрытия соответствующих platform tasks. Документация может описывать target requirements, signing references, browser hosting metadata и expected smoke checks, но не должна считать mobile или web export готовым release path до реальной проверки.

Remote Android/iOS/WebAssembly debugger, publishing automation, app store upload, cloud signing, remote hosting deploy и произвольное выполнение shell через MCP не входят в `0.1.0 Preview`.

## Verification

Canonical goal alignment проверяется локально:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-CanonicalGoalAlignment.ps1
```

Verifier проверяет, что этот документ и release/Agent-native cross-platform 2D game engine specifications закрепляют актуальный native-plus-browser platform contract, specialized node/resource, `Node2D` transform и `scene_attach_script` contract, а старое backend-specific или component-first позиционирование не возвращается как действующая цель.
