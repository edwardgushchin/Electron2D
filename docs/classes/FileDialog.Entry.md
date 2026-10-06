# FileDialog.Entry

Last updated: 2026-10-06

**Scope:** private implementation. **Source:** [source](../../src/Scene/GUI/FileDialog.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Immutable per-refresh file record storing name, scoped path, directory flag and modified time. ItemList metadata borrows this record until the next listing rebuild; directories precede filtered files.

This type has no caller-facing construction or API. The connected FileDialog/native/isolated-trash checks exercise its owner; foreign native profiles remain separate acceptance gates.
