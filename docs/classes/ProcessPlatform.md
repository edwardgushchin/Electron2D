# ProcessPlatform

Last updated: 2026-10-12

Internal native bindings in [ProcessPlatform.cs](../../src/Core/OS/ProcessPlatform.cs). ObserveExit uses waitid with WNOWAIT to inspect an owned POSIX child without reaping it, preserving normal exit codes and signal/core wait status. The existing native process owner then reaps once and retains coherent lifetime state. Pinned Linux/Darwin siginfo header/layout and wait flags select their system-library bindings; EINTR retries. Windows force termination uses exit code zero and never terminates descendants. No public handle or native type escapes.

[Process checks](../components/processes.md) execute Linux x64 status137 versus SIGKILL9, owned pipe/BCL-free POSIX launch and exit, concurrent duplex and independence. Darwin/Windows native host acceptance and native allocator totals remain unverified.
