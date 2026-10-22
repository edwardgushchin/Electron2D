# TextEdit.GutterMetadata<T>

Last updated: 2026-10-07

**Scope:** internal/private implementation. **Source:** [source](../../src/Scene/GUI/TextEdit.Gutters.cs). **Component:** [Multiline editing](../components/multiline-editing.md).

Exact generic payload wrapper; mismatched retrieval rejects without untyped public access. It participates in its owning editor/highlighter lifetime and the connected TextEditTests workflows; it has no independent public construction or backend.
