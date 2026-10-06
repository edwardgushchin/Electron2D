# ColorPicker.SamplerPopup

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/ColorPicker.Sampler.cs). **Component:** [Color authoring](../components/color-authoring.md).

Owns the captured completed application image/texture, size fitting, scaled/clamped sampling and deterministic hide/disposal cleanup.

This type has no separate public constructor or backend. Connected color authoring tests exercise its owning consumer; inherited native semantic/editor/foreign-target gates remain separate.
