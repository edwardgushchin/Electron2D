# Спецификации Electron2D

Этот раздел содержит целевые спецификации: они описывают, каким должен стать движок, релизы и архитектурные границы. Описание уже реализованного поведения должно жить в `docs/documentation/`.

## Архитектура

- [Архитектура и платформенный стек Electron2D](architecture/engine-platform-stack.md) - стек платформенных backend, fallback-рендеринга, физики, аудио, текста, geometry и сетевой основы.
- [AI-friendly workflow Electron2D 0.1](architecture/ai-friendly-workflow.md) - архитектурный контракт живой Editor-сессии, `ProjectWorkspace`, MCP/tooling, синхронизации изменений, диагностики, управления запущенной игрой и AI benchmark.
- [Source domain layout](architecture/source-domain-layout.md) - правило, какие исходники остаются в `Core`, какие живут отдельными доменами и почему папки не задают namespace.

## Релизы

- [Electron2D 0.1.0 Preview](releases/0.1.0-preview.md) - контракт первого вертикального среза: runtime, редактор, экспорт, примеры, критерии качества и явные исключения.

## Релизное управление

- [Тестовая инфраструктура 0.1.0 Preview](release-management/test-infrastructure.md) - unit, integration, runtime smoke и golden-data проверки после clean reset.
- [CI-матрица 0.1.0 Preview](release-management/ci-matrix.md) - desktop matrix, test runner и явная отметка mobile/export gap.
- [Таблица совместимости API](release-management/api-compatibility.md) - GitHub Wiki repository и verifier для public API surface.
- [Версионирование и release metadata 0.1.0 Preview](release-management/release-metadata.md) - package metadata, changelog и release notes.
- [Формат проекта и шаблон electron2d-empty](release-management/project-template.md) - минимальный project manifest, scene manifest и проверка build/run.
- [Performance budgets и soak-критерии 0.1.0 Preview](release-management/performance-budgets.md) - целевые устройства, 60 FPS, memory budgets и mobile cycles.

## Документация

- [Пользовательская документация 0.1.0 Preview](documentation/user-documentation.md) - обязательные разделы user guide, проверяемые команды и screenshot policy.
- [Документация renderer profiles](documentation/renderer-profiles-user-documentation.md) - обязательная пользовательская справка по renderer profiles, feature flags, Android fallback и `fail_if_unavailable`.
- [Troubleshooting guide и release checklist](documentation/troubleshooting-release-checklist.md) - обязательные разделы troubleshooting и preview release checklist для пользовательской документации.
- [XML documentation публичного API](documentation/public-api-xml-documentation.md) - обязательные XML-теги, verifier modes и правила качества public API comments.
- [GitHub Wiki API reference](documentation/github-wiki-api-reference.md) - автоматическая генерация GitHub Wiki из compiled public API и XML documentation.
- [Machine-readable API manifest](documentation/api-manifest.md) - JSON manifest публичного API, stable identifiers, compatibility status и проверка синхронизации с compiled assembly, XML docs и GitHub Wiki.
- [Local documentation pipeline](documentation/local-documentation-pipeline.md) - локальный документационный индекс и команды `e2d docs` поверх согласованных Wiki/XML/API manifest источников.
- [Canonical goal alignment audit](documentation/canonical-goal-alignment.md) - проверка, что старые goal/architecture материалы не возвращают устаревшее component-first или four-platform позиционирование как актуальный контракт.

## CLI

- [`e2d` CLI для headless, CI и active Editor routing](cli/e2d-cli.md) - общий parser/result envelope, common flags, generic workspace transaction, job JSONL и routing через active Editor session.

## Runtime

- [Headless runtime automation](runtime/headless-runtime-automation.md) - CLI-запуск сцены без ручного Editor UI, fixed frame loop, input trace, runtime artifacts, JSON schemas и snapshot identity для CI и автономных агентов.

## Тестирование

- [Scene tests и visual regression tests](testing/scene-visual-testing.md) - project scene test suite manifest, deterministic frame advance, node/property assertions, visual comparison artifacts, JSON schemas и CLI `e2d test --format json`.

## MCP

- [Локальный MCP-сервер поверх active Editor session и Tooling](mcp/mcp-server.md) - resources/tools manifest, route semantics, Tooling adapter rules, task guard, job events и fail-closed security для локального MCP adapter-а.

