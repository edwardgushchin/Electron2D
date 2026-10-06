# TextEdit.OwnedVScroll

Last updated: 2026-10-07

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/TextEdit.Menu.cs). **Component:** [Multiline editing](../components/multiline-editing.md).

Required vertical scrollbar with parent disposal guard. It participates in its owning editor/highlighter lifetime and the connected TextEditTests workflows; it has no independent public construction or backend.
