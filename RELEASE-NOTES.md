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
- Тестовая инфраструктура и desktop CI matrix.
- GitHub Wiki source для API compatibility.
- Package metadata `0.1.0-preview`.
- MIT License: корневой `LICENSE`, package metadata и source-file headers согласованы и проверяются CI.

## Чего пока нет

- Real texture GPU transfer/import, real-window GPU smoke/fallback pipeline, public `ResourceLoader`/`ResourceSaver`, import cache, file-level scene serialization, full `ConnectFlags` semantics, `CallGroupFlags`, `GetPath()`, `GetPathTo()`, `SetDeferred()`, pause/process modes, `CanvasItem.Material`, real shader/material GPU binding, real primitive/GPU rasterization, real render-to-texture draw pass, real text raster/GPU draw call, camera smoothing/limits и public `Window` API ещё реализуются следующими задачами.
- Экспорт Android/iOS пока отмечен как явный release gap, а не как active CI gate.

## Правило API

В новый runtime не переносится Unity-like/component history. Публичный API должен появляться только как согласованный Godot-like 2D-поднабор.

## Breaking changes policy для 0.x

До стабильной версии `1.0` публичный API может меняться между preview-сборками. Breaking changes допустимы только при явной записи в `CHANGELOG.md`, `RELEASE-NOTES.md` и compatibility table; compatibility layer ради старого API не добавляется.
