# Electron2D 0.1.0 Preview

Дата baseline: 2026-06-20.

`0.1.0 Preview` перезапущен как clean rewrite. Старая реализация `src/Electron2D/` удалена, а предыдущая работа сохранена в локальной ветке `private/pre-rewrite-backup-2026-06-20`.

## Что есть сейчас

- Новый пустой runtime-проект `Electron2D`.
- Начальный Godot-like public API: `Object`, `RefCounted`, `Resource`.
- Начальный lifecycle API: `Node`, `SceneTree`, `InputEvent`.
- Начальная иерархия `Node`: `Owner`, `GetParent()`, `GetChild()`, `MoveChild()`, `Reparent()` и `QueueFree()`.
- Начальное разрешение путей: `NodePath`, `GetNode()` и `GetNodeOrNull()`.
- Начальные группы: `AddToGroup()`, `RemoveFromGroup()`, `GetGroups()` и `SceneTree.CallGroup()`.
- Начальные сигналы: `Callable`, `Connect()`, `Disconnect()`, `EmitSignal()` и error-return semantics.
- Начальные deferred calls: `Object.CallDeferred()`, `Callable.CallDeferred()` и безопасный drain deferred queue после traversal.
- Начальные scene resources: `PackedScene`, in-memory `Pack()`/`Instantiate()` и `SceneTree.ChangeSceneToPacked()`.
- Начальная внутренняя runtime-диагностика: исключения из пользовательского кода сохраняют node context, callback, kind, message и stack trace без остановки обхода дерева, очереди или signal emission.
- Начальный 2D math API: `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`.
- Начальный RNG API: `RandomNumberGenerator` с документированными `Seed`, `State` и воспроизводимой PCG32 sequence policy.
- Начальный identity API: `StringName` и `Rid` для будущего `Variant` и server-backed resources.
- Начальный resource UID API: `ResourceUid`, `uid://` conversion, path mapping и сохранение ссылок при rename/move через `SetId()`.
- Начальный internal resource file baseline: `.e2res` JSON document model, external/internal references, стабильный LF output и golden-data проверка exact text.
- Начальный internal import cache baseline: `.e2res` discovery, cache root отдельно от source assets, reimport при изменении source/dependency, сохранение предыдущего валидного cache при ошибке и prune удалённых source assets.
- Начальный internal PNG/JPEG texture import baseline: image metadata, sidecar настройки filter/repeat/mipmaps, atlas regions, platform variants и stable `texture.e2tex.json` cache artifact.
- Начальный internal TTF/OTF font import baseline: font names metadata, fallback font dependencies, SDF/bitmap policy и stable `font.e2font.json` cache artifact.
- Начальный internal shader source artifact import baseline: `.e2shader`, sidecar target platforms, diagnostics file/line/column, stable `shader.e2shader.json` cache artifact и iOS artifact без runtime compilation.
- Начальный internal scene/resource serialization baseline: stable resource/scene JSON documents, custom `Resource` round-trip, arrays, dictionaries, enums, nullable и resource reference slots.
- Начальный internal AOT-safe metadata baseline: `ResourceObjectMetadataRegistry`, typed descriptors, custom `Resource` serialization без reflection fallback и trimmed/NativeAOT smoke verifier.
- Начальный data stability stress gate: 100 save/load cycles, rename/move resources, import cache rebuild и corruption diagnostics без silent data loss.
- Начальный C# script class baseline: обычные .NET classes, inheritance from `Node`, lifecycle callbacks, доступ к `GetTree()`/`RenderingServer` и script sample в `electron2d-empty`.
- Начальный script metadata baseline: публичные Godot-like `[Export]`, `[Signal]`, `[Tool]`, internal AOT-safe bridge для export properties/signals/tool-state, callable signals и sandboxed experimental `[Tool]`.
- Начальный SDL input event mapping baseline: public `InputEventKey`, `InputEventMouseButton`, `InputEventMouseMotion`, `Key`, `KeyLocation`, `MouseButton`, `MouseButtonMask`, keyboard/mouse/wheel/text input и dispatch order через `SceneTree`.
- Начальный `PhysicsServer2D` boundary baseline: public Godot-like RID-граница для spaces, areas, bodies, joints, shapes, `SpaceParameter`, `ShapeType`, `ProcessInfo`, `FreeRid()` и internal backend boundary без публичных Box2D handles.
- Начальный physics nodes lifecycle baseline: public Godot-like `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D`, `Shape2D`, RID lifecycle, transform sync и безопасное удаление через `QueueFree()` во время physics traversal.
- Начальный shape resources baseline: public Godot-like `RectangleShape2D`, `CircleShape2D`, `CapsuleShape2D`, `SegmentShape2D`, `ConvexPolygonShape2D`, `ConcavePolygonShape2D`, validation, shape RID creation, concave-only-static rule и AOT-safe serialization metadata.
- Начальный Box2D.NET candidate validation baseline: smoke-проект на `Box2D.NET 3.1.654`, Release/JIT и NativeAOT verifier, allocations per tick measurement, desktop CI gate и documented Android/iOS Release/AOT gaps.
- Начальный Variant API: `Variant`, `Variant.Type`, `Electron2D.Collections.Array` и `Electron2D.Collections.Dictionary` с закрытым списком значений `0.1.0 Preview`.
- Начальная стабильная сериализация `Variant`: internal canonical JSON round-trip для переносимых базовых значений.
- Начальная серверная граница рендеринга: `RenderingServer.CurrentProfile`, `RenderingServer.HasFeature()` и internal `Compatibility`/`Standard` backend abstraction.
- Начальный internal SDL_GPU lifecycle: SDL3-CS dependency, SDL device/window claim/command buffer adapter, frame begin/submit state machine и smoke-проверки ошибок/resize/fullscreen/high-DPI.
- Начальный internal CanvasItem render queue: stable sort, visibility/modulate filtering и contiguous batching для будущих `CanvasItem` submissions.
- Начальный texture resource baseline: public `Texture2D`/`AtlasTexture`/`ViewportTexture`, atlas regions, transparency queries, internal upload/reload/release registry и no-leak runtime smoke.
- Начальный canvas node baseline: public `CanvasItem`, `Node2D`, `Sprite2D`, `CanvasLayer`, transform/visibility/z-order/self-modulate behavior и internal sprite submission.
- Начальный camera/viewport baseline: public `Camera2D`, `Viewport`, current camera selection, camera transform, pixel snapping, `SceneTree.Root` как `Viewport` instance и internal resize/high-DPI presentation plan.
- Начальный immediate drawing baseline: `CanvasItem._Draw()`, `QueueRedraw()`, `DrawLine()`, `DrawRect()`, `DrawCircle()`, `DrawPolygon()`, `DrawTexture()`, `DrawString()`, public `Font`/`HorizontalAlignment` и internal command capture.
- Начальный text backend baseline: public `Font` measurement API, `VerticalAlignment`, `Control`, `Label`, internal glyph layout, fallback font resolution, layout cache и SDL_ttf boundary через SDL3-CS.
- Начальный offscreen render target recovery baseline: `Viewport.GetTexture()`, внутренние render target descriptors для кода движка и восстановление active texture resources после пересоздания device.
- Начальный canvas shader import baseline: public `Shader`, import-time vertex/fragment compilation через SDL_shadercross boundary, diagnostics file/line/column и iOS artifact без runtime compilation.
- Начальный shader material baseline: public `Material`/`ShaderMaterial`, supported uniforms, `Texture2D` sampler parameters, reserved canvas built-ins и stable internal material parameter JSON snapshot.
- Начальный SDL_Renderer compatibility backend baseline: internal frame plan для sprites, UI/text, primitives, tile-like texture copies, documented limitations и golden reference command stream.
- Начальный Android mobile GPU fallback baseline: internal mobile SDL_GPU create profile, smoke steps texture/pipeline/command buffer/first submit, `Automatic`/`FailIfUnavailable` policy и startup result с GPU/driver/backend/reasons.
- Тестовая инфраструктура и desktop CI matrix.
- GitHub Wiki source для API compatibility.
- Package metadata `0.1.0-preview`.
- MIT License: корневой `LICENSE`, package metadata и source-file headers согласованы и проверяются CI.

