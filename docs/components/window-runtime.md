# Window runtime component

Last updated: 2026-09-23

## Scope and types

[Window](../classes/Window.md) derives from [Viewport](../classes/Viewport.md), which derives from the neutral Node. Window owns the [ModeEnum](../classes/Window.ModeEnum.md) and [Flags](../classes/Window.Flags.md) identifiers. A consumer configures a root Window, adds scene children, and calls Engine.Run. The root window connects scene processing and retained canvas drawing through [RenderingServer](../classes/RenderingServer.md). Broader rendering and GUI APIs remain incomplete.

[ADR 0028](../decisions/rendering.md#adr-0028) selects HLSL and GLSL compilation at project import/build, followed by one SPIR-V validation, reflection and GPU-program path through SDL3-CS/SDL_shadercross. Compatible SPIR-V from third-party compilers uses the same path. Direct language support also requires diagnostics, material parameters, textures, consistent bindings and backend checks. The current [shader/material integration](shader-materials.md) executes typed fragment uniforms and sampled textures on Linux Wayland/Vulkan. The SDL_Renderer fallback rejects arbitrary shaders explicitly.

## Runtime flow

Engine reserves its idle state, opens the native window through DisplayServer and initializes RenderingServer before creating SceneTree. It publishes the tree before ready, drives the native event pump before fixed/process frames, and submits the canvas after scene processing. Cleanup disposes the scene, rendering resources and display in that order. MaxFPS uses unscaled monotonic time; native events continue during bounded waits. SceneTree.Quit requests exit and returns its code from Run. Window.CloseRequested precedes the default AutoAcceptQuit decision.

Window properties configure title, positive client size, minimum/maximum constraints, optional desktop position and screen, mode, four executable policies and visibility. Mode queries report observed native state; flag queries retain accepted configuration and are stored in PackedScene. Unsupported policies reject use, and platform refusal does not commit a requested flag. Native calls inherit DisplayServer platform capability failures. Window.Position is the native desktop position and is rejected on Wayland. Window declares its own Visible, Show/Hide and VisibilityChanged API; it has no canvas transform or drawing surface. GetVisibleRect uses a zero client origin. SizeChanged follows client-size updates, never mere desktop movement.

Window also forwards effective mouse entry/exit, content-scale changes and completed file-drop snapshots before frame callbacks. Native IME activation/caret placement, taskbar progress requests, decorated geometry, maximization capability and centering are available on Window with the existing platform limits. Signal failures remain visible after the native queue drains; scene and native cleanup still run.

Viewport shares SceneTree's current handled-input flag. PushInput borrows a client-coordinate event for synchronous scene dispatch without modifying global Input polling state. Child nodes discover their Window/Viewport through ancestor lookup.

## Dependencies and invariants

- Engine.Run depends on SceneTree and Window; Window depends on DisplayServer and RenderingServer. SDL bindings stay internal to those backend implementations. All types remain in Electron2D.dll.
- One active native root is supported. Child Viewports are rejected before hierarchy mutation; direct SceneTree(Window) activation is rejected unless Engine.Run has opened that root.
- Attached mutation and native calls use the owner/main thread. Quit and MaxFPS configuration accept cross-thread calls.
- Native services opened directly through DisplayServer must finish before shutdown. Pending asynchronous file dialogs can reject native disposal under the existing DisplayServer contract; Run reports the cleanup failure and DisplayServer.Instance remains available for completion/release. Window exposes no asynchronous dialog API yet.
- Engine remains reserved throughout scene exit, disposal and native cleanup. Manual frame/stop/tree-disposal interference is rejected. All owned cleanup stages are attempted and failures remain observable.
- Validation/busy-engine rejection preserves caller ownership. After reservation, failed startup also disposes the transferred root. A later run uses a new Window.
- PackedScene stores the title and size/limit configuration plus inherited stored Node properties. Position is an optional platform startup request, not stored scene data. CurrentScreen follows the same optional-request rule. Mode and the four supported policies are stored; unset/default policies do not issue unsupported startup requests.

## Verification and limits

WindowRuntimeTests covers detached configuration/validation, packed reconstruction, native title/visibility, root discovery, input-before-frame ordering, viewport input borrowing/handling, frame limiting, quit/close policy, cross-thread quit, lifecycle interference, callback failure cleanup, startup ownership and repeated runs. Mode/flag startup and mutation, packed policy reconstruction, unsupported-flag rollback, screen validation, IME enable/disable, retained file-drop data and window-signal failure continuation are also covered. Dummy and native Wayland executions passed; dummy accepts native flag setters without applying them, so native policy assertions run on real backends. The preceding root-lifecycle slice also passed as a published self-contained linux-x64 test binary on Wayland with LD_LIBRARY_PATH unset; that packaging contains Electron2D.dll and native SDL through the runtime project. This controls extension was checked through dotnet run with LD_LIBRARY_PATH unset. Native Wayland tests use injected events, not physical user input or visual acceptance. Repeated SDL/GTK initialization emits a GTK locale warning. Canvas pixel and warmed render-allocation checks are recorded in [Canvas rendering](canvas-rendering.md). Other platforms, full performance acceptance, content scaling, GUI, offscreen targets and multiwindow behavior remain unverified or absent.

Run: `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_WINDOW=1 SDL_VIDEODRIVER=dummy dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` (use `wayland` for the native integration check).

Decisions: [0004](../decisions/product.md#adr-0004), [0008](../decisions/scene.md#adr-0008), [0016](../decisions/core-object-runtime.md#adr-0016), [0021](../decisions/product.md#adr-0021), [0028](../decisions/rendering.md#adr-0028), [0040](../decisions/display.md#adr-0040). Per-member incomplete dependencies live in the [Window](../coverage/classes/Window.md) and [Viewport](../coverage/classes/Viewport.md) coverage pages.
