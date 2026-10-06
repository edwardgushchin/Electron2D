# OS.TrashNative

Last updated: 2026-10-06

**Scope:** private implementation. **Source:** [source](../../src/Core/OS/OS.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Private lazy system GIO loader. It retains the library for process lifetime and resolves only GFile creation/trash, GObject unref and GError free. Failed construction releases the library. Calls are blocking filesystem work, not rendering/audio callbacks.

This type has no caller-facing construction or API. The connected FileDialog/native/isolated-trash checks exercise its owner; foreign native profiles remain separate acceptance gates.
