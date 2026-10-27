# Native host memory

Last updated: 2026-10-27

`OS.GetMemoryInfo()` exposes a fresh immutable [OSMemoryInfo](../classes/OSMemoryInfo.md)
through the retained static OS service. It works before, during and after an Engine
lifecycle and on ordinary workers. It does not open SDL, graphics, audio or a window.
The snapshot has no disposable ownership. Returned bytes describe the host rather
than GC accounting, a container's quota or reserved resources.

| Platform | Physical | Free | Available | Stack |
| --- | --- | --- | --- | --- |
| Linux/Android | sysinfo totalram × mem_unit | freeram × mem_unit | Free + freeswap × mem_unit | process soft RLIMIT_STACK |
| Windows | PhysicalTotal × PageSize | PhysicalAvailable × PageSize | CommitLimit × PageSize, including current commitments | caller's native stack address range |
| macOS | hw.memsize | physical minus native used pages | Free + vm.swapusage available | process soft RLIMIT_STACK |
| iOS/tvOS | hw.memsize | physical minus native used pages | Free, without swap | process soft RLIMIT_STACK |

Apple used pages are active + inactive + speculative + wired + compressor minus
purgeable and external pages, matching the pinned platform implementation. One
host-port send right remains process-owned by the permanent service. Unix Stack
is not the size requested for a managed Thread. Unlimited and unavailable budgets
are -1. Known zero values remain zero. Byte products/sums and inconsistent Apple
page differences cannot wrap into usable budgets; independently successful fields
survive a failed query. Other platforms throw NotSupportedException explicitly.

Linux `Free` intentionally excludes cache/reclaimable memory. These values can
change immediately; even a positive `Available` cannot guarantee allocation.
The Windows commit limit has a different meaning from remaining free commit.

## Verification

The public-only [HostMemory.Consumer](../../tests/HostMemory.Consumer) references
only the runtime, with no native-consumer import. Four first-use workers, including
explicit managed stack sizes, query independent snapshots. Physical total is
compared with `/proc/meminfo` on Linux. The prepared 1,024-query interval checks
zero caller-managed allocations and absence of a renderer. Separate child processes
run with independently configured finite and unlimited stack limits.

`ELECTRON2D_TEST_HOST_MEMORY=1` selects root arithmetic boundaries, lifecycle callbacks and the public
contract; the ordinary aggregate root suite also runs it. Compile `tests/Electron2D.Tests/Fixtures/OSMemory.c` with
`cc -shared -fPIC -Wall -Wextra -Werror` and set `ELECTRON2D_MEMORY_FIXTURE` to its
absolute shared-library path in a separate root-test process to exercise the
native ABI failure paths. Its current-thread seccomp filter denies only sysinfo or
resource-limit queries on disposable worker threads; the real P/Invoke returns
unknown fields while preserving independently successful values. Arithmetic checks
cover known zero, unknown unit size and product/sum overflow. The fixture uses
system C headers independently of the managed structs. Strict trimmed
NativeAOT runs the public contract separately from the root fixture process.
Staged CLR (four managed/configuration files) and AOT (one executable) directories
without engine native assets pass 8 MiB, 16 MiB and unlimited stack profiles.

The exercised platform gate is Linux x64. Windows/Apple/mobile adapters are present
but require their own runtime/ABI qualification; browser has no adapter. Native
heap/system-call costs, foreign hardware and user acceptance are not established
by these managed allocation checks. See [ADR 0113](../decisions/host-platform.md#native-memory-snapshots).