## Чего пока нет

- Real texture GPU transfer/upload, PNG/JPEG pixel decoding, glyph rasterization/font atlas generation, real-window GPU smoke/fallback pipeline, Android device/export run, SDL_Renderer window presentation, public `ResourceLoader`/`ResourceSaver`, audio importers, file-level scene serialization, metadata source generator, `InputMap`/actions/gamepad/touch/mobile input, real physics simulation, production Box2D.NET backend, mobile physics AOT proof, contacts/queries и запись geometry в production solver, editor script attach/external IDE workflow, full `ConnectFlags` semantics, `CallGroupFlags`, `GetPath()`, `GetPathTo()`, `SetDeferred()`, pause/process modes, `CanvasItem.Material`, real shader/material GPU binding, real primitive/GPU rasterization, real render-to-texture draw pass, real text raster/GPU draw call, camera smoothing/limits и public `Window` API ещё реализуются следующими задачами.
- Экспорт Android/iOS пока отмечен как явный release gap, а не как active CI gate.

## Правило API

В новый runtime не переносится Unity-like/component history. Публичный API должен появляться только как согласованный Godot-like 2D-поднабор.

## Breaking changes policy для 0.x

До стабильной версии `1.0` публичный API может меняться между preview-сборками. Breaking changes допустимы только при явной записи в `CHANGELOG.md`, `RELEASE-NOTES.md` и compatibility table; compatibility layer ради старого API не добавляется.
