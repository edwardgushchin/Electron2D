# Tree.OwnedPopup

Last updated: 2026-10-07

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/Tree.Editing.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

Required embedded cell editor child. Uses the existing public control behavior and guarded Tree ownership; popup cancel and multiline commit are routed through the owner. It is exercised through TreeTests and has no independent public construction or backend.
