# Public enum identities

<a id="adr-0051"></a>
## ADR 0051: Public enum identities and declaring owners

Last updated: 2026-10-06

### Status

Accepted.

### Context

Reference enum names are often scoped by their owner. In typed C#, repeating the same value contract under several owners creates incompatible public types and numeric casts. Conversely, equal numbers can belong to different contracts. ADR 0004 still requires each applicable value and behavior; placement and type identity are explicit C# API choices.

### Decision

One semantic value contract has one public enum type. Reuse that type across every public property, method, override and event with the same member meanings and valid-value set. A type used by several owners lives at the top level of the flat `Electron2D` namespace and has a neutral domain name. A contract specific to one owner may remain nested. Matching names or numeric values alone do not justify unification: different meanings or different valid-value sets retain distinct types. `Max` and similar nonselectable sentinels do not create a separate contract; callers reject them where selection is required. Foreign backend enums remain private to their adapters.

Alignment enums preserve the reference declaring owner as part of their public type identity, even when meanings and numeric values agree. `BoxContainer.AlignmentMode`, `AspectRatioContainer.AlignmentMode`, `FlowContainer.AlignmentMode` and `TabBar.AlignmentMode` remain separate nested types; `FlowContainer.LastWrapAlignmentMode` remains nested too. `HorizontalAlignment`, `VerticalAlignment` and the future `InlineAlignment` remain namespace-level types because their reference owner is the global scope. Future `RenderingServer.ParticlesTransformAlign`, `ParticlesTransformAlignAxis` and `ParticlesTransformAlignCustomSrc` belong to RenderingServer. This alignment-family rule overrides semantic unification; do not expose a namespace-level `AlignmentMode`, owner aliases or duplicate mirrors. Missing enums still require their executable owning slice; this placement rule does not authorize inert declarations. The coverage validator rejects exported alignment enums under a different owner before aliases are applied.

The current shared identities are `CursorShape`, `MouseMode`, `WindowMode`, `WindowFlag`, `AxisStretchMode`, `TextureStretchMode`, `ProcessPhase`, `RecursiveBehavior`, `Vector2Axis`, `Vector3Axis`, `Vector4Axis`, `AudioFFTSize`, `AudioLoopMode` and `FileDialogMode`. `FileDialogMode` supplies the same five selection meanings to FileDialog and DisplayServer; FileDialogAccess stays distinct from unrelated file access/open flags. Vector axes share types between floating-point and integer vectors of the same dimension; dimensions retain distinct valid-value sets. `AudioFFTSize` is the common 256/512/1024/2048/4096 transform-size preset with a nonselectable Max bound: `AudioEffectSpectrumAnalyzer` and `AudioEffectPitchShift` now both use it. The analyzer interprets the number as bins from a window twice that long; the pitch shifter uses a window of the named length. `AudioLoopMode` is shared by WAV metadata and immutable sample snapshots (Disabled/Forward/PingPong/Backward). `AudioDefaultPlaybackType` stays distinct from `AudioServer.PlaybackType`: project defaults have two values and no recursive Default selector. `Image.Format` and `AudioStreamWAV.Format`, for example, have different meanings and remain distinct despite their short name.

The following other selected names remain exact public type identities. Listed targets are top-level except where the target explicitly includes an owner:

- `CanvasItemMaterial.BlendMode` → `BlendMode`; `CanvasItem.TextureFilter` → `TextureFilter`; `CanvasItem.TextureRepeat` → `TextureRepeat`; `Window.Mode` → `WindowMode`.
- `Control.FocusMode` → `FocusMode`; `Control.LayoutPreset` → `LayoutPreset`; `Control.GrowDirection` → `GrowDirection`; `Control.LayoutDirection` → `LayoutDirection`; `Control.LayoutPresetMode` → `LayoutPresetMode`; `Control.MouseFilter` → `MouseFilter`.
- `FileAccess.CompressionMode` → `FileCompressionMode`; `FileAccess.ModeFlags` → `FileAccessModeFlags`; `FileAccess.UnixPermissionFlags` → `UnixPermissionFlags`; `Node.ProcessMode` → `ProcessMode`; `PackedScene.GenEditState` → `PackedSceneEditState`; `Resource.DeepDuplicateMode` → `DeepDuplicateMode`; `SceneTree.GroupCallFlags` → `GroupCallFlags`.
- `Gradient.InterpolationMode` → `InterpolationMode`; `GradientTexture2D.Fill` → `FillEnum`; `GradientTexture2D.Repeat` → `Repeat`; `Camera2D.AnchorMode` → `AnchorMode`; `Line2D.LineCapMode` → `LineCapMode`; `Line2D.LineJointMode` → `LineJointMode`; `Line2D.LineTextureMode` → `LineTextureMode`.
- `FastNoiseLite.NoiseType` → `NoiseType`; `FastNoiseLite.FractalType` → `FractalType`; `FastNoiseLite.CellularDistanceFunction` → `CellularDistanceFunction`; `FastNoiseLite.CellularReturnType` → `CellularReturnType`; `FastNoiseLite.DomainWarpType` → `DomainWarpType`; `FastNoiseLite.DomainWarpFractalType` → `DomainWarpFractalType`.

The owning class keeps its applicable property names. Enum numeric values and observable behavior do not change with location. Do not ship former type spellings, compatibility aliases or duplicate public mirrors. Keep the bidirectional coverage mappings, source XML, consumers and class pages synchronized with these identities. When a new enum is proposed, compare its meaning and valid values with existing public enums before declaring a new type.

### Consequences

Moving or unifying public enum types breaks source and binary compatibility. The value-contract rule governs new enum identities; it does not move unrelated owner-specific types. ADR 0045 continues to govern acronyms in function, method and property names, not enum type names.
