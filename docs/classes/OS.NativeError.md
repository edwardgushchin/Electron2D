# OS.NativeError

Last updated: 2026-10-06

**Scope:** private implementation. **Source:** [source](../../src/Core/OS/OS.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Private sequential GError view with domain, code and borrowed UTF-8 message. MoveToTrash copies the error message before freeing the native error; no pointer escapes the service.

This type has no caller-facing construction or API. The connected FileDialog/native/isolated-trash checks exercise its owner; foreign native profiles remain separate acceptance gates.
