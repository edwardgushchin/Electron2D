# Host platform queries

Last updated: 2026-10-27

<a id="adr-0113"></a>
## ADR 0113: Retained host identity and feature queries

Last updated: 2026-10-27

- Status: Accepted
- Scope: OS native memory, platform/CPU/locale/sandbox queries, thread identities/native naming, typed project host tags and renderer feature observations
- Preserves: [0004](product.md#adr-0004), [0019](core-data-io.md#adr-0019), [0021](product.md#adr-0021), [0028](rendering.md#adr-0028), [0095](singleton-services.md#adr-0095)

### Context

Games need to choose assets and worker settings using the actual host and available rendering path. A package being installed does not establish native compression support, and application translation culture is separate from operating-system locale. Existing process/environment, typed project feature and renderer lifecycle services provide those observations without opening another device.

### Decision

OS keeps static public operations on its retained service. GetName reports the platform family. GetProcessorCount uses the native logical-core query, including its unknown-zero result; it is host information rather than a worker recommendation. GetProcessorName caches available CPUID brand data or Linux CPU metadata. GetDistributionName uses the exact Linux os-release NAME field, with platform-family fallback. Linux GetVersion uses the exact VERSION field, falling back to the kernel build description. Optional metadata reads are bounded and cold; immutable results are prepared once. Other detailed platform adapters retain their applicable reference behavior and remain explicit gaps where this implementation has only a managed description or no native model source.

Linux/FreeBSD locale queries read live LANG and strip the encoding suffix, using en only when absent. Other profiles use the native preferred locale API, serialized with environment edits and freed after reading its pointer array. GetLocaleLanguage returns the language component, including three-letter languages and nonstandard system values. Neither query assigns TranslationServer.Culture; ADR0007 retains its managed application-culture startup policy. IsDebugBuild observes the engine assembly's build, independently of debugger attachment. Current Linux sandbox detection checks Flatpak/container markers and the complete three-variable Snap environment; other platform sandbox adapters remain missing instead of guessing false.

OS and ProjectSettings share normalized, case-insensitive host/build/architecture tags under ADR0019. Host tags include process bitness and architecture families, scalar precision, template/build role and current supported thread profile. Custom project tags remain runtime-managed; saved typed overrides follow explicit feature activation. OS.HasFeature also observes live movie output state, the available system-font provider and current renderer tags. Unknown tags return false and null is rejected. Dynamic service observations are available through OS; the typed registry's default feature set remains its documented host/custom set, while explicit caller feature lists retain their own complete selection context.

RenderingServer.HasOSFeature reports the active ordinary native-image upload profile. Each GPU backend captures complete BC family support after creating its already selected device; compatibility has no native compressed-upload family. Queries reuse immutable session data, require no new device and can run across threads. OS returns false for renderer tags without a published service; the renderer's public operation retains its ordinary active-service requirement. Native capability data retires with the session. CPU decompression support and unrelated SDL device-only image roles do not advertise ordinary canvas native upload support.

### Thread identities and names

OS.GetThreadCallerID exposes process-local engine identity, not a native kernel
thread number. Ordinary threads have stable distinct identities; the reserved
main identity is assigned to the actual Engine lifecycle owner before startup
callbacks. Manual and owned-window hosts share this role. Failed or normal
retirement clears it before another lifecycle starts; first-use workers and
rejected competing hosts cannot capture it. Retained native-service owner restrictions
remain in force across lifecycle changes. Identity grants no additional scene,
processing-group or native permission. Thread start/join/priority and the unresolved
thread-safety override remain separate under ADR 0104.

OS.SetThreadName submits the caller's native name through pthread naming on
Linux/Android/Apple and SetThreadDescription on Windows. Typed exceptions replace
native error sentinels. Null/embedded NUL input rejects before submission, native
length limits are preserved without truncation and missing adapters fail explicitly.
See [thread identity and naming](../components/os-thread-identity.md) for ownership
and executable verification boundaries.

### Native memory snapshots

OS.GetMemoryInfo returns an immutable OSMemoryInfo value with signed byte counts
and an independent -1 sentinel for each unknown or unrepresentable result. It
queries the host on each call without initializing SDL, a window or a renderer;
concurrent callers do not share mutable output. Host totals do not represent a
container quota, the managed heap or a reservation.

Linux/Android use sysinfo totalram, freeram and freeswap with mem_unit. This is
the kernel source of the pinned proc memory fields: Available is Free plus free
swap, without adding reclaimable cache or MemAvailable. Windows uses
GetPerformanceInfo: PhysicalTotal, PhysicalAvailable and CommitLimit multiplied
by PageSize. Its Available includes existing commitments, preserving the native
commit-limit contract rather than claiming a remaining allocation guarantee.
Apple uses hw.memsize and host_statistics64 with the pinned used-page formula;
macOS adds vm.swapusage available bytes, while mobile/TV has no swap contribution.
The permanent service retains one Apple host-port send right for process lifetime.

Stack preserves the native implementations: Unix reports the process soft
RLIMIT_STACK (including on worker threads); Windows reports the calling thread's
GetCurrentThreadStackLimits range. Unlimited limits become -1. Known zero budgets
remain zero, a deliberate correction to truthiness-based sentinel assignment.
Native failures preserve other independent fields. Multiplication, addition and
Apple page subtraction reject overflow or inconsistent readings as unknown.
Platforms without an adapter fail explicitly. See [host memory](../components/host-memory.md)
for the public fields, independent native checks and platform qualification limits.

### Verification and remaining gates

The public HostPlatform.Consumer checks host metadata, shared and custom tags, typed override selection/save/load, live locale/environment isolation, sandbox boundaries and prepared queries. Its native modes select an actual compressed or fallback texture, draw and capture pixels on each claimed backend/display pair. Root checks cover metadata parsing and related project/environment/process/font/movie contracts. Strict NativeAOT is a separate executable gate. Foreign detailed version/model/sandbox and simulator/browser feature adapters, native allocations, physical GPU, editor and human acceptance remain distinct from the exercised Linux profile.

### Reference evidence

The pinned [thread identity and main-role implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/os/thread.cpp)
and [POSIX name submission](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/drivers/unix/thread_posix.cpp)
retain logical identities and native naming failures as distinct contracts.

The pinned [OS feature dispatch](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/os/os.cpp#L471-L644), [Linux metadata and sandbox queries](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/platform/linuxbsd/os_linuxbsd.cpp#L211-L344) and [Unix locale query](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/drivers/unix/os_unix.cpp#L1031-L1042) define the applicable behavior. Exact metadata key parsing avoids matching VERSION_ID or PRETTY_NAME when another field was requested. No comparison engine identity enters production source, XML or the public wiki.

The pinned [Unix memory query](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/drivers/unix/os_unix.cpp#L406-L607)
and [Windows memory query](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/platform/windows/os_windows.cpp#L1249-L1302)
define memory sources and platform-specific budget meanings.
