# Windows recoverable trash

Last updated: 2026-11-06

OS.MoveToTrash now supplies the existing [file-dialog action](file-dialogs.md) on Windows. Files, populated directories and NTFS junctions use the Shell recycle service; source names/data, collision identities and restoration metadata remain native. Ordinary, relative, res:// and user:// paths retain the existing path boundary. No public API, package dependency or native handle is added.

## Ownership and failure

Each blocking request creates a private STA worker, leaving an STA/MTA caller's apartment unchanged. COM initialization, the file-operation object, source shell identity and progress sink are released on every exit. The sink uses the SDK's generated COM support, including NativeAOT; client pointers remain internal. System DLL imports are restricted to System32. Independent callers retain separate operations. These cold filesystem operations make no real-time allocation claim.

The operation requests recycle-only/undo behavior, disables confirmation/progress/error/elevation UI, excludes connected HTML companion files and stops at the first error. The progress callback rejects any delete step lacking the recycle flag. Completion checks the native operation result, aborted state, item result and a non-null newly created recycle identity. A missing Shell class/interface reports NotSupportedException; native failures and an offered permanent-delete step report IOException. There is no permanent-delete fallback.

Windows can offer permanent deletion for paths it will not recycle. The exercised host does this for long paths in a longPathAware process. The guarded independent native reference reproduces it; OS.MoveToTrash refuses and preserves the source. An unavailable recycle destination is an error, not an assertion of universal path/volume support. Native junction recycling preserves the target. FileDialog retains its confirmation/error handling and refreshes already committed selections after a later item fails.

## Verification

[WindowsTrash.Consumer](../../tests/WindowsTrash.Consumer/README.md) uses public API for Unicode/punctuation, exact binary bytes, readonly files, duplicate names/versions, long-path refusal, directories/junctions, relative/virtual paths, STA/MTA, concurrent requests, sharing failure/retry and the browser flow. The browser scenario drives a managed SceneTree's real input/confirmation/error/list refresh; it does not claim rendered-window or physical UI acceptance.

The independent C++ oracle enumerates the actual Shell recycle folder and matches the exact original path from native $I metadata, including extended prefixes. It compares the retained payload and invokes the native undelete verb, checking that the matching entry disappears. Its manifest matches .NET longPathAware behavior. Only controlled fixture paths are restored; it never empties the user's recycle bin. Fresh managed-package and strict full-trim NativeAOT consumers exercise the same workflow. The preceding managed engine refuses Windows trash without changing its source.

Receipts and identities are retained under `C:/Users/eduar/.codex/artifacts/Electron2D/windows-trash-20261106`. The exercised host is Windows x64. Linux GIO retains its existing implementation; macOS/mobile/browser trash, other Windows architectures/volumes, physical UI, native heap totals and the full ADR 0021 delivery matrix remain unqualified. Declaration totals do not change.

The native contract follows Microsoft's [operation flags](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-setoperationflags), [pre-delete veto](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperationprogresssink-predeleteitem), [post-delete recycle identity](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperationprogresssink-postdeleteitem) and [aborted-operation query](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-getanyoperationsaborted).
