# FileDialog.Customization

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.FileDialog.Customization`. **Inherits:** `System.Enum`. **Inherited By:** —. **Source:** [source](../../src/Scene/GUI/FileDialog.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Nine feature identities retain their exact numeric values. Each defaults to enabled and controls the actual input/context command or validation in FileDialog. Disabling a feature does not remove the independently programmable public API. Unknown values throw ArgumentOutOfRangeException.

## Values

| Complete declaration | Contract |
| --- | --- |
| `public const Electron2D.FileDialog.Customization CreateFolder = 1` | Folder creation. |
| `public const Electron2D.FileDialog.Customization Delete = 8` | Recoverable trash command. |
| `public const Electron2D.FileDialog.Customization Favorites = 4` | Shared favorite directories. |
| `public const Electron2D.FileDialog.Customization FileFilter = 2` | Filename substring filtering. |
| `public const Electron2D.FileDialog.Customization FileSort = 3` | File ordering. |
| `public const Electron2D.FileDialog.Customization HiddenFiles = 0` | Hidden-file toggle. |
| `public const Electron2D.FileDialog.Customization Layout = 6` | Thumbnail/list layout toggle. |
| `public const Electron2D.FileDialog.Customization OverwriteWarning = 7` | Existing destination confirmation. |
| `public const Electron2D.FileDialog.Customization Recent = 5` | Shared recent directories. |

## Storage, verification and decisions

FileDialogTests, DisplayServerDialogTests and FileDialogRenderingTests exercise the connected owner workflow, saved fresh-process scenes, current Linux native keyboard/pointer/pixels and retained rendering. Their exact measured boundaries and absent native chooser/platform prerequisites are recorded in the [component](../components/file-dialogs.md). [ADR 0051](../decisions/enum-identities.md#adr-0051) controls enum identity; [ADR 0095](../decisions/singleton-services.md#adr-0095) controls static retained service access.
