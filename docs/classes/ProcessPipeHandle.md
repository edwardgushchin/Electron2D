# ProcessPipeHandle

Last updated: 2026-10-12

Internal type in [ProcessPipeHandle.cs](../../src/Core/IO/ProcessPipeHandle.cs); unavailable to consumers.

SafeHandle owning one native SDL IOStream and one OSProcess pipe retention count. Deterministic close and finalization run under the process gate, close the stream once, and allow native child-owner release only after completion and final stream closure. Native stream cleanup clears its process property, preventing double close.

[Process component](../components/processes.md) records executable lifetime, pipe, GC and failure checks. This is cold non-realtime ownership code; no native allocator or foreign-host acceptance is claimed.
