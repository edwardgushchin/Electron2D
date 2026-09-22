# Rendering domain

Last updated: 2026-09-23

## Responsibility

Rendering turns retained scene commands and typed resources into frames for the active root Window. Runtime source is in `src/Servers/Rendering/`; it compiles into Electron2D.dll. SDL3-CS and owned SDL handles remain internal. DisplayServer exposes supported borrowed operating-system context identities under ADR 0042.

## Components and public surface

| Component | Public types and integration | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | [RenderingServer](../classes/RenderingServer.md), CanvasItem drawing, [Sprite](../classes/Sprite.md) nodes and Texture drawing | Executable rectangle/line/texture path; full API incomplete |
| [Shader materials](../components/shader-materials.md) | Shader, ShaderMaterial, Material, Texture and ImageTexture, owned by Resources | Executable HLSL/GLSL import, typed uniforms and sampled textures; broader language profile incomplete |

Games use Sprite for texture, sheet-frame and region drawing, or record custom commands from CanvasItem.OnDraw through CanvasItem and Texture. RenderingServer provides the active method/driver, frame events and clear/submission controls. Engine.Run starts and closes the renderer. CanvasItem.ItemRectChanged reports local geometry changes synchronously; Sprite integrates the event with its setters while resource worker notifications remain atomic redraw requests.

## Dependencies and invariants

- Uses Scene for Window, tree membership, transforms, visibility, behind-parent and nested local Y ordering, with stable Z precedence.
- Uses Resources for copied image snapshots and typed materials; consumers borrow these resources.
- GPU is primary; compatibility is explicit startup fallback and rejects shaders.
- Frame submission and native resource lifetimes belong to the scene owner thread.
- HLSL and GLSL share the SPIR-V interface. Import/build compiles source; runtime consumes bytecode.
- Invalid resources or unsupported backend features fail explicitly. SDL types and owned graphics handles remain internal; DisplayServer may expose documented borrowed OS/context identities.

## Verification and limits

Current native verification covers Linux Wayland GPU/Vulkan and compatibility, plus the software renderer under the dummy video driver. Pixel checks cover ordinary drawing, both shader languages, canvas ordering and lifecycle-driven recording through notifications/events/overrides; other target backends, user visual acceptance, whole-frame performance, lights, clipping, meshes, GUI, offscreen/multiwindow rendering and device recovery remain unfinished. See the component pages for exact checks and limits.

[ADR 0028](../decisions/rendering.md#adr-0028) owns backend/shader decisions, [ADR 0004](../decisions/product.md#adr-0004) owns the 2D product boundary, and [ADR 0021](../decisions/product.md#adr-0021) owns the current platform gate.
