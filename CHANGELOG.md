# Changelog

## 0.1.0-preview

Статус: clean rewrite baseline.

### Добавлено

- Новый пустой runtime-проект `src/Electron2D/Electron2D.csproj` с package version `0.1.0-preview`.
- Начальный public API: `Object`, `RefCounted`, `Resource`.
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
- Resource import cache baseline: internal pipeline для `.e2res` discovery, cache artifacts отдельно от source assets, reimport при изменении source/dependency, сохранение старого валидного cache при ошибке и prune удалённых source assets.
- Texture image import baseline: internal PNG/JPEG metadata importer, sidecar `<image>.e2import.json`, filter/repeat/mipmaps, atlas regions, platform variants и stable `texture.e2tex.json` cache artifact.
- Font import baseline: internal TTF/OTF metadata importer, sidecar `<font>.e2import.json`, fallback font dependencies, SDF/bitmap policy и stable `font.e2font.json` cache artifact.
- Shader source import baseline: internal `.e2shader` importer, sidecar `<shader>.e2import.json`, platform-specific compiled stages, diagnostics file/line/column и stable `shader.e2shader.json` cache artifact.
- Scene/resource serialization baseline: internal `SerializedResourceDocument`, `SceneFileDocument`, typed property value model, custom `Resource` round-trip, arrays, dictionaries, enums, nullable и resource reference slots.
- AOT-safe metadata baseline: internal `ResourceObjectMetadataRegistry`, typed resource/property descriptors, custom `Resource` serialization без reflection fallback и trimmed/NativeAOT smoke verifier.
- Data stability stress gate: 100 save/load cycles, rename/move resource UID stability, import cache rebuild, corruption diagnostics и исправление prune cache artifacts при переносе ресурса с тем же UID.
- C# script class baseline: ordinary .NET script classes, inheritance from `Node`, lifecycle callbacks, service access через `GetTree()`/`RenderingServer` и script sample в `electron2d-empty` template.
- Script metadata baseline: public `[Export]`, `[Signal]`, `[Tool]` marker attributes, internal AOT-safe metadata bridge, exported property round-trip, callable signal registration и sandboxed experimental `[Tool]` state.
- Input event mapping baseline: public `InputEventKey`, `InputEventMouseButton`, `InputEventMouseMotion`, `Key`, `KeyLocation`, `MouseButton`, `MouseButtonMask`, internal platform mapper для keyboard, mouse, wheel, text input и dispatch order через `SceneTree`.
- PhysicsServer2D boundary baseline: public `PhysicsServer2D`, `SpaceParameter`, `ShapeType`, `ProcessInfo`, RID creation/free для spaces, areas, bodies, joints, shapes и internal swappable backend без публичных Box2D handles.
- Physics nodes lifecycle baseline: public `CollisionObject2D`, `PhysicsBody2D`, `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D`, `Shape2D`, RID lifecycle, transform sync и безопасное `QueueFree()` во время physics traversal.
- Shape2D resources baseline: public `RectangleShape2D`, `CircleShape2D`, `CapsuleShape2D`, `SegmentShape2D`, `ConvexPolygonShape2D`, `ConcavePolygonShape2D`, validation, lazy shape RID creation, concave-only-static rule и AOT-safe serialization metadata.
- Collision material state baseline: public `PhysicsMaterial`, layer/mask helper methods, `PhysicsBody2D.PhysicsMaterialOverride`, validation, AOT-safe material serialization и internal collision/body-state snapshots.
- Area2D sensors baseline: public `GetOverlappingBodies()`, `GetOverlappingAreas()`, `HasOverlappingBodies()`, `HasOverlappingAreas()`, `OverlapsBody()`, `OverlapsArea()` и сигналы `body_entered`/`body_exited`/`area_entered`/`area_exited` поверх managed AABB snapshots.
- Direct 2D physics query baseline: public `World2D`, `PhysicsDirectSpaceState2D`, query parameter resources, `RayCast2D` execution, ray/point/shape AABB queries, filters, masks и no-hit cases.
- Fixed physics and rigid body motion baseline: `SceneTree.PhysicsFrame()` запускает fixed ticks по `1/60`, `RigidBody2D` двигается по `LinearVelocity`, fast body AABB sweep останавливает тело перед `StaticBody2D`, `CollisionShape2D.OneWayCollision` работает для движения сверху вниз, а queued bodies пропускаются на следующих fixed ticks.
- Box2D.NET candidate validation baseline: отдельный smoke-проект на `Box2D.NET 3.1.654`, verifier `Verify-Box2DPhysicsCandidate.ps1`, desktop JIT/NativeAOT CI gate, local `win-x64` allocation measurement и documented Android/iOS Release/AOT gaps.
- Variant baseline: `Variant`, `Variant.Type`, `Electron2D.Collections.Array` и `Electron2D.Collections.Dictionary` с закрытым списком значений для `0.1.0 Preview`.
- Stable Variant serialization baseline: internal canonical JSON round-trip для сериализуемых `Variant` значений и понятные ошибки для runtime-only значений.
- Rendering server baseline: `RenderingServer`, nested renderer profiles/features и internal backend abstraction для `Compatibility`/`Standard`.
- Graphics device lifecycle baseline: pinned native backend dependencies, internal GPU rendering backend, platform adapter, window claim/frame state machine и smoke-тесты resize/fullscreen/high-DPI/device errors.
- CanvasItem render queue baseline: internal stable sort по layer/z/y/tree order, visibility filtering, effective modulate и contiguous batching с измеримым draw-call count.
- Texture resource baseline: public `Texture2D`/`AtlasTexture`, atlas region transparency behavior, internal upload/reload/release registry, sampling descriptors и no-leak runtime smoke test.
- Canvas node submission baseline: public `CanvasItem`, `Node2D`, `Sprite2D`, `CanvasLayer`, local/global 2D transforms, visibility/modulate inheritance, `Node2D` global-transform reparent и internal sprite submission model.
- Camera/Viewport presentation baseline: public `Camera2D`, `Viewport`, current camera selection, camera transform, pixel snapping flags, `SceneTree.Root` как `Viewport` instance и internal resize/high-DPI presentation plan.
- Immediate drawing baseline: public `CanvasItem._Draw()`, `QueueRedraw()`, `DrawLine()`, `DrawRect()`, `DrawCircle()`, `DrawPolygon()`, `DrawTexture()`, `DrawString()`, public `Font`/`HorizontalAlignment` и internal cached draw command submission.
- Text backend baseline: public `Font` measurement API, `VerticalAlignment`, `Control`, `Label`, internal glyph layout, fallback font resolution, layout cache и native text backend boundary.
- Offscreen render target recovery baseline: public `ViewportTexture`, `Viewport.GetTexture()`, внутренние render target descriptors для кода движка и восстановление active texture resources после пересоздания device.
- Canvas shader import baseline: public `Shader`, `Shader.Mode.CanvasItem`, import-time vertex/fragment compilation через offline shader compiler boundary, diagnostics file/line/column и iOS artifact без runtime compilation.
- Shader material baseline: public `Material`/`ShaderMaterial`, supported scalar/vector uniforms, `Texture2D` sampler parameters, fail-closed reserved canvas built-ins и stable internal material parameter JSON snapshot.
- Compatibility renderer backend baseline: internal fallback frame plan для sprites, UI/text, primitives, tile-like texture copies, documented limitations и golden reference command stream.
- Android mobile GPU fallback baseline: internal mobile GPU create profile, smoke steps texture/pipeline/command buffer/first submit, `Automatic`/`FailIfUnavailable` policy и startup log с GPU/driver/backend/reasons.
- Settings persistence baseline: internal `Electron2D.ProjectSettings` и `Electron2D.UserSettings` JSON documents, `project.e2d.json` template defaults, input actions, display/window defaults, locale user setting и fail-closed diagnostics для повреждённых файлов.
- Export preset baseline: internal `export_presets.e2export.json` model, deterministic round-trip, SDK/toolchain/signing reference validation и fail-closed diagnostics без signing, deploy или публикации.
- Windows x64 export baseline: internal package plan для `win-x64`, Debug/Release, self-contained publish, window/fullscreen state, renderer profile и локальный verifier exported reference scene.
- Linux x64 glibc export baseline: internal package plan для `linux-x64`, Debug/Release, self-contained publish, Wayland/X11 desktop protocols, явный out-of-scope для musl/ARM runtime identifiers и локальный verifier exported reference scene.
- macOS arm64 export baseline: internal package plan для `osx-arm64`, Debug/Release, self-contained `.app` bundle, Metal-backed desktop backend, x64 policy, user-provided signing plan и macOS-only verifier exported reference scene.
- Тестовая инфраструктура: unit, integration, runtime smoke и golden-data проекты.
- CI-матрица для Windows, Linux и macOS.
- GitHub Wiki source для таблицы совместимости API.
- Пользовательская документация 0.1.0 Preview: установка, первый проект, первая сцена, scripting, resources, physics, UI, animation, Input Map, export limitations и verifier `tools/Verify-UserDocumentation.ps1`.
- Verifier-скрипты для тестов, CI, API compatibility и release metadata.
- MIT License policy: корневой `LICENSE`, MIT source headers для C# и PowerShell файлов, verifier `tools/Verify-SourceLicenseHeaders.ps1` и CI-шаг.

