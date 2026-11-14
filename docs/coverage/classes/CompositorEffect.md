# CompositorEffect API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/CompositorEffect.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CompositorEffect`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Owns callback, enabled state and attachment requests for a 3D scene compositor effect. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`enum EffectCallbackType`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Selects the 3D scene pass that dispatches the effect. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`enum_value EFFECT_CALLBACK_TYPE_MAX [EffectCallbackType] = 5`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Upper-bound sentinel for the 3D scene callback stages. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`enum_value EFFECT_CALLBACK_TYPE_POST_OPAQUE [EffectCallbackType] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Callback after the 3D opaque geometry pass. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`enum_value EFFECT_CALLBACK_TYPE_POST_SKY [EffectCallbackType] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Callback after the 3D sky pass. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`enum_value EFFECT_CALLBACK_TYPE_POST_TRANSPARENT [EffectCallbackType] = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Callback after the 3D transparent geometry pass. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`enum_value EFFECT_CALLBACK_TYPE_PRE_OPAQUE [EffectCallbackType] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Callback before the 3D opaque geometry pass. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`enum_value EFFECT_CALLBACK_TYPE_PRE_TRANSPARENT [EffectCallbackType] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Callback before the 3D transparent geometry pass. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`method _render_callback(int effect_callback_type, RenderData render_data) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Receives the selected 3D scene pass type and live RenderData at dispatch. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`property bool access_resolved_color`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Requests an MSAA color resolve at a 3D scene callback stage. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`property bool access_resolved_depth`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Requests an MSAA depth resolve at a 3D scene callback stage. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`property int effect_callback_type [CompositorEffect.EffectCallbackType]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Selects the 3D scene pass that dispatches the effect. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`property bool enabled`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Controls which effects in the 3D scene compositor are eligible for dispatch. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`property bool needs_motion_vectors`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Requests the 3D scene velocity attachment. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`property bool needs_normal_roughness`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Requests the 3D scene normal/roughness attachment. [ADR 0004](../../decisions/product.md#adr-0004) |
| [`property bool needs_separate_specular`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CompositorEffect.xml) | — | Excluded | Requests the separate 3D scene specular attachment. [ADR 0004](../../decisions/product.md#adr-0004) |
