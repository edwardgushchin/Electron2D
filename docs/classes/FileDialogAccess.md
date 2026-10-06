# FileDialogAccess

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.FileDialogAccess`. **Inherits:** `System.Enum`. **Inherited By:** —. **Source:** [source](../../src/Scene/GUI/FileDialogAccess.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Selects resource, user-data or ordinary filesystem directory owners. Changing FileDialog.Access resets boundary/history/filename and navigates to the virtual root or ordinary filesystem working directory. This value contract is separate from file open/permission flags.

## Values

| Complete declaration | Contract |
| --- | --- |
| `public const Electron2D.FileDialogAccess FileSystem = 2` | Browses ordinary filesystem paths and drives. |
| `public const Electron2D.FileDialogAccess Resources = 0` | Browses the configured project resource root. |
| `public const Electron2D.FileDialogAccess UserData = 1` | Browses the configured application user-data root. |

## Storage, verification and decisions

FileDialogTests, DisplayServerDialogTests and FileDialogRenderingTests exercise the connected owner workflow, saved fresh-process scenes, current Linux native keyboard/pointer/pixels and retained rendering. Their exact measured boundaries and absent native chooser/platform prerequisites are recorded in the [component](../components/file-dialogs.md). [ADR 0051](../decisions/enum-identities.md#adr-0051) controls enum identity; [ADR 0095](../decisions/singleton-services.md#adr-0095) controls static retained service access.