### Изменено

- `main` возвращён к baseline `4007f36bf6857b33d6fc8cf614732f92e839287d`.
- Старая реализация `src/Electron2D/` удалена полностью.
- Корневой `LICENSE` приведён к MIT License, чтобы он совпадал с package metadata `PackageLicenseExpression=MIT`.
- Source layout разнесён на крупные root domains `Core`, `Runtime`, `Graphics`, `Physics`, `Assets`, `Export`: `Core` содержит только базовое ядро, а мелкие подсистемы живут вторым уровнем внутри своих доменов.

### Удалено

- Unity-like/component history, включая `IComponent`, `SpriteRenderer`, `SpriteAnimator`, `AudioSource` и legacy physics components.

### Ограничения

- Runtime assembly экспортирует `120` публичных типов.
- `0.1.0-preview` ещё не является готовым игровым runtime; compatibility renderer backend пока строит deterministic command plan, Android fallback пока проверяется fake adapter в CI, Box2D.NET пока является candidate validation gate без production backend и без mobile AOT proof, physics nodes пока имеют только AABB baseline для queries/overlaps/basic rigid motion без contacts, gravity integration, rigid-rigid collision и записи geometry/material в production solver, PNG/JPEG import пока фиксирует metadata без pixel decoding/GPU upload, TTF/OTF import пока фиксирует metadata без glyph rasterization, shader source import пока не привязан к real draw pipeline/export packaging, scene/resource serialization пока не подключена к public `ResourceLoader`/`ResourceSaver`, metadata source generator, Project Settings UI, Input Map UI и editor script attach workflow ещё не реализованы, а mobile device run/export остаётся следующей задачей.

### Breaking changes policy

- В ветке `0.x` публичный API может меняться между preview-сборками.
- Compatibility layer ради старого API не добавляется.
- Каждое breaking change должно быть явно отражено в changelog и release notes.
