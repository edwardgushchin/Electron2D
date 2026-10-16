# OS

Last updated: 2026-10-17

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

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

<a id="getsystemfontpath"></a>
`public static System.String GetSystemFontPath(System.String fontName, System.Int32 weight = 400, System.Int32 stretch = 100, System.Boolean italic = false)`

Returns copied catalog family names, the closest style file or ordered files contributing text/locale coverage. These are cold static queries independent of a graphics session. Empty/absent catalog results are explicit; null/NUL or queries above 65536 UTF16 units fail. Weight clamps to 100..999 and stretch to 50..200. Linux script metadata is accepted while actual coverage/locale drives matching. Current Linux uses optional host Fontconfig; other provider gates remain in coverage.

<a id="getsystemfontpathfortext"></a>
`public static System.String[] GetSystemFontPathForText(System.String fontName, System.String text, System.String locale = "", System.String script = "", System.Int32 weight = 400, System.Int32 stretch = 100, System.Boolean italic = false)`

Returns copied catalog family names, the closest style file or ordered files contributing text/locale coverage. These are cold static queries independent of a graphics session. Empty/absent catalog results are explicit; null/NUL or queries above 65536 UTF16 units fail. Weight clamps to 100..999 and stretch to 50..200. Linux script metadata is accepted while actual coverage/locale drives matching. Current Linux uses optional host Fontconfig; other provider gates remain in coverage.

<a id="getsystemfonts"></a>
`public static System.String[] GetSystemFonts()`

Returns copied catalog family names, the closest style file or ordered files contributing text/locale coverage. These are cold static queries independent of a graphics session. Empty/absent catalog results are explicit; null/NUL or queries above 65536 UTF16 units fail. Weight clamps to 100..999 and stretch to 50..200. Linux script metadata is accepted while actual coverage/locale drives matching. Current Linux uses optional host Fontconfig; other provider gates remain in coverage.

## Independent process operations

OS process operations use the retained static service. CreateProcess starts a literal executable/argv independently; CreateInstance restarts the current application, including the entry assembly for a framework-dependent host. Execute blocks and appends one captured string after exit, preserving existing collection items. POSIX stdout/stderr capture merges into one native stream in write order; nonzero exit codes remain ordinary results. Arguments never enter an implicit shell. GetExecutablePath returns the actual native host path, GetCommandLineArgs copies application arguments, and GetCommandLineUserArgs copies the suffix after the first -- or ++ separator.

ExecuteWithPipe returns ProcessPipes with PID, owned bidirectional StandardIO and read-only StandardError FileAccess streams. Pipes are non-seekable. Blocking I/O waits on the caller; nonblocking ReadBytes returns an available prefix immediately without mistaking NotReady for EOF. Exact reads can consume an incomplete prefix before throwing IOException; use byte buffers for incremental framing. A partial write throws PipeWriteException.BytesWritten so retries send only the remainder. Closing/disposal releases streams without killing the independent process. Native SafeHandles also release forgotten streams. IsProcessRunning queries native IDs, GetProcessExitCode retains the most recently owned child status, and Kill terminates only one process, never its descendants. IDs may be reused by the OS. Completed children release native/BCL handles; a small exit-history record remains in the service.

This is cold, blocking/allocating process and I/O work. It is unsuitable for real-time callbacks. The runtime does not impose application-specific deadlines or kill policy. Tooling hosts still own cancellation, deadlines, diagnostics and project orchestration under ADR 0090. Linux/.NET10 with SDL3.4.18 is exercised. Mobile launch adapters, macOS .app selection and Windows shared-output/explicit-new-console launch remain exact dependencies. Arbitrary named pipe:// paths remain unavailable; these pipe://PID/channel values identify already-owned streams and do not create a named-pipe connector. See [executable checks and examples](../components/processes.md).

## Process method reference

