# Документация Electron2D

`docs/` теперь является корнем доменных документов. Внутри него нет отдельного слоя `specifications` или `documentation`: каждый файл описывает конкретную вещь и объединяет контракт, фактическое состояние, ограничения и проверки.

Рабочий пайплайн для любого домена:

1. Обновить или создать доменный документ в `docs/<domain>/`.
2. Добавить красные тесты, которые фиксируют ожидаемое поведение.
3. Реализовать изменение в коде.
4. Проверить зеленые тесты.
5. Если реализация уточнила поведение, ограничения или проверки, обновить тот же документ.

## Индекс доменов

## Анимация

- [Animation, AnimationLibrary и AnimationPlayer baseline](animation/animation-player-tracks.md)
- [SpriteFrames и AnimatedSprite2D baseline](animation/spriteframes-animatedsprite2d.md)
- [Tween baseline](animation/tween-baseline.md)

## Архитектура

- [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](architecture/agent-native-workflow.md)
- [Архитектура и платформенный стек Electron2D](architecture/engine-platform-stack.md)
- [Source domain layout](architecture/source-domain-layout.md)

## Звук

- [Audio bus routing](audio/audio-bus-routing.md)
- [AudioServer и внутренние voice handles](audio/audio-server-voice-handles.md)
- [AudioStreamPlayer и AudioStreamPlayer2D](audio/audio-stream-player-nodes.md)

## CLI

- [`e2d` CLI для headless, CI и active Editor routing](cli/e2d-cli.md)

## Базовые типы

- [2D math baseline](core-types/2d-math-baseline.md)
- [RandomNumberGenerator](core-types/random-number-generator.md)
- [`StringName` и `Rid`](core-types/stringname-rid.md)
- [Stable `Variant` serialization](core-types/variant-serialization.md)
- [`Variant`](core-types/variant.md)

## Диагностика

- [Diagnostics adapters: JSON, stream и SARIF](diagnostics/diagnostics-adapters.md)
- [Diagnostics.Core](diagnostics/diagnostics-core.md)

## Документация

- [Machine-readable API manifest](documentation/api-manifest.md)
- [Canonical goal alignment audit](documentation/canonical-goal-alignment.md)
- [GitHub community profile репозитория](documentation/github-community-profile.md)
- [GitHub Wiki API reference](documentation/github-wiki-api-reference.md)
- [Local documentation pipeline](documentation/local-documentation-pipeline.md)
- [Repository README и публичная входная страница](documentation/repository-readme.md)
- [XML documentation публичного API](documentation/public-api-xml-documentation.md)
- [Документация renderer profiles](documentation/renderer-profiles.md)
- [Troubleshooting guide и release checklist](documentation/troubleshooting-release-checklist.md)
- [Пользовательская документация 0.1.0 Preview](documentation/user-guide.md)

## Редактор

- [Agent process bootstrap из Editor](editor/agent-process-bootstrap.md)
- [Agent Workspace panel редактора](editor/agent-workspace-panel.md)
- [Electron2D.Editor project shell](editor/editor-project-shell.md)
- [Editor shell layout и visual harness](editor/editor-shell-layout.md)
- [FileSystem dock редактора](editor/file-system-dock.md)
- [Референс интерфейса редактора Godot 4](editor/godot4-editor-reference.md)
- [Inspector редактора](editor/inspector.md)
- [Project Manager редактора](editor/project-manager.md)
- [Project Settings UI редактора](editor/project-settings-ui.md)
- [Project Tasks board редактора](editor/project-tasks-board.md)
- [Run/output workflow редактора](editor/run-output-workflow.md)
- [Scene Tree dock редактора](editor/scene-tree-dock.md)
- [Script workspace редактора](editor/script-workspace.md)
- [Specialized editors: SpriteFrames, TileMap и AnimationPlayer](editor/specialized-editors.md)
- [2D Viewport редактора](editor/viewport-2d.md)

## Примеры и reference games

- [Ассеты reference games 0.1.0 Preview](examples/reference-game-assets.md)
- [Reference game platform matrix 0.1.0 Preview](examples/reference-game-platform-matrix.md)
- [Reference platformer 0.1.0 Preview](examples/reference-platformer.md)
- [UI-heavy reference game 0.1.0 Preview](examples/ui-heavy-reference.md)

## Export pipeline и платформы

- [Android arm64 export](export/android-arm64-export.md)
- [Export user documentation](export/export-guide.md)
- [Export preset model and toolchain validation](export/export-preset-model.md)
- [iOS arm64 export](export/ios-arm64-export.md)
- [Linux x64 glibc export](export/linux-x64-export.md)
- [macOS arm64 export](export/macos-arm64-export.md)
- [WebAssembly browser export](export/webassembly-browser-export.md)
- [Windows x64 export](export/windows-x64-export.md)

