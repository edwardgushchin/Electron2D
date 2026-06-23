# Документация реализации Electron2D

Этот раздел предназначен для описания фактически реализованного поведения движка, редактора, инструментов, экспортного пайплайна и проверок.

Целевые требования и проектные контракты живут в `docs/specifications/`. Когда задача меняет исполняемый код, API, доменную модель, конфигурацию или пользовательское поведение, соответствующая справка должна обновляться здесь вместе с реализацией.

## Текущий baseline после reset

С 2026-06-20 реализация `0.1.0 Preview` строится заново: старый каталог `src/Electron2D/` удалён, а дальнейшие задачи должны создавать только согласованный публичный API без compatibility layer и без legacy component history.

## Архитектура

- [Source domain layout](architecture/source-domain-layout.md) - текущая раскладка `src/Electron2D`, узкий `Core`, отдельные домены подсистем и namespace policy.

## Релизное управление

- [Тестовая инфраструктура 0.1.0 Preview](release-management/test-infrastructure.md) - текущие тестовые проекты, команды запуска и baseline-режим.
- [CI-матрица 0.1.0 Preview](release-management/ci-matrix.md) - текущий GitHub Actions workflow и локальная проверка его структуры.
- [Таблица совместимости API](release-management/api-compatibility.md) - GitHub Wiki repository и verifier текущего public API surface.
- [Версионирование и release metadata 0.1.0 Preview](release-management/release-metadata.md) - текущие package metadata, changelog и release notes.
- [Формат проекта и шаблон electron2d-empty](release-management/project-template.md) - текущий AI-ready шаблон проекта и проверка его сборки/запуска.
- [Performance budgets и soak-критерии 0.1.0 Preview](release-management/performance-budgets.md) - текущая матрица устройств и release-gate бюджеты.

## Качество

- [Performance verification для 0.1.0 Preview](quality/performance-verification.md) - текущий verifier empty scene, sprite scene, reference games, steady allocations и batching evidence.
- [Leak verification для 0.1.0 Preview](quality/leak-verification.md) - текущий verifier для texture/render-target, audio voice, physics RID и scene load/unload cycles.

## Репозиторий

- [Политика MIT License и source headers](repository/license-policy.md) - текущая проверка корневой лицензии и license header в исходных файлах.
- [Раскладка репозитория и локальных рабочих материалов](repository/repository-layout.md) - текущие local-only рабочие файлы и `data/` root для шаблонов/ассетов.

## Project system

- [Canonical document model](project-system/canonical-document-model.md) - текущий internal project-system слой для document identity, classification, revisions, parser snapshot, debug serializer и structural diff.
- [Stable project text formats](project-system/project-text-formats.md) - текущий formatter/schema/migration layer для scene/resource/project JSON, validation safety checks и published JSON Schema files.
- [Live ProjectWorkspace](project-system/live-project-workspace.md) - текущий internal workspace core для ownership lease, document store, revision/dirty state, in-memory events, operation journal и diagnostics store.
- [WorkspaceSnapshot](project-system/workspace-snapshot.md) - текущий internal snapshot core для build/test/run/export input identity, materialization, stale rules и dirty export policy.
- [WorkspaceJob contract и event stream](project-system/workspace-jobs.md) - текущий internal job core для import/build/test/export/run lifecycle, progress, cancel, diagnostics, artifacts и stale markers.
- [WorkspaceTransactionEngine](project-system/workspace-transactions.md) - текущий internal transaction core для project operations: dry-run, revision checks, save/headless/external import modes, atomic write, conflicts и grouped undo.
- [Human-AI concurrent editing и grouped Undo](project-system/concurrent-editing-and-undo.md) - текущая internal policy для совместного редактирования, conflict records, operation provenance, reversible undo groups, external batch undo и binary asset pending conflicts.
- [ProjectTaskManager](project-system/project-task-manager.md) - текущий internal task core для встроенных задач пользовательского проекта, activity, human acceptance guard, stable task documents, dependency graph, transaction integration и read-only Markdown report export.
- [Статический context pack проекта](project-system/static-context-pack.md) - текущая CLI-команда `e2d context build`, generated `.electron2d/context/`, snapshot semantics, output layout и security/size policy.
- [Reproducibility lock и e2d doctor](project-system/reproducibility-lock-and-doctor.md) - текущие `global.json`, `electron2d.lock.json`, lock verifier, read-only CLI диагностика окружения и защита signing references от раскрытия секретов.
- [External Change Synchronizer](project-system/external-change-synchronizer.md) - текущий internal слой для файловых изменений вне Editor: watcher, debounce, фильтры, `ExternalImport`, task guards и live import state.

