# Changelog

## 0.1.0-preview

Статус: clean rewrite baseline.

### Добавлено

- Новый пустой runtime-проект `src/Electron2D/Electron2D.csproj` с package version `0.1.0-preview`.
- Начальный Godot-like public API: `Object`, `RefCounted`, `Resource`.
- Lifecycle baseline: `Node`, `SceneTree`, `InputEvent`, `_EnterTree`, `_Ready`, `_Process`, `_PhysicsProcess`, `_Input`, `_ExitTree`.
- Hierarchy baseline: `Owner`, `GetParent()`, `GetChild()`, `GetIndex()`, `MoveChild()`, `Reparent()`, `QueueFree()` и `Object.IsQueuedForDeletion()`.
- Node path baseline: `NodePath`, `GetNode()` и `GetNodeOrNull()` для relative/absolute lookup.
- Group baseline: `AddToGroup()`, `RemoveFromGroup()`, `IsInGroup()`, `GetGroups()` и `SceneTree` group queries/calls.
- Signal baseline: `Callable`, `Error`, `ConnectFlags`, `AddUserSignal()`, `Connect()`, `Disconnect()`, `IsConnected()` и `EmitSignal()`.
- Deferred-call baseline: `Object.CallDeferred()`, `Callable.CallDeferred()`, deterministic deferred queue и безопасное изменение дерева во время traversal.
- Scene resource baseline: `PackedScene`, `Pack()`, `CanInstantiate()`, `Instantiate()`, `SceneTree.CurrentScene` и `ChangeSceneToPacked()`.
- Внутренняя runtime-диагностика: исключения из lifecycle, group call, deferred call и signal callback сохраняют node/callback/kind/message/stack trace, а движок продолжает работу по documented recover policy.
- 2D math baseline: `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`.
- Random baseline: `RandomNumberGenerator` с `Seed`, `State`, `Randi()`, `RandiRange()`, `Randf()`, `RandfRange()`, `Randfn()` и воспроизводимой PCG32 sequence policy.
- Identity baseline: `StringName` с ordinal equality/hashing и `Rid` с invalid/default semantics, comparison operators и internal allocator для будущих серверов.
- Resource UID baseline: public `ResourceUid` с `uid://` conversion, path mapping и rename/move через `SetId()`.
- Resource file baseline: internal `.e2res` JSON document model, external/internal references, stable LF output и golden-data проверка exact text.
- Variant baseline: `Variant`, `Variant.Type`, `Electron2D.Collections.Array` и `Electron2D.Collections.Dictionary` с закрытым списком значений для `0.1.0 Preview`.
- Stable Variant serialization baseline: internal canonical JSON round-trip для сериализуемых `Variant` значений и понятные ошибки для runtime-only значений.
- Rendering server baseline: `RenderingServer`, nested renderer profiles/features и internal backend abstraction для `Compatibility`/`Standard`.
- SDL_GPU lifecycle baseline: pinned SDL3-CS `3.4.10.3`, internal `SdlGpuRenderingBackend`, SDL adapter, window claim/frame state machine и smoke-тесты resize/fullscreen/high-DPI/device errors.
- CanvasItem render queue baseline: internal stable sort по layer/z/y/tree order, visibility filtering, effective modulate и contiguous batching с измеримым draw-call count.
- Texture resource baseline: public `Texture2D`/`AtlasTexture`, atlas region transparency behavior, internal upload/reload/release registry, sampling descriptors и no-leak runtime smoke test.
- Canvas node submission baseline: public `CanvasItem`, `Node2D`, `Sprite2D`, `CanvasLayer`, local/global 2D transforms, visibility/modulate inheritance, `Node2D` global-transform reparent и internal sprite submission model.
- Camera/Viewport presentation baseline: public `Camera2D`, `Viewport`, current camera selection, camera transform, pixel snapping flags, `SceneTree.Root` как `Viewport` instance и internal resize/high-DPI presentation plan.
- Immediate drawing baseline: public `CanvasItem._Draw()`, `QueueRedraw()`, `DrawLine()`, `DrawRect()`, `DrawCircle()`, `DrawPolygon()`, `DrawTexture()`, `DrawString()`, public `Font`/`HorizontalAlignment` и internal cached draw command submission.
- Тестовая инфраструктура: unit, integration, runtime smoke и golden-data проекты.
- CI-матрица для Windows, Linux и macOS.
- GitHub Wiki source для таблицы совместимости API.
- Verifier-скрипты для тестов, CI, API compatibility и release metadata.
- MIT License policy: корневой `LICENSE`, MIT source headers для C# и PowerShell файлов, verifier `tools/Verify-SourceLicenseHeaders.ps1` и CI-шаг.

### Изменено

- `main` возвращён к baseline `4007f36bf6857b33d6fc8cf614732f92e839287d`.
- Старая реализация `src/Electron2D/` удалена полностью.
- Корневой `LICENSE` приведён к MIT License, чтобы он совпадал с package metadata `PackageLicenseExpression=MIT`.

### Удалено

- Unity-like/component history, включая `IComponent`, `SpriteRenderer`, `SpriteAnimator`, `AudioSource` и legacy physics components.

### Ограничения

- Runtime assembly экспортирует `39` публичных типов.
- `0.1.0-preview` ещё не является готовым игровым runtime; дальнейшая реализация идёт задачами из `TASKS.md`.

### Breaking changes policy

- В ветке `0.x` публичный API может меняться между preview-сборками.
- Compatibility layer ради старого API не добавляется.
- Каждое breaking change должно быть явно отражено в changelog и release notes.