## Ввод

- [Gamepad input baseline](input/gamepad-input.md)
- [Input dispatch, UI focus и mouse filter baseline](input/input-dispatch-ui-focus.md)
- [InputMap, action state и persistence baseline](input/input-map-actions.md)
- [Mobile input baseline](input/mobile-input.md)
- [Platform input event mapping и Electron2D `InputEvent*`](input/sdl-input-event-mapping.md)

## MCP

- [Локальный MCP-сервер поверх active Editor session и Tooling](mcp/mcp-server.md)

## Объектная модель

- [Базовые типы `Object`, `RefCounted`, `Resource`](object-model/base-object-lifetime.md)
- [Deferred calls и безопасное изменение дерева во время обхода](object-model/deferred-calls-safe-traversal.md)
- [Группы `Node` и group calls](object-model/node-groups.md)
- [Иерархия `Node`, ownership и безопасное удаление](object-model/node-hierarchy-ownership.md)
- [`NodePath` и разрешение node paths](object-model/node-path-resolution.md)
- [`Node` и `SceneTree`: lifecycle baseline](object-model/node-scene-tree-lifecycle.md)
- [`PackedScene` и смена активной сцены](object-model/packed-scene.md)
- [Runtime diagnostics пользовательского кода](object-model/runtime-diagnostics.md)
- [Сигналы, `Callable` и emission semantics](object-model/signals-callable.md)

## Физика 2D

- [Area2D sensors и overlap signals baseline](physics/area2d-overlap-signals.md)
- [Box2D.NET platform/AOT validation](physics/box2d-net-validation.md)
- [CharacterBody2D kinematic solver baseline](physics/character-body-2d-kinematic-solver.md)
- [Collision layers, material, gravity и sleeping baseline](physics/collision-material-state.md)
- [Debug collision shapes baseline](physics/debug-collision-shapes.md)
- [PhysicsDirectSpaceState2D raycast, point query и shape query baseline](physics/direct-space-state-queries.md)
- [Fixed physics timestep, basic CCD и one-way platform baseline](physics/fixed-physics-step-and-rigid-body-motion.md)
- [Physics nodes lifecycle baseline](physics/physics-nodes-lifecycle.md)
- [PhysicsServer2D boundary](physics/physics-server-2d.md)
- [Shape2D resources baseline](physics/shape2d-resources.md)

## Project system

- [Canonical document model, revision model и structural diff](project-system/canonical-document-model.md)
- [Human-AI concurrent editing, conflicts и grouped Undo](project-system/concurrent-editing-and-undo.md)
- [External Change Synchronizer](project-system/external-change-synchronizer.md)
- [Live ProjectWorkspace](project-system/live-project-workspace.md)
- [ProjectTaskManager, TaskActivity и task storage](project-system/project-task-manager.md)
- [Markdown report export для Project Tasks](project-system/project-tasks-markdown-report-export.md)
- [Stable project text formats, migrations и JSON Schema](project-system/project-text-formats.md)
- [Reproducibility lock и `e2d doctor`](project-system/reproducibility-lock-and-doctor.md)
- [Статический context pack проекта](project-system/static-context-pack.md)
- [WorkspaceJob contract и event stream](project-system/workspace-jobs.md)
- [WorkspaceSnapshot, job input identity и dirty export policy](project-system/workspace-snapshot.md)
- [WorkspaceTransactionEngine и безопасные project operations](project-system/workspace-transactions.md)

## Качество

- [Leak verification для `0.1.0 Preview`](quality/leak-verification.md)
- [Performance verification для `0.1.0 Preview`](quality/performance-verification.md)

## Релизное управление

- [Таблица совместимости Electron2D API](release-management/api-compatibility.md)
- [CI-матрица `0.1.0 Preview`](release-management/ci-matrix.md)
- [Performance budgets и soak-критерии `0.1.0 Preview`](release-management/performance-budgets.md)
- [Формат проекта и шаблон `electron2d-empty`](release-management/project-template.md)
- [Release packaging и GitHub Release rehearsal](release-management/release-packaging.md)
- [Версионирование и release metadata `0.1.0 Preview`](release-management/release-metadata.md)
- [Тестовая инфраструктура `0.1.0 Preview`](release-management/test-infrastructure.md)

## Релизы

- [Electron2D 0.1.0 Preview](releases/0.1.0-preview.md)

## Рендеринг