## Репозиторий

- [Политика лицензирования исходного кода](repository/license-policy.md) - MIT License, source headers и verifier для вручную написанного исходного кода.
- [Раскладка репозитория и локальных рабочих материалов](repository/repository-layout.md) - local-only task/diary/release logs и поставляемые данные в `data/`.

## Project system

- [Canonical document model, revision model и structural diff](project-system/canonical-document-model.md) - внутренний контракт identity, классификации, ревизий, parser/serializer boundary и structural diff до `ProjectWorkspace`.
- [Stable project text formats, migrations и JSON Schema](project-system/project-text-formats.md) - deterministic formatting, validation, migration pipeline и JSON Schema для scene/resource/project JSON source files.
- [Live ProjectWorkspace](project-system/live-project-workspace.md) - внутренняя live model проекта: document store, revisions, dirty state, events, ownership lease, operation journal и diagnostics store.
- [WorkspaceSnapshot, job input identity и dirty export policy](project-system/workspace-snapshot.md) - immutable snapshot для build/test/run/export artifacts, materialization, stale rules и export dirty snapshot policy.
- [WorkspaceJob contract и event stream](project-system/workspace-jobs.md) - внутренний контракт долгих import/build/test/export/run операций, lifecycle states, progress, cancel, diagnostics, artifacts и stale markers.
- [WorkspaceTransactionEngine и безопасные project operations](project-system/workspace-transactions.md) - внутренний контракт транзакционных project operations: dry-run, revisions, save/headless/external import modes, atomic write, conflicts и grouped undo.
- [ProjectTaskManager, TaskActivity и task storage](project-system/project-task-manager.md) - встроенная модель задач пользовательского проекта, activity, human acceptance guard, dependency graph, stable task documents и transaction integration.

## Diagnostics

- [Diagnostics.Core](diagnostics/diagnostics-core.md) - внутренний контракт structured diagnostics, stable error code registry, locations, suggested fixes и deterministic JSON serialization.

## Tooling

- [Electron2D.Tooling service boundary](tooling/tooling-service-boundary.md) - общий слой семантических операций над `ProjectWorkspace`, единый operation result, task commands, job-backed long operations и запрет обхода transaction/task/job contracts.
- [Editor session discovery и Editor-hosted Agent Gateway](tooling/editor-session-discovery.md) - registry, lease/heartbeat, локальные endpoint-ы, project-root verification, read-only второй Editor и headless fallback для CLI/MCP adapter-ов.

## Ресурсы, импорт и сериализация

- [Resource file baseline, stable UID и ссылки ресурсов](resources/resource-file-baseline.md) - `ResourceUid`, `.e2res`, external/internal references и diff-friendly формат ресурсов.
- [Import cache ресурсов](resources/resource-import-cache.md) - внутренний pipeline для discovery, reimport on change, dependency tracking, safe cache writes и prune unused cache.
- [Импорт PNG/JPEG в Texture2D и AtlasTexture](resources/texture-image-import.md) - internal image importer, sidecar настройки, texture metadata, atlas regions и platform variants.
- [Импорт TTF/OTF в Font](resources/font-import.md) - internal font importer, fallback font dependencies, SDF/bitmap policy и stable font metadata cache.
- [Импорт shader source в platform-specific artifacts](resources/shader-source-import.md) - internal shader importer, compiled stages per target, diagnostics file/line/column и iOS/export precompiled artifact policy.
- [Сериализация сцен, ресурсов и переносимых property values](resources/scene-resource-serialization.md) - internal scene/resource documents, arrays, dictionaries, enums, nullable и resource reference slots.
- [AOT-safe metadata для Inspector и serialization](resources/aot-safe-metadata.md) - internal metadata registry, typed delegates и NativeAOT smoke для custom `Resource` serialization.
- [Stress data stability для scene/resource pipeline](resources/data-stability-stress.md) - 100 save/load cycles, rename/move UID stability, import cache rebuild и corruption diagnostics.

## C# scripting

