# ADR 0022: Typed directory access and scoped filesystem mutation

Last updated: 2026-09-21

## Status

Accepted.

## Context

Electron2D already has typed file streams, directory-backed virtual paths, managed lifetime, and standard-exception error reporting, but no directory cursor or mutation API. The reference `DirAccess` surface combines current-directory state, streaming enumeration, static absolute helpers, temporary ownership, drive queries, links, and platform-specific filesystem identity. Reproducing its numeric errors, `RefCounted` protocol, thread-local last-open error, or packed-resource behavior would contradict accepted decisions or advertise absent domains.

The runtime target matrix also requires platform gaps to fail explicitly instead of returning plausible empty values.

## Decision

`DirAccess` is a sealed `ElectronObject` in the existing Core file-access component and `Electron2D.dll`. It has a private constructor and non-null `Open`/`CreateTemp` factories. Managed memory remains runtime-owned; `IDisposable` closes listing state and deterministically deletes non-kept temporary directories.

Standard exceptions replace numeric `Error` results. There is no `GetOpenError`, last-error slot, or nullable factory result. Sorted string collections are immutable `IReadOnlyList<string>` snapshots.

Each instance captures one of three scopes at open time: physical filesystem, project resources, or user data. Virtual instances capture their concrete root and cannot switch scope or escape it lexically, even if process project roots are later reconfigured. Two-path static operations capture both current virtual roots under one `ProjectSettings` lock before resolving either argument.

One private lock serializes current directory, include flags, listing snapshot/cursor, last-entry kind, temporary ownership, and disposal. `ListDirBegin` replaces an active listing, `GetNext` auto-closes at EOF, `ListDirEnd` is idempotent, and `ChangeDir` closes the listing after a successful transition. Streaming order remains filesystem-defined; snapshot helpers sort ordinally.

Public removal is deliberately nonrecursive and permanent. It removes one file, link/reparse point, or empty directory. Recursive deletion exists only during disposal of the exact canonical root created and owned by `CreateTemp`; directory links are removed rather than traversed.

Link creation/detection/reading is enabled only on Linux, Windows, and macOS until mobile native-host behavior is verified. Relative link sources retain native relative-link semantics and may be dangling; virtual and absolute sources are stored as physical absolute targets. Windows can require Developer Mode or elevation; detection recognizes every reparse point while reading is limited to symbolic-link and mount-point targets exposed by the runtime. `IsEquivalent` compares native file identity so symbolic and hard links are handled, using Win32 handle metadata or Unix device/inode data. `IsCaseSensitive` uses platform metadata rather than mutating the queried directory.

Drive enumeration uses a private platform snapshot: Windows logical drives with native labels, visible macOS mounts, and Linux `/dev` mounts plus home, desktop, and GTK 3 file bookmarks. Android's StorageVolume/permission semantics and iOS enumeration remain explicit host-integration gaps and throw rather than return misleading zero/empty results. Current-drive, available-space, and filesystem-type queries select the most specific visible mount root containing the current path; Windows space queries accept UNC paths directly and UNC filesystem type is reported as `Network Share`.

The current `res://` and `user://` backends remain directories. Packed/exported resource contents, import remapping, and resource-loader views are not simulated.

## Consequences

- Directory operations compose with existing file and project-path APIs without another package or abstraction layer.
- Blocking enumeration and mutation stay out of real-time hot paths.
- Individual instance calls are thread-safe, but compound caller sequences and external filesystem races remain non-transactional.
- Copy is not atomic and rename does not simulate cross-volume copy/delete.
- Native platform behavior requires native-host tests in addition to compilation.
- Future pack/resource-loader integration can provide another backend without changing current physical-directory semantics.

## Rejected alternatives

- Return numeric errors and retain a last-open error: rejected because standard exceptions already define the Core I/O contract.
- Add a public reference-count protocol: rejected because managed lifetime and deterministic disposal already own this boundary.
- Treat `Directory.Exists`/`File.Exists` as complete validation: rejected because they can hide invalid or inaccessible-path failures.
- Implement recursive public erase: rejected because it is destructive and absent from the audited stable surface.
- Compare canonical path strings for equivalence: rejected because hard links can name the same filesystem object.
- Infer case sensitivity only from the operating system: rejected because policies can vary by filesystem and directory.
- Return empty drive results on mobile: rejected because that would make a missing host/storage backend appear successful.
- Add packed-resource placeholders: rejected because no pack-mount or resource-loader domain exists.

## Verification boundary

The executable harness verifies ordinary and virtual scope behavior, listing state/filtering/concurrency, mutations, nonrecursive deletion, temporary ownership, relative/dangling link safety, Linux hard-link/symlink identity and case metadata, drive/filesystem queries, and disposal on Linux. It does not establish Windows, macOS, Android, or iOS native behavior, exported-pack behavior, crash atomicity, cross-volume rename, or hostile-filesystem confinement.

## Related decisions

- [0001: Typed C# without Variant](0001-typed-csharp-without-variant.md)
- [0003: ElectronObject lifetime](0003-electron-object-lifetime.md)
- [0014: Managed Resource lifetime and realtime allocation](0014-managed-resource-lifetime.md)
- [0019: Typed project settings and directory-backed virtual paths](0019-typed-project-settings.md)
- [0020: Typed file access and transformed-file containers](0020-file-access.md)
- [0021: Cross-platform runtime target matrix](0021-cross-platform-runtime-targets.md)
