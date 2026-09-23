# ParallaxLayer API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/ParallaxLayer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ParallaxLayer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public sealed class Electron2D.ParallaxLayer`](../../classes/ParallaxLayer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ParallaxLayer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ParallaxLayer.xml) | [`public sealed class Electron2D.ParallaxLayer`](../../classes/ParallaxLayer.md) | Partial | Legacy canvas layer and direct spatial children follow pinned parallax_background.cpp/parallax_layer.cpp camera scrolling, zoom policy, mirroring and scene storage. Managed and native root Window checks pass. Editor behavior, independent/offscreen viewports and full canvas interpolation are missing or unaudited; no complete type parity is claimed. |
| [`property Vector2 motion_mirroring = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ParallaxLayer.xml) | [`public Electron2D.Vector2 MotionMirroring { get; set; }`](../../classes/ParallaxLayer.md) | Partial | Pinned motion setters and one-extra-copy mirrored canvas submission execute through retained drawing; managed state and Linux dummy/Wayland pixels cover camera motion, original scale and packing. Editor and independent viewport behavior remain unaudited. |
| [`property Vector2 motion_offset = Vector2(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ParallaxLayer.xml) | [`public Electron2D.Vector2 MotionOffset { get; set; }`](../../classes/ParallaxLayer.md) | Partial | Pinned motion setters and one-extra-copy mirrored canvas submission execute through retained drawing; managed state and Linux dummy/Wayland pixels cover camera motion, original scale and packing. Editor and independent viewport behavior remain unaudited. |
| [`property Vector2 motion_scale = Vector2(1, 1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ParallaxLayer.xml) | [`public Electron2D.Vector2 MotionScale { get; set; }`](../../classes/ParallaxLayer.md) | Partial | Pinned motion setters and one-extra-copy mirrored canvas submission execute through retained drawing; managed state and Linux dummy/Wayland pixels cover camera motion, original scale and packing. Editor and independent viewport behavior remain unaudited. |
| [`property int physics_interpolation_mode [Node.PhysicsInterpolationMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ParallaxLayer.xml) | — | Blocked | Trigger: implement Node.PhysicsInterpolationMode and render-pose interpolation in the scene and canvas backends; ParallaxLayer must default the inherited policy to Off. No inert per-type property is exposed (ADR 0008). |
