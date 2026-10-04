# Electron2D documentation

Last updated: 2026-10-04

This directory describes the engine as it exists now. Planned features are listed only as explicit limitations or next boundaries; they are never presented as implemented.

## Current snapshot

- Product boundary: exclusively 2D; 3D is out of scope.
- Game runtime target matrix: Windows, macOS, Linux on X11 and Wayland, Android, iOS, Android TV, tvOS, and Web. Editor target matrix: Windows, macOS, and Linux on X11 and Wayland. These are product boundaries, not claims of completed delivery.
- Game-object model: Node-based and scene-oriented. `Node` is the primary public game object, `SceneTree` owns the active hierarchy, and `PackedScene` packages any reusable Node hierarchy—from one composed object to a complete level—for independent instantiation. The current packing implementation is typed and in-memory; disk and editor workflows are not implemented.
- Public engine assembly: one managed `Electron2D.dll` class library, including internal SDL3-CS bindings, Box2D.NET physics and the managed text/audio dependency sources. Native libraries remain separate deployment files.
- Product source boundary: runtime engine code lives in `src/`; the future self-hosted editor belongs to a separate executable project under `editor/Electron2D.Editor/`; first-party example games and templates belong under `examples/<Game>/`. Editor and games depend on the public runtime API, never the reverse.
- Production source root: `src/`, organized by engine module while retaining the flat public `Electron2D` namespace.
- Architecture context: `decisions/index.md` routes to bounded domain decision documents; read only the affected documents and explicit cross-domain dependencies. No decision document may exceed 500 lines.
- Target framework: .NET 10 (`net10.0`), with target-specific Android, iOS and tvOS frameworks selected by runtime identifier.
- Implemented domains: Core, Input, Scene, Localization, Resources, Display, Rendering, Navigation, Physics, Audio and Networking. Each domain page records its exact capabilities and remaining gaps.
- Implemented components include object/resource lifetime, math, configuration and I/O, input, scene scheduling, window lifecycle, translation, CPU images and codecs, canvas drawing, typed shader materials, GUI/text, navigation, physics and audio playback/recording. The [inventory](inventory.md) links implemented production types to their source and reference pages.
- SDL3-CS managed bindings: complete core, Image and ShaderCross modules from pinned release `v3.4.16.1`, internal to the engine assembly and refreshed by the release import script.
- Native SDL packaging: Linux x64 consumers receive SDL 3.4.16, SDL_image 3.4.6 and SDL_shadercross 3.0.0 from the runtime project’s pinned native packages. Published self-contained host/rendering/codec checks run without a development library path; native libraries remain separate files.
- Platform delivery status: Linux Wayland has native host/rendering checks, with additional XWayland renderer checks. Diagnostic Android phone/TV packages and isolated browser probes cover the paths recorded in the [platform verification matrix](platform-verification.md). Production mobile/browser hosts, Apple application bundles, a complete target CI matrix, an editor executable and signing workflows remain absent. The macOS Apple library-build workflow is prepared but has not been verified by a run.
- 2D physics backend: internal vendored `Box2D.NET` supplies the executable CPU body, area, shape, query and joint profiles documented in the [physics domain](domains/physics.md).
- Rendering architecture: Engine.Run owns an SDL GPU primary renderer with a startup SDL_Renderer fallback. Retained CanvasItem rectangles, lines and textures use transforms, visibility, stable Z order and modulation. Typed shader materials require the GPU path; fallback rejects them explicitly. HLSL/GLSL compile at import/build into the common SPIR-V path. The [rendering domain](domains/rendering.md) records the verified baseline and remaining capabilities.
- Managed memory remains runtime-owned; `IDisposable` controls deterministic logical/native cleanup. Public manual reference counting is excluded, while internal asset leases are reserved for a future resource manager with concrete native-backed assets.
- Canonical scalar mathematics with seven constants, 127 typed overloads, strict `1e-6f`/`1e-14` approximation, angle/interpolation/wrapping helpers, documented managed failures, and allocation-free warmed execution: implemented in `Mathf`. Geometry and Entity transform math use this shared contract.
- Floating-point RGBA values, HSV and perceptual OKHSL conversion, straight-alpha blend, arithmetic/comparison, packed/HTML formats, strict finite configuration serialization, packed-scene value storage, and all 146 standard named colors: implemented without a renderer dependency.
- Engine-owned `Vector2`/`Vector2i` and `Vector4`/`Vector4i` families with complete float/integer value math, strict typed configuration schemas, packed-scene storage, documented IEEE/overflow behavior, and allocation-free warmed numeric paths: implemented. The four-component values are numeric tuples and do not introduce 3D/4D scene geometry.
- Floating-point `Rect2` geometry with explicit negative-size normalization, half-open containment, enclosure/intersection/growth/merge/support and transform-bound operations, strict finite configuration serialization, packed-scene storage, and stable side identities: implemented without renderer, UI, or physics dependencies.
- Integer `Rect2i` geometry with explicit negative-size normalization, half-open containment, enclosure/intersection/growth/merge, typed `Rect2` conversions, strict configuration serialization, and packed-scene storage: implemented for foreseeable pixel, atlas, image-region, and grid bounds without depending on those future consumers.
- Engine-owned `Transform` is implemented with complete affine math, rectangle operators, strict finite configuration persistence, direct packed-scene storage, allocation-free numeric hot paths, and direct `Entity` local/global integration through `Vector2`.
- Process-wide typed project settings, feature overrides, directory-backed `res://`/`user://`, blocking typed file access with metadata/hashes/temporary files/cross-platform extended attributes/compression/authenticated encryption, scoped directory navigation/listing/mutations/links/temporary ownership/filesystem identity, host-driven bounded fixed-step scheduling, scaled/original frame deltas, time scaling, frame metrics, named engine singletons, typed keyboard/mouse/touch/gesture/controller events, action mapping and frame-latched state, deterministic Node input propagation, typed sectioned configuration files with atomic persistence and authenticated encryption, separate Node, CanvasItem and spatial Entity layers, local/global transforms, hierarchy paths and groups, visibility and Z state, pause-aware public/internal process lanes, reusable Timer scene nodes, lightweight one-shot frame timers, typed Tween sequences and interpolation, exception-safe scene-tree lifecycle, typed group operations, queued deletion, typed deferred work and event connections, in-memory typed packed scenes with per-instance local resources, translations, notifications including the future-facing `ScriptChanged` hook, typed editor-property descriptors, and the typed resource base with graph duplication: implemented.
- Managed CPU images implement raw pixel layouts, copied-buffer ownership, mip chains, format conversion, transforms, filters, compositing, channel/alpha inspection, normal-map helpers and metrics. PNG/JPEG/WebP/BMP/TGA file/buffer decoding and PNG/JPEG encoding execute through the native image integration. Further codecs and block compression/decompression remain incomplete under ADR 0039.
- Rendering has Linux Wayland native pixel checks for canvas geometry, shader parameters, textures, text and resource cleanup; broader rendering and shader features remain partial in coverage. Physics and non-spatial audio playback/recording are implemented within their documented profiles. Scene file serialization and an editor application remain absent. Native input pumping exists in DisplayServer; remaining hardware gaps have explicit triggers in ADR 0038.
- Native TCP/UDP/Unix-domain listeners and peers, endian-aware stream codecs, caller-span packets, framed streams and extensible typed transport hooks are implemented. Linux x64 IPv4/IPv6 loopback, local IPC, scene-owned exchange and warmed managed allocation checks pass; TLS client/server streams, key/certificate resources and `.key`/`.crt` resource loading are implemented on Linux OpenSSL 3; further protocol layers, routed traffic and other platforms retain separate gates in the [networking domain](domains/networking.md).
- The editor source root is reserved in this repository, but no editor project or source exists yet. Its future assembly is a consumer of `Electron2D.dll` and is not part of the one-runtime-DLL boundary.
- Persistent event connections: deferred until a typed stable endpoint schema exists.
- Packed/exported resource filesystems, import remapping, `uid://`, and `pipe://` are not implemented; current `res://`/`user://` resolution is directory-backed and lexically confined. FastLZ and Zstandard are explicit file-access gaps. Extended attributes and directory links are implemented for Linux, macOS, and Windows, with native-host verification currently limited to Linux. Android/iOS link and drive-enumeration integration is explicitly absent.

