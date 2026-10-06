# OS.TrashNative.TrashDelegate

Last updated: 2026-10-06

**Scope:** private implementation. **Source:** [source](../../src/Core/OS/OS.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Private Cdecl g_file_trash ABI with a null cancellation object, native boolean result and owned error out-pointer.

This type has no caller-facing construction or API. The connected FileDialog/native/isolated-trash checks exercise its owner; foreign native profiles remain separate acceptance gates.