## Diagnostics

- [Diagnostics.Core](diagnostics/diagnostics-core.md) - текущий internal contract для structured diagnostics, stable code registry, locations, safe suggested fixes и deterministic JSON serialization.
- [Diagnostics adapters: JSON, JSONL stream и SARIF](diagnostics/diagnostics-adapters.md) - текущий adapter слой для полного diagnostic payload в CLI/JSONL, diagnostics stream events, SARIF 2.1.0 output и published schemas.

## Tooling

- [Electron2D.Tooling service boundary](tooling/tooling-service-boundary.md) - текущий internal project для общего operation result, project transaction wrappers, task service wrappers и job-backed long operations.
- [Editor session discovery и Editor-hosted Agent Gateway](tooling/editor-session-discovery.md) - текущий internal registry/gateway contract для active Editor discovery, lease/heartbeat, endpoint validation, read-only второго Editor и headless fallback CLI/MCP adapter-ов.
- [Editor Capability Manifest](tooling/editor-capability-manifest.md) - текущий tracked manifest возможностей Editor, Tooling/MCP parity verifier, CLI binding policy и MCP exposure.

## CLI

- [`e2d` CLI для headless, CI и active Editor routing](cli/e2d-cli.md) - текущий executable CLI parser, common flags, stable JSON/JSONL envelope, generic workspace transaction, job stubs и route selection через active Editor или headless workspace.

## Runtime

- [Headless runtime automation](runtime/headless-runtime-automation.md) - текущий `e2d run` headless mode: fixed frame loop, input trace, stable runtime artifacts, JSON schemas и snapshot identity для CI и автономных агентов.
- [Runtime debug bridge и scene inspection](runtime/runtime-debug-bridge.md) - текущий shared runtime inspection contract для Remote Scene Tree, inspect node, pause/step/input/screenshot, metrics и CLI `e2d run debug`.
- [Editor-attached runtime control](runtime/editor-attached-runtime-control.md) - текущий shared runtime session для видимого Editor-owned run, Tooling/MCP pause/step/input/screenshot/tree/diagnostics и crash isolation.

## Тестирование

- [Scene tests и visual regression tests](testing/scene-visual-testing.md) - текущий `Electron2D.Testing` layer, `e2d test --format json`, scene assertions, visual artifacts, progress events, diagnostics и JSON schemas.
- [Agent acceptance benchmarks](testing/agent-acceptance-benchmarks.md) - текущий release gate manifest и runner для Editor co-development и headless AI benchmark suites.

## MCP

- [Локальный MCP adapter для Editor-сессии и Tooling](mcp/mcp-server.md) - текущий in-process contract для local resources/tools, route selection, workspace transactions, job events, task guard и `e2d mcp serve` manifest.

## Документация

