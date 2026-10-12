# OSProcess

Last updated: 2026-10-12

Internal type in [OSProcess.cs](../../src/Core/OS/OSProcess.cs); unavailable to consumers.

Serializes owned BCL/native child status, force termination, pipe reference counts and completion. Poll observes raw POSIX status without reaping, then reaps through the native owner to preserve its lifetime state; Windows retains its actual native exit value. It caches actual exit status and releases completed process handles only after every native pipe closes. Exit-history metadata remains in OS. Abandon kills/reaps a child only during failed unpublished setup. Gate serializes native calls against release.

[Process component](../components/processes.md) records executable lifetime, pipe, GC and failure checks. This is cold non-realtime ownership code; no native allocator or foreign-host acceptance is claimed.
