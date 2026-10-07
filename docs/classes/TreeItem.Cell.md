# TreeItem.Cell

Last updated: 2026-10-07

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/TreeItem.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

Retained typed cell configuration, borrowed graphics, metadata and custom draw delegate with reusable TextLayout/BIDI buffers. Column resize or item disposal releases references. It is exercised through TreeTests and has no independent public construction or backend.
