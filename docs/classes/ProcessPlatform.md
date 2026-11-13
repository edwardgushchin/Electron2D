# ProcessPlatform

Last updated: 2026-11-13

Internal native bindings in [ProcessPlatform.cs](../../src/Core/OS/ProcessPlatform.cs). ObserveExit uses waitid with WNOWAIT to inspect an owned POSIX child without reaping it, preserving normal exit codes and signal/core wait status. The existing native process owner then reaps once and retains coherent lifetime state. Pinned Linux/Darwin siginfo header/layout and wait flags select their system-library bindings; EINTR retries. Windows force termination uses exit code zero and never terminates descendants. No public handle or native type escapes.

[Process checks](../components/processes.md) execute Linux x64 status137 versus SIGKILL9, owned pipe/BCL-free POSIX launch and exit, concurrent duplex and independence. Windows x64 CLR scenarios now verify shared capture, full exit bits and actual hidden/new/inherited console identity; Darwin, other Windows architectures/sandboxes and native allocator totals remain unverified.

The [Windows launcher](../../src/Core/OS/ProcessPlatform.Windows.cs) shares CreateProcessW setup between independent launches and capture. It quotes literal argv, uses CREATE_NEW_CONSOLE or CREATE_NO_WINDOW, one merged output pipe, NUL for disabled stderr and a STARTUPINFOEX handle allowlist. SafeHandles retain actual process identity through fast exits and release pipe/thread/process ownership on success or failure. No shell or background exit-code substitution is used.

Windows launch preallocates its SafeProcessHandle before native creation. A finally guard owns both raw handles until adoption completes; failed adoption terminates and waits for the unpublished child, closes the thread and releases either the raw or adopted process handle exactly once. The exposed anonymous-pipe client handle closes explicitly on failure as well as success. STARTUPINFO size follows the actual extended-startup flag. The isolated [adoption failure fixture](../../tests/ProcessAdoption.Tests/README.md) checks this boundary without adding production fault controls.
