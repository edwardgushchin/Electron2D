# OS

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.OS`. **Inherits:** `ElectronObject`. **Inherited By:** —. **Source:** [source](../../src/Core/OS/OS.cs). **Component:** [File dialogs](../components/file-dialogs.md).

Retained permanent process operating-system service with static operations and named Engine lookup. It cannot be independently constructed, disposed or unregistered. MoveToTrash uses system GIO on desktop Linux with native recovery metadata/mount/link policies; no permanent deletion fallback exists. ShellOpen delegates URI/path requests to SDL. Linux ShellShowInFileManager opens the appropriate folder URI; Windows/macOS native selection requires its backend.

Empty/null/NUL inputs fail before external work. Recoverable trash failure throws IOException or unavailable-service NotSupportedException. Launch failure throws InvalidOperationException. These blocking native operations are unsuitable for real-time callbacks. FileDialog invokes trash only after its confirmation; the caller decides when to launch an external application.

The isolated XDG child verifies recovery content/metadata and symlink-target preservation. External application launch, native memory and foreign platforms remain unverified. See [file-dialog component](../components/file-dialogs.md).

## Methods

| Complete declaration | Contract |
| --- | --- |
| `public static System.Void MoveToTrash(System.String path)` | Moves a file, directory or symbolic link to the desktop trash without permanently deleting it. path: An ordinary, res:// or user:// path identifying the entry itself. System.ArgumentException: The path is empty or contains a null character. System.ArgumentNullException: The path is null. System.NotSupportedException: The platform or system trash service is unavailable. System.IO.IOException: The native service cannot trash the entry. |
| `public static System.Void ShellOpen(System.String uri)` | Requests the platform's default application for a URI or filesystem resource. uri: A nonempty URI or ordinary filesystem path. System.ArgumentException: The resource identifier is empty or contains a null character. System.InvalidOperationException: The native platform rejects the launch request. |
| `public static System.Void ShellShowInFileManager(System.String fileOrDirPath, System.Boolean openFolder = true)` | Requests the file manager for a file or directory. fileOrDirPath: An ordinary filesystem path. openFolder: Enters a directory when true; otherwise opens its parent. System.NotSupportedException: The profile requires a native item-selection backend. |
| `protected override System.Void ValidateDisposal()` | Inherited dialog/tree/scene lifecycle extension point; see the owning base reference. |

## Storage, verification and decisions

FileDialogTests, DisplayServerDialogTests and FileDialogRenderingTests exercise the connected owner workflow, saved fresh-process scenes, current Linux native keyboard/pointer/pixels and retained rendering. Their exact measured boundaries and absent native chooser/platform prerequisites are recorded in the [component](../components/file-dialogs.md). [ADR 0051](../decisions/enum-identities.md#adr-0051) controls enum identity; [ADR 0095](../decisions/singleton-services.md#adr-0095) controls static retained service access.
