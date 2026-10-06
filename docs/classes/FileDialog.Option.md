# FileDialog.Option

Last updated: 2026-10-06

**Scope:** private implementation. **Source:** [source](../../src/Scene/GUI/FileDialog.Options.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Private copied option schema with raw name, choice captions and clamped default. Identity guards prevent removed controls from writing newer option results. The dialog owns its lifetime.

This type has no caller-facing construction or API. The connected FileDialog/native/isolated-trash checks exercise its owner; foreign native profiles remain separate acceptance gates.
