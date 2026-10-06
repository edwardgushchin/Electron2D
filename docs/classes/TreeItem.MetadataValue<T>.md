# TreeItem.MetadataValue<T>

Last updated: 2026-10-07

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/TreeItem.Values.cs). **Component:** [Hierarchical cells](../components/hierarchical-cells.md).

Exact generic runtime metadata wrapper. Stored privately; retrieval requires the matching T and preserves borrowed payload ownership. It is exercised through TreeTests and has no independent public construction or backend.
