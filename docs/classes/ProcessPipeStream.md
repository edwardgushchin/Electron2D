# ProcessPipeStream

Last updated: 2026-10-12

Internal type in [ProcessPipeStream.cs](../../src/Core/IO/ProcessPipeStream.cs); unavailable to consumers.

Adapts actual SDL read/write pipes to non-seekable Stream. Blocking NotReady polls on the caller while releasing the process gate; nonblocking reads return immediately. Readiness is distinct from EOF. CR lookahead uses one-byte pushback without seeking. Writes expose exact consumed-prefix exceptions. Disposal closes its ProcessPipeHandle objects without killing the child.

[Process component](../components/processes.md) records executable lifetime, pipe, GC and failure checks. This is cold non-realtime ownership code; no native allocator or foreign-host acceptance is claimed.