## Navigation

- [Inventory](inventory.md)
- [Third-party software](thirdparty.md)
- [Approved Sprite identity and source assets](design/identity.md)
- [API comparison and implementation roadmap](coverage/index.md)
- Domain: [Core](domains/core.md)
- Domain: [Input](domains/input.md)
- Domain: [Scene](domains/scene.md)
- Domain: [Localization](domains/localization.md)
- Domain: [Resources](domains/resources.md)
- Domain: [Display](domains/display.md)
- Domain: [Rendering](domains/rendering.md)
- Domain: [Navigation](domains/navigation.md)
- Domain: [Physics](domains/physics.md)
- Domain: [Audio](domains/audio.md)
- Domain: [Networking](domains/networking.md)
- Component: [Native streams and packets](components/networking.md)
- Component: [TLS and security resources](components/tls.md)
- Component: [HTTP transfers and stream compression](components/http.md)
- Component: [WebSocket messages](components/websocket.md)
- Component: [Object lifecycle](components/object-lifecycle.md)
- Component: [Typed event connections](components/event-connections.md)
- Component: [Typed editor properties](components/editor-properties.md)
- Component: [Scalar math](components/scalar-math.md)
- Component: [Color values](components/color-values.md)
- Component: [Geometry values](components/geometry-values.md)
- Component: [Configuration files](components/config-files.md)
- Component: [File and directory access](components/file-access.md)
- Component: [Project settings](components/project-settings.md)
- Component: [Main loop](components/main-loop.md)
- Component: [Engine runtime](components/engine-runtime.md)
- Component: [Input runtime](components/input-runtime.md)
- Component: [Scene hierarchy](components/scene-hierarchy.md)
- Component: [Scene tree](components/scene-tree.md)
- Component: [Tweening](components/tweening.md)
- Component: [Packed scenes](components/packed-scenes.md)
- Component: [Translation](components/localization.md)
- Component: [Resource base](components/resources.md)
- Component: [Managed images](components/images.md)
- Component: [Noise](components/noise.md)
- Component: [Display server](components/display-server.md)
- Component: [Window runtime](components/window-runtime.md)
- Component: [Canvas rendering](components/canvas-rendering.md)
- Component: [Shader materials](components/shader-materials.md)
- Classes: [DisplayServer](classes/DisplayServer.md), [Window](classes/Window.md), [Viewport](classes/Viewport.md), [RenderingServer](classes/RenderingServer.md)
- Classes: [Shader](classes/Shader.md), [Shader.Mode](classes/Shader.Mode.md), [Material](classes/Material.md), [ShaderMaterial](classes/ShaderMaterial.md), [Texture](classes/Texture.md), [ImageTexture](classes/ImageTexture.md)
- Class: [ElectronObject](classes/ElectronObject.md)
- Class: [EventConnection](classes/EventConnection.md)
- Class: [PropertyDescriptor](classes/PropertyDescriptor.md)
- Class: [PropertyDescriptor&lt;TOwner, TValue&gt;](classes/PropertyDescriptor.Generic.md)
- Static class: [Mathf](classes/Mathf.md)
- Struct: [Color](classes/Color.md)
- Static class: [Colors](classes/Colors.md)
- Struct: [Vector2](classes/Vector2.md)
- Struct: [Vector2i](classes/Vector2i.md)
- Struct: [Vector4](classes/Vector4.md)
- Struct: [Vector4i](classes/Vector4i.md)
- Struct: [Rect2](classes/Rect2.md)
- Struct: [Rect2i](classes/Rect2i.md)
- Struct: [Transform](classes/Transform.md)
- Enum: [Side](classes/Side.md)
- Class: [ConfigKey&lt;T&gt;](classes/ConfigKey.Generic.md)
- Class: [ConfigFile](classes/ConfigFile.md)
- Class: [FileAccess](classes/FileAccess.md)
- Class: [DirAccess](classes/DirAccess.md)
- Enum: [FileAccessModeFlags](classes/FileAccessModeFlags.md)
- Enum: [FileCompressionMode](classes/FileCompressionMode.md)
- Enum: [UnixPermissionFlags](classes/UnixPermissionFlags.md)
- Class: [ProjectSetting&lt;T&gt;](classes/ProjectSetting.Generic.md)
- Class: [ProjectSettings](classes/ProjectSettings.md)
- Class: [MainLoop](classes/MainLoop.md)
- Class: [Engine](classes/Engine.md)
- Class: [EngineVersionInfo](classes/EngineVersionInfo.md)
- Class: [Input](classes/Input.md)
- Class: [InputMap](classes/InputMap.md)
- Class hierarchy: [InputEvent](classes/InputEvent.md), [InputEventFromWindow](classes/InputEventFromWindow.md), [InputEventWithModifiers](classes/InputEventWithModifiers.md), [InputEventAction](classes/InputEventAction.md), [InputEventKey](classes/InputEventKey.md), [InputEventMouse](classes/InputEventMouse.md), [InputEventMouseButton](classes/InputEventMouseButton.md), [InputEventMouseMotion](classes/InputEventMouseMotion.md), [InputEventJoypadButton](classes/InputEventJoypadButton.md), [InputEventJoypadMotion](classes/InputEventJoypadMotion.md), [InputEventScreenTouch](classes/InputEventScreenTouch.md), [InputEventScreenDrag](classes/InputEventScreenDrag.md), [InputEventGesture](classes/InputEventGesture.md), [InputEventMagnifyGesture](classes/InputEventMagnifyGesture.md), and [InputEventPanGesture](classes/InputEventPanGesture.md)
- Input enums: [Key](classes/Key.md), [KeyModifierMask](classes/KeyModifierMask.md), [KeyLocation](classes/KeyLocation.md), [MouseButton](classes/MouseButton.md), [MouseButtonMask](classes/MouseButtonMask.md), [JoyAxis](classes/JoyAxis.md), and [JoyButton](classes/JoyButton.md)
- Scene hierarchy: [Node](classes/Node.md), [CanvasItem](classes/CanvasItem.md), [Entity](classes/Entity.md)
- Class: [Sprite](classes/Sprite.md)
- Enum: [ProcessMode](classes/ProcessMode.md)
- Class: [SceneTree](classes/SceneTree.md)
- Class: [Timer](classes/Timer.md)
- Enum: [ProcessPhase](classes/ProcessPhase.md)
- Class: [SceneTreeTimer](classes/SceneTreeTimer.md)
- Enum: [GroupCallFlags](classes/GroupCallFlags.md)
- Class: [Tween](classes/Tween.md)
- Enum: [Tween.TweenPauseMode](classes/Tween.TweenPauseMode.md)
- Enum: [Tween.TransitionType](classes/Tween.TransitionType.md)
- Enum: [Tween.EaseType](classes/Tween.EaseType.md)
- Class: [Tweener](classes/Tweener.md)
- Class: [PropertyTweener&lt;TValue&gt;](classes/PropertyTweener.Generic.md)
- Class: [MethodTweener&lt;TValue&gt;](classes/MethodTweener.Generic.md)
- Class: [CallbackTweener](classes/CallbackTweener.md)
- Class: [IntervalTweener](classes/IntervalTweener.md)
- Class: [SubtweenTweener](classes/SubtweenTweener.md)
- Class: [AwaitTweener](classes/AwaitTweener.md)
- Class: [PackedScene](classes/PackedScene.md)
- Class: [SceneState](classes/SceneState.md)
- Enum: [PackedSceneEditState](classes/PackedSceneEditState.md)
- Class: [TranslationServer](classes/TranslationServer.md)
- Class: [Resource](classes/Resource.md)
- Enum: [DeepDuplicateMode](classes/DeepDuplicateMode.md)
- Class: [Image](classes/Image.md)
- Image enums: [Image.Format](classes/Image.Format.md), [Image.Interpolation](classes/Image.Interpolation.md), [Image.AlphaMode](classes/Image.AlphaMode.md), [Image.UsedChannels](classes/Image.UsedChannels.md), [Image.CompressSource](classes/Image.CompressSource.md), [Image.CompressMode](classes/Image.CompressMode.md), and [Image.ASTCFormat](classes/Image.ASTCFormat.md)
- Struct: [ImageMetrics](classes/ImageMetrics.md)
- Enum: [ClockDirection](classes/ClockDirection.md)
- Decisions: [routing index](decisions/index.md) with bounded logs for [Product architecture](decisions/product.md), [Core object/runtime](decisions/core-object-runtime.md), [Core data/I/O](decisions/core-data-io.md), [Core math](decisions/core-math.md), [Input](decisions/input.md), [Scene](decisions/scene.md), [Resources](decisions/resources.md), [Localization](decisions/localization.md), and [Rendering](decisions/rendering.md).

Repository workflow instructions live in [AGENTS.md](../AGENTS.md). The [maintenance contract](maintaining.md) covers implementation and documentation checks; the [decision index](decisions/index.md) routes to architectural decisions.

## Rendering integration

- Domain: [Rendering](domains/rendering.md)
- Component: [Canvas rendering](components/canvas-rendering.md)
- Class: [RenderingServer](classes/RenderingServer.md)