| Declaration | Contract |
| --- | --- |
| `public static System.Int32 CreateInstance(System.ReadOnlySpan<System.String> arguments)` | [CreateInstance](#createinstance) |
| `public static System.Int32 CreateProcess(System.String path, System.ReadOnlySpan<System.String> arguments, System.Boolean openConsole = false)` | [CreateProcess](#createprocess) |
| `public static System.Int32 Execute(System.String path, System.ReadOnlySpan<System.String> arguments, System.Collections.Generic.ICollection<System.String> output = null, System.Boolean readStderr = false, System.Boolean openConsole = false)` | [Execute](#execute) |
| `public static Electron2D.ProcessPipes ExecuteWithPipe(System.String path, System.ReadOnlySpan<System.String> arguments, System.Boolean blocking = true)` | [ExecuteWithPipe](#executewithpipe) |
| `public static System.Int32 GetProcessID()` | [GetProcessID](#getprocessid) |
| `public static System.String GetExecutablePath()` | [GetExecutablePath](#getexecutablepath) |
| `public static System.String[] GetCommandLineArgs()` | [GetCommandLineArgs](#getcommandlineargs) |
| `public static System.String[] GetCommandLineUserArgs()` | [GetCommandLineUserArgs](#getcommandlineuserargs) |
| `public static System.Int32 GetProcessExitCode(System.Int32 pid)` | [GetProcessExitCode](#getprocessexitcode) |
| `public static System.Boolean IsProcessRunning(System.Int32 pid)` | [IsProcessRunning](#isprocessrunning) |
| `public static System.Void Kill(System.Int32 pid)` | [Kill](#kill) |

### Process method descriptions


<a id="createinstance"></a>
`public static System.Int32 CreateInstance(System.ReadOnlySpan<System.String> arguments)`

Restarts the current native application; a dotnet host prepends its entry assembly. New arguments replace the original argv. Returns child PID; launch failure throws. Android application activity launch is unavailable.

<a id="createprocess"></a>
`public static System.Int32 CreateProcess(System.String path, System.ReadOnlySpan<System.String> arguments, System.Boolean openConsole = false)`

Starts a literal executable and separate ordered arguments; no implicit shell. Virtual project/user paths resolve before launch. Returns a native PID; failures throw before result publication. The child survives renderer/application shutdown. macOS .app/Android launch and exact Windows new-console adapters remain dependencies.

<a id="execute"></a>
`public static System.Int32 Execute(System.String path, System.ReadOnlySpan<System.String> arguments, System.Collections.Generic.ICollection<System.String> output = null, System.Boolean readStderr = false, System.Boolean openConsole = false)`

Blocks until actual exit, captures/discards omitted stdout and disabled stderr, and appends one complete output string to a supplied collection. Existing items remain. POSIX stderr merging preserves native write order; large output is drained before exit polling. Windows readStderr=true explicitly rejects until its shared-output adapter. Output allocation and unbounded child duration are caller policy.

<a id="executewithpipe"></a>
`public static Electron2D.ProcessPipes ExecuteWithPipe(System.String path, System.ReadOnlySpan<System.String> arguments, System.Boolean blocking = true)`

Starts native owned stdin/stdout and stderr streams, returning ProcessPipes. Blocking I/O waits on the caller. Nonblocking ReadBytes returns available prefixes and keeps EOF false for temporary readiness; partial writes throw PipeWriteException with BytesWritten. Closing pipes does not kill the child. Streams have no cursor/length/resize/whole-file snapshot. Mobile and .app adapters remain dependencies.

<a id="getprocessid"></a>
`public static System.Int32 GetProcessID()`

Returns Environment.ProcessId, the actual current native process identity.

<a id="getexecutablepath"></a>
`public static System.String GetExecutablePath()`

Returns Environment.ProcessPath, the actual native executable (dotnet for framework-dependent applications); unavailable hosts throw.

<a id="getcommandlineargs"></a>
`public static System.String[] GetCommandLineArgs()`

Returns a new string array of application arguments in original literal order, excluding the executable/entry assembly. No shell decoding or mutation of process state.

<a id="getcommandlineuserargs"></a>
`public static System.String[] GetCommandLineUserArgs()`

Returns an independent suffix after the first -- or ++ separator; absent separator yields an empty array. Later separators remain literal arguments.

<a id="getprocessexitcode"></a>
`public static System.Int32 GetProcessExitCode(System.Int32 pid)`

For a positive owned PID, polls/caches actual completed status; returns -1 while running; unknown children throw ArgumentException. Exit history refers to the most recently created child with that PID and retains small metadata after native handles release. Invalid nonpositive IDs throw.

<a id="isprocessrunning"></a>
`public static System.Boolean IsProcessRunning(System.Int32 pid)`

Queries a positive PID. Tracked children are polled/reaped before native lookup; IDs reused by external processes resolve current running state. Missing processes return false; denied/unavailable platform inspection may throw.

<a id="kill"></a>
`public static System.Void Kill(System.Int32 pid)`

Forces termination of one positive native PID; descendants survive. Owned BCL/native handles and external native process lookup execute. Missing/denied targets throw. A later poll reaps exit status; PID reuse is an OS race callers must consider.

## Process environment and directories

GetEnvironment, HasEnvironment, SetEnvironment and UnsetEnvironment manage process-local variables, including empty values, with native SDL child inheritance. GetConfigDir, GetDataDir, GetCacheDir and GetTempDir expose host paths; GetUserDataDir returns the current user:// root. GetSystemDir accepts the nested SystemDir enum and native user-directory policy. Queries do not create directories or grant access. See [behavior, failures and executable verification](../components/os-environment.md).

| API | Contract |
| --- | --- |
| GetEnvironment(string variable) / HasEnvironment(string variable) | Empty result for absent variables; existence distinguishes an empty value. |
| SetEnvironment(string variable, string value) / UnsetEnvironment(string variable) | Update managed and SDL environments for subsequent children; process-local, validated names and literal values. |
| GetConfigDir() / GetDataDir() / GetCacheDir() / GetTempDir() | Global host paths, with absolute XDG overrides on Linux. |
| GetUserDataDir() | Exact active ProjectSettings.UserDataRoot. |
| GetSystemDir(SystemDir dir, bool sharedStorage = true) | Native Desktop, DCIM, Documents, Downloads, Movies, Music, Pictures and Ringtones roles; explicit unsupported-host/role errors. |

## Standard handles and input

GetSTDINType, GetSTDOUTType and GetSTDERRType return the nested StdHandleType enum. ReadBufferFromSTDIN reads actual owned bytes, filling pipe/file requests or returning a terminal line/prefix. ReadStringFromSTDIN reads a bounded UTF-8 line, normalizes CRLF and removes trailing LF without taking the next line. These cold blocking operations borrow the process handle and reject invalid sizes/handles or invalid UTF-8 explicitly. See [complete semantics, native adapters and child-process checks](../components/standard-input.md).

## Restart on normal process exit

| Complete declaration | Contract |
| --- | --- |
| `public static void SetRestartOnExit(bool restart, ReadOnlySpan<string> arguments = default)` | Copies and validates literal arguments, then replaces the final request. |
| `public static bool IsRestartOnExitSet()` | Thread-safe flag read; false initially. |
| `public static string[] GetRestartOnExitArguments()` | Independent retained argument snapshot, initially empty. |

OS restart-on-exit configuration is a process-wide, desktop-only request with a copied literal argument list. SetRestartOnExit atomically replaces both values; IsRestartOnExitSet reads the flag and GetRestartOnExitArguments returns an independent array, including while disabled. Invalid Unicode, NUL or null elements fail before publication. A single normal managed process-shutdown handler captures the final request and uses CreateInstance. A standalone host must finish its loop cleanup and return; stopping a borrowed loop or returning from Engine.Run alone does not exit its host. The replacement process starts with disabled restart state. Failed native launch reports to standard error and preserves the original exit code; crashes and forced termination do not guarantee shutdown callbacks.

### SetRestartOnExit

Arguments replace the entire previous list. Omitting them launches the application with no application arguments. To preserve the original arguments, explicitly pass GetCommandLineArgs(). Disabling retains the supplied snapshot. Setter and getters may be called from worker threads. Once normal shutdown has captured the request, a setter throws InvalidOperationException. Non-desktop profiles throw PlatformNotSupportedException for this missing process service. ArgumentNullException identifies a null element, ArgumentException a NUL element, and EncoderFallbackException invalid Unicode; none changes the committed request.

### IsRestartOnExitSet and GetRestartOnExitArguments

Both read the retained OS service under its process lock. The flag query allocates no managed memory after warmup. Arguments are intentionally copied; changing the returned array does not change restart. The configured state belongs only to this process. CreateInstance handles both application executables and framework-dependent entry assemblies without shell interpretation.

```csharp
OS.SetRestartOnExit(true, OS.GetCommandLineArgs());
Environment.ExitCode = Engine.Run(window);
// Return from the standalone application entry point after the owned runtime ends.
```

The fragment assumes an authored root Window and a standalone desktop entry point. OSRestartTests launches independent dotnet/apphost children to verify literal empty/Unicode/quote/separator arguments, replacement/cancellation, new process identity, default state and original exit status. Its native profile verifies real GPU/compatibility pixels and runtime cleanup in both applications. Other desktop targets, editor launch behavior and abnormal termination are not verified.

A restart attempt clears its retained flag and arguments before subsequent managed shutdown handlers run. Shutdown capture rejects later setters. A controlled Linux apphost fixture removes only its copied executable and verifies actual launch-failure reporting without changing the original exit status.

The native host check covers both an explicit SceneTree.Quit and a queued native WM-close event. In either case the old host has completed Engine.Run cleanup before normal process shutdown launches the replacement.

CreateInstance resolves a framework-dependent entry DLL by assembly name in AppContext.BaseDirectory, verifies that the file exists, and launches with literal arguments. Application executables relaunch directly. The path no longer depends on Assembly.Location.


Runtime messages now use [diagnostic delivery](../components/diagnostics.md). Engine output flags affect its physical stream; typed logger callbacks retain delivery. Host Console writers retain ordinary C# semantics.

`AddLogger(Logger)`/`RemoveLogger(Logger)` retain registration identity. `LogMessage(string, bool)` and `LogError(string, LoggerErrorType, string, string, string, int)` provide typed message/error ingress with compiler caller metadata.


## Keyboard names

| Declaration | Contract |
| --- | --- |
| `public static string GetKeycodeString(Key code)` | Formats the embedded modifier flags and a named key or Unicode scalar. None produces an empty string; invalid unnamed scalars produce the replacement character. |
| `public static Key FindKeycodeFromString(string text)` | Parses a case-insensitive named final key and recognized plus-separated modifier prefixes. Unknown final names contribute None; unknown prefixes are ignored. Null throws ArgumentNullException. |
| `public static bool IsKeycodeUnicode(int code)` | Classifies the exact unmodified key identity as character-capable. Keypad characters pass; designated editing/navigation/function/system keys do not. |

These operations require no display or renderer and permit independent caller threads.
Their names are stable platform vocabulary, independent of translation and current culture.
Control/Meta/Alt names follow the host platform. Logical CommandOrControl formatting
resolves to the platform modifier; parsing that displayed name returns the concrete modifier.
The parser does not trim input or infer unnamed Unicode characters. Unknown is a known
sentinel name and returns Key.Unknown. The character classifier is not Unicode scalar
validation, produced-text prediction or a keyboard-layout query; it does not remove modifiers.

[The input component](../components/input-runtime.md#keyboard-name-conversion) describes
splitting a parsed chord into a typed persisted action binding. KeyCodeNamesTests checks the
191 named key identities and classifier data, modifier edge cases, thread/culture independence
and 1024 warmed zero-caller-byte lookups. Fresh-process JIT/NativeAOT consumers verify saved
project settings, exact action matching and press/release without native initialization.

<a id="getkeycodestring"></a>
`public static string GetKeycodeString(Key code)` masks the key identity and modifier
fields, emits modifiers in canonical Control/Alt/Shift/Meta order, then the key name.
CommandOrControl resolves at its platform-specific position. A modifier-only value
formats only those modifiers. String construction is a cold UI operation.

<a id="findkeycodefromstring"></a>
`public static Key FindKeycodeFromString(string text)` combines known Shift/Control/Alt/Meta
prefixes and the final named key. Repeated modifiers collapse; `Ctrl+missing` retains
Control with no key, and a literal plus uses the named key `Plus`. Split the code and
modifier masks before populating an InputEventKey or InputBindingSettings.

<a id="iskeycodeunicode"></a>
`public static bool IsKeycodeUnicode(int code)` tests the supplied identity directly.
None, Unknown and otherwise unlisted integer identities return true; this permissive
classification does not establish valid text. Use actual text-input events for produced characters.

```csharp
Key chord = OS.FindKeycodeFromString("Ctrl+Shift+K");
string label = OS.GetKeycodeString(chord);
bool characterKey = OS.IsKeycodeUnicode((int)Key.K);
```