- [Android mobile GPU smoke и fallback policy baseline](rendering/android-mobile-gpu-fallback-policy.md)
- [Camera2D, Viewport and presentation baseline](rendering/camera-viewport-presentation-baseline.md)
- [CanvasItem render queue baseline](rendering/canvas-item-render-queue.md)
- [Canvas node submission baseline](rendering/canvas-node-submission-baseline.md)
- [Canvas shaders import и diagnostics baseline](rendering/canvas-shader-import-baseline.md)
- [Immediate drawing baseline](rendering/immediate-drawing-baseline.md)
- [Offscreen render target и восстановление GPU resources](rendering/offscreen-render-target-recovery-baseline.md)
- [`RenderingServer` и renderer profiles](rendering/rendering-server.md)
- [SDL_GPU lifecycle baseline](rendering/sdl-gpu-lifecycle.md)
- [SDL_Renderer Compatibility backend baseline](rendering/sdl-renderer-compatibility-backend.md)
- [ShaderMaterial, uniforms, samplers и canvas built-ins baseline](rendering/shader-material-baseline.md)
- [Text backend baseline](rendering/text-backend-baseline.md)
- [Texture2D resource baseline](rendering/texture-resource-baseline.md)
- [TileMapLayer runtime API](rendering/tilemap-layer-runtime-api.md)

## Репозиторий

- [Политика лицензирования исходного кода](repository/license-policy.md)
- [Раскладка репозитория и локальных рабочих материалов](repository/repository-layout.md)

## Ресурсы, импорт и сериализация

- [AOT-safe metadata для Inspector и serialization](resources/aot-safe-metadata.md)
- [Импорт WAV/OGG в AudioStream](resources/audio-stream-import.md)
- [Stress data stability для scene/resource pipeline](resources/data-stability-stress.md)
- [Импорт TTF/OTF в Font](resources/font-import.md)
- [Resource file baseline, stable UID и ссылки ресурсов](resources/resource-file-baseline.md)
- [Import cache ресурсов](resources/resource-import-cache.md)
- [Runtime resource loader](resources/runtime-resource-loader.md)
- [Сериализация сцен, ресурсов и переносимых property values](resources/scene-resource-serialization.md)
- [Импорт shader source в platform-specific artifacts](resources/shader-source-import.md)
- [Импорт PNG/JPEG в Texture2D и AtlasTexture](resources/texture-image-import.md)

## Runtime

- [Editor-attached runtime control](runtime/editor-attached-runtime-control.md)
- [Headless runtime automation](runtime/headless-runtime-automation.md)
- [Project runtime runner](runtime/project-runtime-runner.md)
- [Runtime debug bridge и scene inspection](runtime/runtime-debug-bridge.md)

## C# scripting

- [C# script classes, inheritance from `Node` и lifecycle](scripting/csharp-script-classes.md)
- [C# language services в Script workspace](scripting/editor-language-services.md)
- [Script workspace и встроенная C# IDE](scripting/editor-script-workflow.md)
- [Выбор managed .NET debug adapter](scripting/managed-debug-adapter-selection.md)
- [Managed C# debugger в редакторе](scripting/managed-debugger.md)
- [Script/Debugger Tooling parity](scripting/script-debug-tooling-parity.md)
- [Script metadata: `[Export]`, `[Signal]`, `[Tool]`](scripting/script-metadata.md)
- [Безопасное editor-time выполнение `[Tool]` scripts](scripting/tool-script-execution.md)

## Локализация и настройки

- [Settings persistence baseline](settings-localization/settings-persistence.md)
- [Translation resource, locale switching и `Tr`](settings-localization/translation-runtime.md)
- [Unicode, IME и текст справа налево](settings-localization/unicode-ime-rtl-text.md)

## Тестирование

- [Agent acceptance benchmarks для Electron2D 0.1](testing/agent-acceptance-benchmarks.md)
- [Scene tests и visual regression tests](testing/scene-visual-testing.md)

## Tooling

- [Editor Capability Manifest](tooling/editor-capability-manifest.md)
- [Editor session discovery и Editor-hosted Agent Gateway](tooling/editor-session-discovery.md)
- [Electron2D.Tooling service boundary](tooling/tooling-service-boundary.md)

## UI

- [Спецификация: базовые UI controls](ui/basic-controls.md)
- [UI containers](ui/containers.md)
- [Control layout core](ui/control-layout-core.md)
- [UI public API gate](ui/public-api-gate.md)
- [Спецификация: структурные UI controls](ui/structured-controls.md)
- [Спецификация: темы, DPI scale и tooltips UI](ui/theme-tooltips.md)
