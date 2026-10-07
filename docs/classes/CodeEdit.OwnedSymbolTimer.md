# CodeEdit.OwnedSymbolTimer

Last updated: 2026-10-07

**Scope:** private implementation. **Source:** [source](../../src/Scene/GUI/CodeEdit.cs). **Component:** [Code authoring](../components/code-authoring.md).

Required internal scene Timer. Independent disposal is rejected while its owner lives; owner shutdown disposes it with the hierarchy. Exercised through CodeEditTests; there is no separate public factory.
