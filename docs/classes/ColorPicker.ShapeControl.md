# ColorPicker.ShapeControl

Last updated: 2026-10-06

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/ColorPicker.Shapes.cs). **Component:** [Color authoring](../components/color-authoring.md).

Retained color-space mesh, cursor and themed focus; routed pointer/keyboard/controller editing and gesture/repeat cancellation.

This type has no separate public constructor or backend. Connected color authoring tests exercise its owning consumer; inherited native semantic/editor/foreign-target gates remain separate.
