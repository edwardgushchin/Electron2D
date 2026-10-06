# FileDialog.DisplayModeType

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.FileDialog.DisplayModeType`. **Inherits:** `System.Enum`. **Inherited By:** —. **Source:** [source](../../src/Scene/GUI/FileDialog.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Thumbnail and list layouts use the existing ItemList backend. FileDialog theme icons/colors/thumbnail_size and shared borrowed-texture callbacks determine visuals. Invalid selection values throw ArgumentOutOfRangeException.

## Values

| Complete declaration | Contract |
| --- | --- |
| `public const Electron2D.FileDialog.DisplayModeType List = 1` | Shows one row per file with a leading icon. |
| `public const Electron2D.FileDialog.DisplayModeType Thumbnails = 0` | Shows icons above file names. |

## Storage, verification and decisions

FileDialogTests, DisplayServerDialogTests and FileDialogRenderingTests exercise the connected owner workflow, saved fresh-process scenes, current Linux native keyboard/pointer/pixels and retained rendering. Their exact measured boundaries and absent native chooser/platform prerequisites are recorded in the [component](../components/file-dialogs.md). [ADR 0051](../decisions/enum-identities.md#adr-0051) controls enum identity; [ADR 0095](../decisions/singleton-services.md#adr-0095) controls static retained service access.
