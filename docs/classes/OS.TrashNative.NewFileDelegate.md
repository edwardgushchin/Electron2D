# OS.TrashNative.NewFileDelegate

Last updated: 2026-10-29

**Scope:** private implementation. **Source:** [source](../../src/Core/OS/OS.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Private Cdecl g_file_new_for_path ABI with a borrowed byte-pointer UTF8 input and an owned GFile result. TrashNative.NewFile retains scoped caller scratch or tracked heap storage until the call completes.

This type has no caller-facing construction or API. The connected FileDialog/native/isolated-trash checks exercise its owner; foreign native profiles remain separate acceptance gates.

[Input ownership](../coverage/performance-producers.md#legacy-platform-input-conversion-2026-10-29) records the temporary buffer boundary and the remaining provider/runtime limits.