- [Пользовательская документация 0.1.0 Preview](documentation/user-guide.md) - проверенный путь установки, первого проекта, сцены, scripting, ресурсов, physics, UI, animation, Input Map и export limitations.
- [Renderer profiles](documentation/renderer-profiles.md) - пользовательское описание `Compatibility`, `Standard`, feature flags, Android fallback и `fail_if_unavailable`.
- [Troubleshooting guide и release checklist](documentation/troubleshooting-release-checklist.md) - проверяемые действия для import, build, shader, export, mobile lifecycle, runtime diagnostics и preview release gate.
- [XML documentation публичного API](documentation/public-api-xml-documentation.md) - текущее правило, команды verifier и статус заполнения XML comments.
- [GitHub Wiki API reference](documentation/github-wiki-api-reference.md) - текущий генератор GitHub Wiki и проверка синхронизации с публичным API.
- [Machine-readable API manifest](documentation/api-manifest.md) - текущий JSON manifest публичного API, stable identifiers, compatibility status и CI-проверка синхронизации.
- [Local documentation pipeline](documentation/local-documentation-pipeline.md) - текущий generated local-docs index и команды `e2d docs search/type/member/example`.
- [Canonical goal alignment audit](documentation/canonical-goal-alignment.md) - текущая проверка, что исторические goal/architecture материалы не возвращают устаревший project contract.

## Ресурсы, импорт и сериализация

- [Resource file baseline, stable UID и ссылки ресурсов](resources/resource-file-baseline.md) - текущий public `ResourceUid`, internal `.e2res` document model, external/internal references и golden-data формат.
- [Import cache ресурсов](resources/resource-import-cache.md) - текущий internal pipeline для `.e2res` discovery, reimport on source/dependency changes, safe cache writes и prune unused cache.
- [Импорт PNG/JPEG в Texture2D и AtlasTexture](resources/texture-image-import.md) - текущий internal importer для image metadata, sidecar settings, atlas regions и platform variants.
- [Импорт TTF/OTF в Font](resources/font-import.md) - текущий internal importer для font metadata, fallback dependencies и SDF/bitmap policy.
- [Импорт shader source в platform-specific artifacts](resources/shader-source-import.md) - текущий internal importer для `.e2shader`, compiled stages, diagnostics file/line/column и iOS precompiled artifacts.
- [Сериализация сцен, ресурсов и переносимых property values](resources/scene-resource-serialization.md) - текущие internal scene/resource JSON documents, custom `Resource` round-trip и typed property value model.
- [AOT-safe metadata для Inspector и serialization](resources/aot-safe-metadata.md) - текущий internal metadata registry для custom `Resource` serialization без reflection fallback.
- [Stress data stability для scene/resource pipeline](resources/data-stability-stress.md) - текущий release-gate набор для 100 save/load cycles, rename/move, cache rebuild и corruption diagnostics.

## C# scripting

