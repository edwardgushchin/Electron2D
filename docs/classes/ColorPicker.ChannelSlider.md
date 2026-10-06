# ColorPicker.ChannelSlider

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/ColorPicker.Shapes.cs). **Component:** [Color authoring](../components/color-authoring.md).

Existing HSlider with retained RGB/HSV/linear/OKHSL/alpha gradient drawing; Range identity is shared with the required SpinBox.

This type has no separate public constructor or backend. Connected color authoring tests exercise its owning consumer; inherited native semantic/editor/foreign-target gates remain separate.
