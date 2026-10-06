# ColorPickerButton.OwnedPicker

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/ColorPickerButton.cs). **Component:** [Color authoring](../components/color-authoring.md).

Required ColorPicker subclass rejects independent disposal while its button owner remains live.

This type has no separate public constructor or backend. Connected color authoring tests exercise its owning consumer; inherited native semantic/editor/foreign-target gates remain separate.