- [C# script classes, inheritance from `Node` и lifecycle](scripting/csharp-script-classes.md) - текущая ordinary .NET модель script classes, template sample и lifecycle/services checks.
- [Script metadata: `[Export]`, `[Signal]`, `[Tool]`](scripting/script-metadata.md) - текущие публичные marker attributes и internal metadata bridge для export properties, signals и tool-state.
- [Безопасное editor-time выполнение `[Tool]` scripts](scripting/tool-script-execution.md) - текущий внутренний execution host для registered tool metadata без dynamic assembly load.
- [Script workflow в редакторе](scripting/editor-script-workflow.md) - текущая внутренняя модель создания, встроенного редактирования, attach к node, build diagnostics и запуска проекта после rebuild.
- [C# language services в Script workspace](scripting/editor-language-services.md) - текущая отдельная Roslyn-backed сборка для completion, signature help, hover, live diagnostics, navigation, rename, formatting, code actions и visual acceptance harness.
- [Выбор managed .NET debug adapter](scripting/managed-debug-adapter-selection.md) - текущий `netcoredbg` selection manifest, DAP capability matrix, license/redistribution decision и platform packaging plan для managed debugger.
- [Managed C# debugger в редакторе](scripting/managed-debugger.md) - текущая внутренняя сборка `Electron2D.ManagedDebugging`, local breakpoints, DAP boundary, debug session state и visual acceptance harness.
- [Script/Debugger Tooling parity](scripting/script-debug-tooling-parity.md) - текущий Tooling/MCP контракт для script edits, IDE-запросов, debugger commands и visual smoke с Agent Workspace.

## Редактор

- [Electron2D.Editor project shell](editor/editor-project-shell.md) - текущий executable shell редактора, smoke-запуск на runtime Electron2D и отсутствие внешнего desktop UI framework.
- [Editor shell layout и visual harness](editor/editor-shell-layout.md) - текущий default layout редактора, central workspaces, docks, layout persistence, shortcut map и PNG/JSON visual acceptance harness.
- [Project Manager редактора](editor/project-manager.md) - текущая внутренняя логика создания/открытия проекта, recent projects, renderer profile и SDK smoke-проверка.
- [Agent process bootstrap из Editor](editor/agent-process-bootstrap.md) - текущий internal contract для agent profiles, временной MCP configuration, safe process plan, handshake state и подключения к active Editor route.
- [Agent Workspace panel редактора](editor/agent-workspace-panel.md) - текущая внутренняя UI-модель Agent Workspace dock, current task, changeset, diagnostics, artifacts, runtime snapshot, jobs, actions и PNG/JSON visual acceptance harness.
- [Project Tasks board редактора](editor/project-tasks-board.md) - текущая внутренняя UI-модель центрального `Tasks` workspace, task details в Inspector, filters, actions, drag-and-drop intent, human acceptance guard и PNG/JSON visual acceptance harness.
- [Script workspace редактора](editor/script-workspace.md) - текущая внутренняя UI-модель центрального `Script` workspace, вкладки C# documents, line gutter, caret/selection, search/replace, diagnostics, conflict marker и PNG/JSON visual acceptance harness.
- [Scene Tree dock редактора](editor/scene-tree-dock.md) - текущая внутренняя логика редактирования дерева сцены, undo/redo и синхронизация с runtime `Tree`.
- [2D Viewport редактора](editor/viewport-2d.md) - текущая внутренняя модель pan/zoom, выбора, transform-инструментов, snapping, bounds, collision overlays и camera preview.
- [Inspector редактора](editor/inspector.md) - текущая внутренняя модель редактирования saved properties, reset defaults, nested resources, serialization и undo/redo.
- [FileSystem dock редактора](editor/file-system-dock.md) - текущая внутренняя модель просмотра project files, folders, rename/move resources, reimport, search, drag resource into scene и visible import errors.
- [Specialized editors в `Electron2D.Editor`](editor/specialized-editors.md) - текущий workflow для `SpriteFrames`, `TileMap` и `AnimationPlayer`: runtime text resources, scene file, reopen round-trip и real-window visual smoke.
- [Run/output workflow редактора](editor/run-output-workflow.md) - текущая модель внутри редактора для запуска project/current scene, stop, output console, diagnostics и frame timing.

## Ввод

- [Input event mapping и `InputEvent*`](input/sdl-input-event-mapping.md) - текущий internal platform mapper для keyboard, mouse, wheel, text input и dispatch order.
- [InputMap, action state и persistence baseline](input/input-map-actions.md) - текущие `InputMap`, `Input`, action bindings, deadzone, `GetVector()` и internal serializer input settings.
- [Input dispatch, UI focus и mouse filter baseline](input/input-dispatch-ui-focus.md) - текущий порядок `_Input()`, `Control._GuiInput()`, handled-state, focus ownership и mouse filter.
- [Gamepad input baseline](input/gamepad-input.md) - текущие `JoyAxis`, `JoyButton`, `InputEventJoypad*`, connected gamepads, action bindings и vibration API.
- [Mobile input baseline](input/mobile-input.md) - текущие `InputEventScreenTouch`, `InputEventScreenDrag`, mobile navigation, virtual keyboard, orientation и safe area state.

## UI

- [Control layout core](ui/control-layout-core.md) - текущие anchors/offsets, minimum size, grow direction, clipping hit-test и focus navigation baseline.
- [UI containers](ui/containers.md) - текущие `Container`, `BoxContainer`, `HBoxContainer`, `VBoxContainer`, `GridContainer`, `MarginContainer`, `CenterContainer`, `ScrollContainer`, size flags и theme constants.
- [Базовые UI controls](ui/basic-controls.md) - текущие `Panel`, кнопки, текстовый ввод, `Range`, `Slider`, `ProgressBar`, `TextureRect` и `NinePatchRect`.
- [Структурные UI controls](ui/structured-controls.md) - текущие `ItemList`, `Tree`, `TreeItem`, `PopupMenu`, `TabContainer`, selection, activation и tab switching.
- [Темы, DPI scale и tooltips UI](ui/theme-tooltips.md) - текущие `Theme`, `StyleBox`, `StyleBoxFlat`, theme overrides, lookup fallback, base scale и tooltip source API.
- [UI public API gate](ui/public-api-gate.md) - текущая проверка, которая требует статус `Supported` для всех UI/Text строк GitHub Wiki compatibility table перед editor-задачами.

## Локализация и настройки

- [Translation resource, locale switching и `Tr`](settings-localization/translation-runtime.md) - текущие `Translation`, `TranslationServer`, locale fallback и обновление `Label` после смены locale.
- [Unicode, IME и текст справа налево](settings-localization/unicode-ime-rtl-text.md) - текущий support level для Unicode scalar values, committed IME text input и mixed LTR/RTL/emoji UI layout.
- [Settings persistence baseline](settings-localization/settings-persistence.md) - текущий внутренний JSON-контракт project/user settings, input actions, display/window defaults и fail-closed diagnostics.

## Export pipeline и платформы

- [Export guide](export/export-guide.md) - пользовательская матрица export targets, desktop verifier commands, мобильные ограничения, signing references, политика секретов и known limitations.
- [Export preset model и toolchain validation](export/export-preset-model.md) - текущий внутренний JSON-контракт export presets, SDK/toolchain/signing checks и fail-closed diagnostics без запуска сборки или публикации.
- [Windows x64 export](export/windows-x64-export.md) - текущий internal package plan и локальная проверка `win-x64` self-contained publish/run.
- [Linux x64 glibc export](export/linux-x64-export.md) - текущий internal package plan и локальная проверка `linux-x64` self-contained publish/run на Linux или через WSL.
- [macOS arm64 export](export/macos-arm64-export.md) - текущий internal package plan, `.app` bundle и проверка `osx-arm64` self-contained publish/run на macOS arm64.
- [Android arm64 export](export/android-arm64-export.md) - текущий planner/staging/debug APK baseline, release AAB signing plan, `adb` run-smoke, engine icon/logo/fullscreen policy и emulator smoke evidence.
- [iOS arm64 export](export/ios-arm64-export.md) - текущий заблокированный статус mobile export, будущие требования Xcode/signing и запрет считать iOS export готовым release path.
- [WebAssembly browser export](export/webassembly-browser-export.md) - текущий internal planner, package builder, CLI plan/build/run commands для `browser-wasm`, static package layout, browser policies и smoke artifact.

## Примеры и reference games

- [Ассеты reference games](examples/reference-game-assets.md) - текущий локальный asset pack, license metadata, manifest и verifier для будущих reference games.
- [Reference platformer](examples/reference-platformer.md) - текущий валидный проект `Electron2D.Editor`, gameplay subsystem markers, assets manifest, export presets и verifier.
- [UI-heavy reference game](examples/ui-heavy-reference.md) - текущий валидный проект `Electron2D.Editor`, карточный UI workflow, локализация, Android `Compatibility` preset, assets manifest и verifier.
- [Reference game platform matrix](examples/reference-game-platform-matrix.md) - текущий общий verifier, который проверяет оба reference project как одну shared-codebase export matrix для Windows, Linux, macOS, Android, iOS и WebAssembly browser.

## Физика 2D

- [PhysicsServer2D boundary](physics/physics-server-2d.md) - текущая public `Rid`-граница физики, internal backend boundary и запрет публичных Box2D handles.
- [Box2D.NET platform/AOT validation](physics/box2d-net-validation.md) - текущий candidate smoke gate, desktop JIT/NativeAOT проверка, mobile Release/AOT gaps и allocations per tick.
- [Physics nodes lifecycle baseline](physics/physics-nodes-lifecycle.md) - текущие `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D`, RID lifecycle и transform sync.
- [Shape2D resources baseline](physics/shape2d-resources.md) - текущие `RectangleShape2D`, `CircleShape2D`, `CapsuleShape2D`, `SegmentShape2D`, `ConvexPolygonShape2D`, `ConcavePolygonShape2D`, validation, RID creation и serialization metadata.
- [Collision layers, material, gravity и sleeping baseline](physics/collision-material-state.md) - текущие layer/mask helpers, `PhysicsMaterial`, material override и внутренний body-state snapshot.
- [Area2D sensors и overlap signals baseline](physics/area2d-overlap-signals.md) - текущие overlap snapshots, `body_entered`/`body_exited`, `area_entered`/`area_exited`, фильтры и deferred removal.
- [PhysicsDirectSpaceState2D raycast, point query и shape query baseline](physics/direct-space-state-queries.md) - текущие `World2D`, direct state query parameters, `RayCast2D` execution и AABB query results.
- [Fixed physics timestep, basic CCD и one-way platform baseline](physics/fixed-physics-step-and-rigid-body-motion.md) - текущий fixed tick `1/60`, базовое движение `RigidBody2D`, AABB sweep, one-way collision и deferred body queue.
- [CharacterBody2D kinematic movement baseline](physics/character-body-2d-kinematic-solver.md) - текущие `MoveAndCollide()`, `MoveAndSlide()`, floor/wall/ceiling state, floor snap, platform velocity и `KinematicCollision2D`.
- [Debug collision shapes baseline](physics/debug-collision-shapes.md) - текущие `SceneTree.DebugCollisionsHint`, `CollisionShape2D.DebugColor` и внутренний снимок форм для editor viewport и diagnostics checks.

## Анимация

- [SpriteFrames и AnimatedSprite2D baseline](animation/spriteframes-animatedsprite2d.md) - текущие `SpriteFrames`, `AnimatedSprite2D`, frame timing, loop modes, autoplay и canvas submission текущего frame.
- [Animation, AnimationLibrary и AnimationPlayer baseline](animation/animation-player-tracks.md) - текущие value tracks, method call tracks, queue playback, completion signal и применение property values через `NodePath`.
- [Tween baseline](animation/tween-baseline.md) - текущие property/callback/interval tweeners, easing, pause/resume, stop/kill, manual step и completion signals.

## Звук

- [AudioServer и внутренние voice handles](audio/audio-server-voice-handles.md) - текущий public `AudioServer`, bus queries, internal voice lifecycle и backend boundary без раскрытия backend handles.
- [AudioStreamPlayer и AudioStreamPlayer2D](audio/audio-stream-player-nodes.md) - текущие playback nodes, pause/resume, polyphony, loop metadata, volume/pitch и 2D attenuation/panning.
- [Audio bus routing](audio/audio-bus-routing.md) - текущие `Master`, пользовательские buses, volume routing, mute, solo и общая громкость через `Master`.

## Рендеринг

- [`RenderingServer` и renderer profiles](rendering/rendering-server.md) - текущий server boundary, renderer profile и feature flags.
- [GPU lifecycle baseline](rendering/sdl-gpu-lifecycle.md) - текущий internal lifecycle adapter для graphics device/window/frame state machine.
- [Android mobile graphics smoke и fallback policy baseline](rendering/android-mobile-gpu-fallback-policy.md) - текущий internal Android mobile create profile, smoke steps, `Automatic`/`FailIfUnavailable` policy и structured startup result.
- [CanvasItem render queue baseline](rendering/canvas-item-render-queue.md) - текущая internal сортировка canvas item команд и contiguous batching.
- [Texture2D resource baseline](rendering/texture-resource-baseline.md) - текущие public `Texture2D`/`AtlasTexture` и internal texture lifetime registry.
- [Canvas node submission baseline](rendering/canvas-node-submission-baseline.md) - текущие public `CanvasItem`/`Node2D`/`Sprite2D`/`CanvasLayer` и internal sprite submission model.
- [Camera2D, Viewport and presentation baseline](rendering/camera-viewport-presentation-baseline.md) - текущие public `Camera2D`/`Viewport`, camera transform, pixel snapping и internal presentation plan.
- [Offscreen render target и восстановление GPU resources](rendering/offscreen-render-target-recovery-baseline.md) - текущие public `ViewportTexture`, `Viewport.GetTexture()` и внутреннее восстановление active texture resources после пересоздания device.
- [Canvas shaders import и diagnostics baseline](rendering/canvas-shader-import-baseline.md) - текущие public `Shader`, import-time vertex/fragment compilation через internal shader translation boundary и diagnostics с file/line/column.
- [ShaderMaterial, uniforms, samplers и canvas built-ins baseline](rendering/shader-material-baseline.md) - текущие public `Material`/`ShaderMaterial`, supported uniforms, texture samplers, serializable snapshot и reserved canvas built-ins.
- [Compatibility renderer backend baseline](rendering/sdl-renderer-compatibility-backend.md) - текущий internal fallback command plan для sprites/UI/text/primitives/tile-like texture copies и documented limitations.
- [Immediate drawing baseline](rendering/immediate-drawing-baseline.md) - текущие `_Draw()`, `QueueRedraw()`, `DrawLine()`/`DrawRect()`/`DrawCircle()`/`DrawPolygon()`/`DrawTexture()`/`DrawString()` и internal command capture.
- [Text backend baseline](rendering/text-backend-baseline.md) - текущие public `Font`/`Control`/`Label`, glyph layout, fallback font resolution, internal cache и text backend boundary.
- [TileMapLayer runtime API](rendering/tilemap-layer-runtime-api.md) - текущие `TileSet`, `TileSetAtlasSource`, `TileData`, `TileMapLayer`, canvas submission и tile collision baseline.

## Базовые типы

- [2D math baseline](core-types/2d-math-baseline.md) - текущие `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`.
- [RandomNumberGenerator](core-types/random-number-generator.md) - текущий deterministic RNG с `Seed`, `State`, range API и deterministic sequence tests.
- [`StringName` и `Rid`](core-types/stringname-rid.md) - текущие identity-типы для имён и низкоуровневых resource handles.
- [`Variant`](core-types/variant.md) - текущий closed-list value carrier и Electron2D коллекции `Array`/`Dictionary`.
- [Stable `Variant` serialization](core-types/variant-serialization.md) - текущий internal canonical JSON round-trip для сериализуемого Variant subset.

## Объектная модель

- [Базовые типы Object, RefCounted, Resource](object-model/base-object-lifetime.md) - текущий baseline public API и правила lifetime.
- [Node и SceneTree lifecycle](object-model/node-scene-tree-lifecycle.md) - текущий baseline lifecycle callbacks и порядок обхода.
- [Иерархия Node, ownership и безопасное удаление](object-model/node-hierarchy-ownership.md) - текущий parent-child API, `Owner`, `Reparent()` и `QueueFree()`.
- [`NodePath` и разрешение node paths](object-model/node-path-resolution.md) - текущий relative/absolute lookup через `GetNode()` и `GetNodeOrNull()`.
- [Группы Node и group calls](object-model/node-groups.md) - текущие group membership, persistent metadata и `SceneTree.CallGroup()`.
- [Сигналы, Callable и emission semantics](object-model/signals-callable.md) - текущие user signals, `Connect()`, `Disconnect()`, `EmitSignal()` и `Callable`.
- [Deferred calls и безопасный traversal](object-model/deferred-calls-safe-traversal.md) - текущие `Object.CallDeferred()`, `Callable.CallDeferred()`, deferred queue и безопасное изменение дерева во время обхода.
- [`PackedScene` и смена активной сцены](object-model/packed-scene.md) - текущие `Pack()`, `Instantiate()`, owned subtree snapshot и `SceneTree.ChangeSceneToPacked()`.
- [Runtime diagnostics пользовательского кода](object-model/runtime-diagnostics.md) - внутренняя диагностика и правило продолжения работы после ошибок в lifecycle, group calls, deferred calls и signals.