- [C# script classes, inheritance from `Node` и lifecycle](scripting/csharp-script-classes.md) - обычная .NET C# модель script classes, lifecycle callbacks и доступ к сервисам движка.
- [Script metadata: `[Export]`, `[Signal]`, `[Tool]`](scripting/script-metadata.md) - публичные marker attributes и internal AOT-safe bridge для serialization/Inspector.
- [Безопасное editor-time выполнение `[Tool]` scripts](scripting/tool-script-execution.md) - внутренний execution host для registered tool metadata без dynamic assembly load.
- [Script workspace и встроенная C# IDE](scripting/editor-script-workflow.md) - центральный C# workspace, text editor, IntelliSense, build diagnostics, managed debugger и Tooling/MCP parity.

## Редактор

- [Референс интерфейса редактора Godot 4](editor/godot4-editor-reference.md) - Godot 4 как baseline layout, центральные workspaces `2D`/`Script`/`Game`/`Tasks`, docks, Agent Workspace и полный запрет 3D/GDScript UI.
- [Electron2D.Editor project shell](editor/editor-project-shell.md) - базовый executable project редактора, smoke-запуск на runtime Electron2D и запрет внешнего desktop UI framework.
- [Project Manager редактора](editor/project-manager.md) - создание и открытие проектов, recent projects, renderer profile и clean-machine SDK smoke workflow.
- [Scene Tree dock редактора](editor/scene-tree-dock.md) - add/delete/rename/duplicate/reparent/drag-and-drop, ownership и undo/redo для сохранённого дерева сцены.
- [2D Viewport редактора](editor/viewport-2d.md) - pan, zoom, select, move, rotate, scale, snapping, pivot, bounds, collision-shape overlays и camera preview.
- [Inspector редактора](editor/inspector.md) - редактирование saved properties, export metadata, reset defaults, nested resources, serialization и undo/redo.
- [FileSystem dock редактора](editor/file-system-dock.md) - browse project files, folders, rename/move resources, reimport, search, drag resource into scene и visible import errors.
- [Run/output workflow редактора](editor/run-output-workflow.md) - запуск project/current scene, stop, output console, diagnostics и frame timing.

## Ввод

- [Input event mapping и `InputEvent*`](input/sdl-input-event-mapping.md) - keyboard, mouse button, mouse motion, wheel, text input и порядок dispatch через `SceneTree`.
- [InputMap, action state и persistence baseline](input/input-map-actions.md) - action registry, bindings, deadzone, action state, `GetVector()` и внутренний serializer input settings.
- [Input dispatch, UI focus и mouse filter baseline](input/input-dispatch-ui-focus.md) - порядок `_Input()`, `Control._GuiInput()`, handled-state, focus ownership и mouse filter.
- [Gamepad input baseline](input/gamepad-input.md) - connected devices, `InputEventJoypadButton`, `InputEventJoypadMotion`, action bindings, axis/button state и vibration API.
- [Mobile input baseline](input/mobile-input.md) - touch events, mobile navigation, virtual keyboard, orientation и display safe area state.

## UI

- [Control layout core](ui/control-layout-core.md) - anchors/offsets, minimum size, grow direction, clipping hit-test и focus navigation baseline.
- [UI containers](ui/containers.md) - `Container`, линейные, табличные, margin, center и scroll containers, size flags и theme constants для layout.
- [Базовые UI controls](ui/basic-controls.md) - `Panel`, кнопки, текстовый ввод, `Range`, `Slider`, `ProgressBar`, `TextureRect` и `NinePatchRect`.
- [Структурные UI controls](ui/structured-controls.md) - `ItemList`, `Tree`, `TreeItem`, `PopupMenu` и `TabContainer` для runtime UI и будущего редактора.
- [Темы, DPI scale и tooltips UI](ui/theme-tooltips.md) - `Theme`, style boxes, local theme overrides, fallback lookup, base scale и tooltip API.
- [UI public API gate](ui/public-api-gate.md) - release gate, который запрещает editor work, пока все UI/Text строки GitHub Wiki compatibility table не имеют статус `Supported`.

## Локализация и настройки

- [Translation resource, locale switching и `Tr`](settings-localization/translation-runtime.md) - resource переводов, общий translation server, locale lookup, fallback и обновление UI после смены локали.
- [Unicode, IME и текст справа налево](settings-localization/unicode-ime-rtl-text.md) - preview-level support Unicode scalar values, committed IME text input и mixed LTR/RTL/emoji UI layout.
- [Settings persistence baseline](settings-localization/settings-persistence.md) - project/user settings, input actions, display/window defaults и fail-closed diagnostics.

## Export pipeline и платформы

- [Export preset model and toolchain validation](export/export-preset-model.md) - общий preset JSON, target/configuration model, signing references без секретов и fail-closed diagnostics.
- [Windows x64 export](export/windows-x64-export.md) - self-contained `win-x64` package plan, локальный publish/run verifier и fail-closed validation.
- [Linux x64 glibc export](export/linux-x64-export.md) - self-contained `linux-x64` package plan, glibc-only scope, WSL/Linux verifier и fail-closed validation.
- [macOS arm64 export](export/macos-arm64-export.md) - self-contained `osx-arm64` `.app` bundle plan, x64 policy, user-provided signing plan и macOS verifier.

## Примеры и reference games

- [Ассеты reference games](examples/reference-game-assets.md) - локальный licensed asset pack, manifest, роли, hash-verification и запрет сетевых download steps во время build.

## Физика 2D

- [PhysicsServer2D boundary](physics/physics-server-2d.md) - public `Rid`-граница, internal swappable backend и запрет публичных physics backend handles.
- [Box2D.NET platform/AOT validation](physics/box2d-net-validation.md) - candidate smoke gate, desktop JIT/NativeAOT matrix, mobile Release/AOT gaps и allocations per tick.
- [Physics nodes lifecycle baseline](physics/physics-nodes-lifecycle.md) - `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D`, RID lifecycle и transform sync.
- [Shape2D resources baseline](physics/shape2d-resources.md) - concrete 2D collision shapes, validation, concave-only-static rule и AOT-safe serialization.
- [Collision layers, material, gravity и sleeping baseline](physics/collision-material-state.md) - collision filter helpers, `PhysicsMaterial`, material override и внутренний body-state snapshot.
- [Area2D sensors и overlap signals baseline](physics/area2d-overlap-signals.md) - `body_entered`/`body_exited`, `area_entered`/`area_exited`, overlap helpers, filters и deferred removal.
- [PhysicsDirectSpaceState2D raycast, point query и shape query baseline](physics/direct-space-state-queries.md) - `World2D`, direct state query parameters, `RayCast2D` execution и AABB query results.
- [Fixed physics timestep, basic CCD и one-way platform baseline](physics/fixed-physics-step-and-rigid-body-motion.md) - fixed tick `1/60`, базовое движение `RigidBody2D`, AABB sweep against `StaticBody2D`, one-way collision и deferred body queue.

## Анимация

- [SpriteFrames и AnimatedSprite2D baseline](animation/spriteframes-animatedsprite2d.md) - `SpriteFrames`, `AnimatedSprite2D`, frame timing, loop modes, autoplay и canvas submission текущего frame.
- [Animation, AnimationLibrary и AnimationPlayer baseline](animation/animation-player-tracks.md) - value tracks, method call tracks, queue playback, completion signal и применение property values через `NodePath`.
- [Tween baseline](animation/tween-baseline.md) - deterministic runtime tween processing, property/callback/interval tweeners, easing, pause/resume и cancellation semantics.

## Звук

- [AudioServer и внутренние voice handles](audio/audio-server-voice-handles.md) - public audio server queries, internal voice lifecycle и backend boundary без раскрытия backend handles.
- [AudioStreamPlayer и AudioStreamPlayer2D](audio/audio-stream-player-nodes.md) - playback nodes для непозиционного и 2D-звука, play/stop/pause, loop metadata, volume, pitch, attenuation и panning.
- [Audio bus routing](audio/audio-bus-routing.md) - пользовательские buses, routing path, mute, solo и общая громкость через `Master`.

## Рендеринг

- [`RenderingServer` и renderer profiles](rendering/rendering-server.md) - серверная граница, `Standard`/`Compatibility` профили и feature flags.
- [GPU lifecycle baseline](rendering/sdl-gpu-lifecycle.md) - internal graphics device lifecycle, window claim, command buffer submit и диагностические smoke-тесты.
- [CanvasItem render queue baseline](rendering/canvas-item-render-queue.md) - internal stable sort, visibility/modulate и contiguous batching для будущих `CanvasItem` submissions.
- [Texture2D resource baseline](rendering/texture-resource-baseline.md) - public `Texture2D`/`AtlasTexture`, internal texture upload/reload/release registry и leak tracking.
- [Canvas node submission baseline](rendering/canvas-node-submission-baseline.md) - public `CanvasItem`/`Node2D`/`Sprite2D`/`CanvasLayer` subset и internal sprite submission model.
- [Camera2D, Viewport and presentation baseline](rendering/camera-viewport-presentation-baseline.md) - public camera/viewport subset, pixel snapping and internal presentation scaling plan.
- [Immediate drawing baseline](rendering/immediate-drawing-baseline.md) - public CanvasItem custom drawing callback, `QueueRedraw()` и immediate `Draw*` command capture.
- [Text backend baseline](rendering/text-backend-baseline.md) - public `Font`/`Control`/`Label`, glyph layout, fallback font resolution и internal text layout cache.
- [Offscreen render target и восстановление GPU resources](rendering/offscreen-render-target-recovery-baseline.md) - public `ViewportTexture`, internal render target descriptors и восстановление active GPU resources после device recreation.
- [Canvas shaders import и diagnostics baseline](rendering/canvas-shader-import-baseline.md) - public `Shader`, import-time vertex/fragment stage compilation через internal shader translation boundary и diagnostics с file/line/column.
- [ShaderMaterial, uniforms, samplers и canvas built-ins baseline](rendering/shader-material-baseline.md) - public `Material`/`ShaderMaterial`, serializable uniforms, texture samplers и reserved canvas built-ins.
- [Compatibility renderer backend baseline](rendering/sdl-renderer-compatibility-backend.md) - внутреннее преобразование `CanvasItemRenderPlan` в compatibility frame plan, supported features, limitations и golden reference scene.
- [Android mobile GPU smoke и fallback policy baseline](rendering/android-mobile-gpu-fallback-policy.md) - internal mobile graphics create profile, smoke steps, automatic/fail policy и structured startup result.
- [TileMapLayer runtime API](rendering/tilemap-layer-runtime-api.md) - `TileSet`, atlas source, tile data, cell storage, canvas submission и tile collision для reference platformer.

## Базовые типы

- [2D math baseline](core-types/2d-math-baseline.md) - целевой контракт `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`.
- [RandomNumberGenerator](core-types/random-number-generator.md) - RNG с документированным seed, state и воспроизводимой PCG32 последовательностью.
- [`StringName` и `Rid`](core-types/stringname-rid.md) - immutable interned names и opaque resource identifiers для будущих серверных abstractions.
- [`Variant`](core-types/variant.md) - закрытый список Variant-значений для `0.1.0 Preview`.
- [Stable `Variant` serialization](core-types/variant-serialization.md) - canonical JSON round-trip для сериализуемого Variant subset.

## Объектная модель

- [Базовые типы Object, RefCounted, Resource](object-model/base-object-lifetime.md) - начальный runtime API и правила lifetime.
- [Node и SceneTree lifecycle](object-model/node-scene-tree-lifecycle.md) - начальный lifecycle baseline и порядок callbacks.
- [Иерархия Node, ownership и безопасное удаление](object-model/node-hierarchy-ownership.md) - parent-child инварианты, `Owner`, reparent/move и `QueueFree()`.
- [`NodePath` и разрешение node paths](object-model/node-path-resolution.md) - relative/absolute path resolution, `GetNode()` и `GetNodeOrNull()`.
- [Группы Node и group calls](object-model/node-groups.md) - group membership, persistent metadata и `SceneTree` group queries/calls.
- [Сигналы, Callable и emission semantics](object-model/signals-callable.md) - user signals, `Connect()`, `Disconnect()`, `EmitSignal()` и `Callable`.
- [Deferred calls и безопасное изменение дерева](object-model/deferred-calls-safe-traversal.md) - `CallDeferred()`, deferred queue и безопасный traversal при изменении дерева.
- [`PackedScene` и смена активной сцены](object-model/packed-scene.md) - pack/instantiate owned subtree и `SceneTree.ChangeSceneToPacked()` baseline.
- [Runtime diagnostics пользовательского кода](object-model/runtime-diagnostics.md) - внутренняя диагностика для lifecycle, signals, deferred calls и group calls.
