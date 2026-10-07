# Tree.OwnedVScroll

Last updated: 2026-10-07

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/Tree.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

Required internal scrollbar. Direct disposal is rejected while its Tree owner is live; owner shutdown disposes it with the scene hierarchy. It is exercised through TreeTests and has no independent public construction or backend.
