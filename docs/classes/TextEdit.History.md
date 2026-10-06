# TextEdit.History

Last updated: 2026-10-07

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/TextEdit.cs). **Component:** [Multiline editing](../components/multiline-editing.md).

Copied document/caret/gutter/background/version snapshot for cold grouped undo/redo. It participates in its owning editor/highlighter lifetime and the connected TextEditTests workflows; it has no independent public construction or backend.
