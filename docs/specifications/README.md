# Спецификации Electron2D

Этот раздел содержит целевые спецификации: они описывают, каким должен стать движок, релизы и архитектурные границы. Описание уже реализованного поведения должно жить в `docs/documentation/`.

## Архитектура

- [Архитектура и платформенный стек Electron2D](architecture/engine-platform-stack.md) - стек SDL3-CS, SDL_GPU, fallback-рендеринга, физики, аудио, текста, geometry и сетевой основы.
- [AI-friendly workflow Electron2D 0.1](architecture/ai-friendly-workflow.md) - архитектурный контракт CLI/MCP/tooling, текстовых форматов, диагностики, автоматизированного запуска и AI benchmark.

## Релизы

- [Electron2D 0.1.0 Preview](releases/0.1.0-preview.md) - контракт первого вертикального среза: runtime, редактор, экспорт, примеры, критерии качества и явные исключения.

## Релизное управление

- [Тестовая инфраструктура 0.1.0 Preview](release-management/test-infrastructure.md) - unit, integration, runtime smoke и golden-data проверки после clean reset.
- [CI-матрица 0.1.0 Preview](release-management/ci-matrix.md) - desktop matrix, test runner и явная отметка mobile/export gap.
- [Таблица совместимости Godot-like API](release-management/api-compatibility.md) - GitHub Wiki source и verifier для public API surface.
- [Версионирование и release metadata 0.1.0 Preview](release-management/release-metadata.md) - package metadata, changelog и release notes.
- [Формат проекта и шаблон electron2d-empty](release-management/project-template.md) - минимальный project manifest, scene manifest и проверка build/run.
- [Performance budgets и soak-критерии 0.1.0 Preview](release-management/performance-budgets.md) - целевые устройства, 60 FPS, memory budgets и mobile cycles.

## Репозиторий

- [Политика лицензирования исходного кода](repository/license-policy.md) - MIT License, source headers и verifier для вручную написанного исходного кода.

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
- [Script metadata: `[Export]`, `[Signal]`, `[Tool]`](scripting/script-metadata.md) - публичные Godot-like marker attributes и internal AOT-safe bridge для serialization/Inspector.

## Ввод

- [SDL input event mapping и Godot-like `InputEvent*`](input/sdl-input-event-mapping.md) - keyboard, mouse button, mouse motion, wheel, text input и порядок dispatch через `SceneTree`.

## Физика 2D

- [PhysicsServer2D boundary](physics/physics-server-2d.md) - Godot-like public `Rid`-граница, internal swappable backend и запрет публичных Box2D handles.
- [Box2D.NET platform/AOT validation](physics/box2d-net-validation.md) - candidate smoke gate, desktop JIT/NativeAOT matrix, mobile Release/AOT gaps и allocations per tick.

## Рендеринг

- [`RenderingServer` и renderer profiles](rendering/rendering-server.md) - серверная граница, `Standard`/`Compatibility` профили и feature flags.
- [SDL_GPU lifecycle baseline](rendering/sdl-gpu-lifecycle.md) - internal SDL3-CS device lifecycle, window claim, command buffer submit и диагностические smoke-тесты.
- [CanvasItem render queue baseline](rendering/canvas-item-render-queue.md) - internal stable sort, visibility/modulate и contiguous batching для будущих `CanvasItem` submissions.
- [Texture2D resource baseline](rendering/texture-resource-baseline.md) - public `Texture2D`/`AtlasTexture`, internal texture upload/reload/release registry и leak tracking.
- [Canvas node submission baseline](rendering/canvas-node-submission-baseline.md) - public `CanvasItem`/`Node2D`/`Sprite2D`/`CanvasLayer` subset и internal sprite submission model.
- [Camera2D, Viewport and presentation baseline](rendering/camera-viewport-presentation-baseline.md) - public camera/viewport subset, pixel snapping and internal presentation scaling plan.
- [Immediate drawing baseline](rendering/immediate-drawing-baseline.md) - public CanvasItem custom drawing callback, `QueueRedraw()` и immediate `Draw*` command capture.
- [Text backend baseline через SDL_ttf](rendering/text-backend-baseline.md) - public `Font`/`Control`/`Label`, glyph layout, fallback font resolution и internal text layout cache.
- [Offscreen render target и восстановление GPU resources](rendering/offscreen-render-target-recovery-baseline.md) - public `ViewportTexture`, internal render target descriptors и восстановление active GPU resources после device recreation.
- [Canvas shaders import и diagnostics baseline](rendering/canvas-shader-import-baseline.md) - public `Shader`, import-time vertex/fragment stage compilation через SDL_shadercross boundary и diagnostics с file/line/column.
- [ShaderMaterial, uniforms, samplers и canvas built-ins baseline](rendering/shader-material-baseline.md) - public `Material`/`ShaderMaterial`, serializable uniforms, texture samplers и reserved canvas built-ins.
- [SDL_Renderer Compatibility backend baseline](rendering/sdl-renderer-compatibility-backend.md) - внутреннее преобразование `CanvasItemRenderPlan` в SDL_Renderer-compatible frame plan, supported features, limitations и golden reference scene.
- [Android mobile GPU smoke и fallback policy baseline](rendering/android-mobile-gpu-fallback-policy.md) - internal SDL_GPU mobile create profile, smoke steps, automatic/fail policy и structured startup result.

## Базовые типы

- [2D math baseline](core-types/2d-math-baseline.md) - целевой контракт `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`.
- [RandomNumberGenerator](core-types/random-number-generator.md) - Godot-like RNG с документированным seed, state и воспроизводимой PCG32 последовательностью.
- [`StringName` и `Rid`](core-types/stringname-rid.md) - immutable interned names и opaque resource identifiers для будущих серверных abstractions.
- [`Variant`](core-types/variant.md) - закрытый список Godot-like Variant-значений для `0.1.0 Preview`.
- [Stable `Variant` serialization](core-types/variant-serialization.md) - canonical JSON round-trip для сериализуемого Variant subset.

## Объектная модель

- [Базовые типы Object, RefCounted, Resource](object-model/base-object-lifetime.md) - начальный Godot-like runtime API и правила lifetime.
- [Node и SceneTree lifecycle](object-model/node-scene-tree-lifecycle.md) - начальный lifecycle baseline и порядок callbacks.
- [Иерархия Node, ownership и безопасное удаление](object-model/node-hierarchy-ownership.md) - parent-child инварианты, `Owner`, reparent/move и `QueueFree()`.
- [`NodePath` и разрешение node paths](object-model/node-path-resolution.md) - relative/absolute path resolution, `GetNode()` и `GetNodeOrNull()`.
- [Группы Node и group calls](object-model/node-groups.md) - group membership, persistent metadata и `SceneTree` group queries/calls.
- [Сигналы, Callable и emission semantics](object-model/signals-callable.md) - user signals, `Connect()`, `Disconnect()`, `EmitSignal()` и `Callable`.
- [Deferred calls и безопасное изменение дерева](object-model/deferred-calls-safe-traversal.md) - `CallDeferred()`, deferred queue и безопасный traversal при изменении дерева.
- [`PackedScene` и смена активной сцены](object-model/packed-scene.md) - pack/instantiate owned subtree и `SceneTree.ChangeSceneToPacked()` baseline.
- [Runtime diagnostics пользовательского кода](object-model/runtime-diagnostics.md) - внутренняя диагностика для lifecycle, signals, deferred calls и group calls.
