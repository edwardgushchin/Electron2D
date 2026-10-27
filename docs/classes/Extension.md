# Extension

Last updated: 2026-10-27

Resource identity for one function-loaded module under [ADR 0116](../decisions/extensions.md#adr-0116).
`IsLibraryOpen` and `GetMinimumLibraryInitializationLevel` observe validated entry
state. A new resource is closed. The manager borrows it through initialization and
registration; explicit disposal rejects until unload. A retained unloaded resource
can remain open until its caller disposes it.

The ordinary Resource copy/file rules preserve metadata in a closed wrapper,
without executable callbacks, native state or automatic initialization. Derived
project types must supply exact construction and their own stored-state copy.
See [the module component](../components/extensions.md) for the actual entry,
callback ordering, error/lifetime checks and remaining class/reload work.

`RegisterClass<T>(classID, factory, bind, clear)` registers exact typed class shells
inside initialization. Concrete factories are static method groups; abstract
parents use null. `New<T>(classID)` constructs a caller-owned instance after
parent/child binding. Existing scene/resource/Script/copy factories also admit
registered ancestors. Construction and deterministic disposal require the manager
thread. Module functions become available after the complete constructor.

Replacement retains this resource, captures nondefault typed state and rebinds
surviving shells before restoring values/issuing notification. Removed classes
remain cleared. CLR ABI mismatch fails with retained retry state; no CLR object is
retyped. See [the executable replacement contract](../components/extensions.md#replacement-and-stored-state).
