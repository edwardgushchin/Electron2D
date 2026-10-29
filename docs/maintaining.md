For [viewport render time](components/viewport-render-time.md), build the matching elapsed-query native bridge and run ELECTRON2D_TEST_VIEWPORT_TIMING=gpu, compatibility and software. Preserve root/SubViewport/RID/native-child pixel evidence, paired publication, CPU process exclusion and single-counted shared child authoring, owner/capture/retirement guards and strict warmed active/idle allocation brackets. Run the native timestamp ABI checker and both existing RenderingDevice timestamp profiles after shared-pool changes; Linux GL/Vulkan execution does not qualify foreign backends or NativeAOT.

# Maintaining the Electron2D contract

For managed memory instrumentation, run the [allocation boundary probe](../tools/physics/allocation-boundary-probe/README.md) with matching CLR/NativeAOT hosts before selecting runtime hooks. Preserve native profiler calibration, exact library identity, object/string/pinned-array routes, raw/aligned size distinction and disabled/uninitialized controls. Helper entries alone do not qualify committed allocation, GC/free lifetime or the composed current/peak monitor. For the CLR lifetime producer, run the [managed live probe](../tools/physics/managed-live-probe/README.md) on workstation and server GC. Preserve generation/relocation/finalization/worker and unseen-peak witnesses, prepared zero-byte reads and disabled/invalid controls. Record MONITOR_GC's concurrent-GC cost and the excluded frozen domain; this diagnostic cannot promote the full monitor. For NativeAOT allocation/retirement work, run the [source-runtime probe](../tools/physics/aot-memory-probe/README.md), preserving pinned-copy patching, actual linker inputs, uninstrumented/concurrent-GC controls, failure/retry, frozen registry events under gc_lock, independent census equality, disjoint/overlapping managed/native witnesses, fast allocation-pointer commits inside the native gate and native wide-size/failure controls. Preserve raw common SOH/UOH reservation commits, caller-owned scopes across GC, finalizer failure retirement, the deliberately late producer rejection and bounded-heap OOM controls. Never hold the gate across GC-capable retries or native event callbacks. Never modify installed SDK archives.

For shared allocator or streaming GZIP bridge changes, retain all twenty-nine ENet/file/GZIP/memory exports and run the assert-enabled Electron2DMemoryCheck allocator oracle, the production native memory oracle, native package tests, `ELECTRON2D_TEST_GZIP_CLEANUP=1|native` and `ELECTRON2D_TEST_HTTP=1`. Preserve strict checksum/end validation, concatenated-member backpressure, partial initialization cleanup, finalization, prepared zero-byte calls and source-built CLR/NativeAOT proof. Run tests/NativeMemory.SharedHost with the source-instrumented runtime and the same native bundle as the standalone probe; require one loaded bundle, the public codec contribution, full native drain and zero prepared read bytes. See [the owned GZIP domain](coverage/performance-producers.md#streaming-gzip-allocation-domain).

For live button Script changes, run `ELECTRON2D_TEST_BUTTON_SCRIPT=cpu|gpu` with owner/separate physics; optional `ELECTRON2D_TEST_BUTTON_SCRIPT_RENDERER=gpu|compatibility` checks actual button pixels. Preserve Script/native/signal order, generation/scene/disposal guards, error continuation, silent assignment, callback replacement, abstract-role CanInstantiate admission and fresh-process typed storage. Keep existing buttons and Control callback regressions. See [the button callback contract](components/scripting.md#live-button-callbacks).

For live Control Script changes, follow [ADR 0091](decisions/scripting.md#live-attachment-and-rebuild-boundaries) and [the Control callback contract](components/scripting.md#live-control-callbacks). Run `ELECTRON2D_TEST_CONTROL_SCRIPT=cpu|gpu` with owner/separate physics; optional `ELECTRON2D_TEST_CONTROL_SCRIPT_RENDERER=gpu|compatibility` exercises real pixels and native text/IME. Preserve signal/Script/native input order, acceptance/removal guards, pointer and accessible drag forwarding, validated queries, copy/fresh-process storage and strict warmed all-thread/owner zero-byte intervals. Keep ordinary Control input/focus/pointer, tooltip and drag regressions; internal deferred capture events must not require public input dispatch state.

For registered extension-class changes, follow [ADR 0116](decisions/extensions.md#adr-0116) and [the module component](components/extensions.md). Keep factory/source/copy/disposal families coherent, run the public reload consumer on CPU/GPU and the CLR source/native-base check, and distinguish single-load events from batch completion. Compilation alone does not qualify state restoration or the automatic notification.

For function-loaded extension lifecycle changes, follow [ADR 0116](decisions/extensions.md#adr-0116) and run the [public consumer](components/extensions.md). Keep typed module initialization/unloading separate from class registration, state rebinding and reload acceptance; storage must never serialize/activate entry callbacks.

Last updated: 2026-10-29

Depth/stencil changes follow [the native state contract](components/depth-stencil-state.md): preserve stored-depth range semantics, independent visible faces, optional feature refusal, ordinary dynamic-reference switching, composed view/pass/MSAA/component variants and the warmed owner-allocation boundary.

This guide describes the implementation and documentation checks used during code changes. It does not define product architecture. [The decision index](decisions/index.md) routes to the accepted ADRs, and the affected class, component, and domain pages describe current behavior. If a rule here conflicts with an accepted ADR, follow the ADR and correct this guide before implementing.

For release preparation and publication, follow [Releasing Electron2D](releasing.md): finish documentation before final qualification, test the actual release packages, retain their source/run/artifact identities and publish those same files. The [development pipeline](development-ci.md) defines fast feedback, full package qualification and exact-artifact promotion; its hosted activation and platform acceptance remain separate. Existing license, package, platform and acceptance checks below still apply.

For project/scene authoring, editor automation, batch execution and capture work, apply the accepted [agent-native contract](decisions/agent-native.md#adr-0090). Verify the supported programmatic path through public runtime capabilities, including error/cleanup cases and machine-readable results. File authoring requires fresh-process load verification; capture requires the actual renderer and its stated backend limits. Keep headless simulation, rendered batch, native/platform verification and human acceptance separate. Record missing prerequisites in coverage and current-state documentation; do not add commands or report end-to-end agent support before they execute.

For the live-session rollout, bind editor and agent mutations to the same operation authority before adding private UI document state. Verify unsaved human/agent edits, revision conflicts, grouped/overlapping undo, external changes, failure recovery and snapshot job identity/staleness. Exercise one real editor/agent connection and process cancellation before claiming co-development; fake adapters remain test fixtures. The existing Batch controller/SDK/formats carry no compatibility obligation: replace/remove affected consumers and docs with the session/job slice instead of retaining a second authoring path. See [delivery stages](decisions/agent-native.md#delivery-and-acceptance).

For the planned project-memory slice, follow [ADR 0090's portable instructions](decisions/agent-native.md#portable-project-instructions-and-engineering-discipline), [commit policy](decisions/agent-native.md#conventional-project-commits) and [journal contract](decisions/agent-native.md#structured-development-journal). Verify template commands and effective instruction loading for each claimed client/version/profile, including scope, imports, overrides and refreshed context. Exercise accepted/rejected commit messages without disturbing unrelated staged content, six-section journal validation, failed/retried publication and fresh-client continuation. Keep deterministic file/Git checks distinct from real executor compliance; a reviewed instruction file does not qualify a provider. These are implementation exit checks, not currently available project-memory operations.

Apply the [bounded documentation/context workflow](decisions/agent-native.md#bounded-context-and-documentation-workflow): route through indexes and relevant sections, keep one canonical contract, update affected links/status with the change and retrieve history on demand. For stage 4, measure bootstrap/read budgets with a large multibyte project and stale paged reads; verify skill loading from actual installed artifacts. Exercise editor/no-UI Git branches, staging, worktrees, remotes and conflicts against disposable real repositories/remotes, including dirty-buffer/shared-ref protection and failed or uncertain push results. Maintain the [provider/client support list](components/project-agent.md#provider-and-client-support) and every root README locale from exact exercised evidence; researched or planned profiles stay labelled accordingly. Full acceptance cases remain in ADR 0090.

For expiry tests using `Environment.TickCount64`, wait for the observed counter
to advance with a bounded deadline. A short `Thread.Sleep` does not establish an
elapsed tick on Windows/.NET 10, whose counter updates every 10–16 ms. Keep the
behavior assertion after the clock precondition; do not relax expiry or
allocation expectations. See the [clock contract](https://learn.microsoft.com/en-us/dotnet/api/system.environment.tickcount64?view=net-10.0).

## Release license audit

The [MIT license](../licence/Electron2D-LICENSE.txt) covers Electron2D-authored code. Keep each vendor's original license text and the [third-party notices](../licence/THIRD_PARTY_NOTICES.md) in `licence/`. The pinned comparison-data [license](../licence/ReferenceData-LICENSE.txt) stays in the source repository but is excluded from game publishes. The separate [CanvasStyleGeometry license](../licence/CanvasStyleGeometry-LICENSE.txt) accompanies the adapted runtime style geometry, popup menu/search algorithms and default theme icons and is included in publishes. Before publishing a Linux application, build a fresh self-contained output for every claimed RID and run `python3 -B tools/licenses/check_publish.py /path/to/publish`. The current check compares 74 ELF files, pinned NuGet/runtime versions and 101 source, license and notice files for `linux-x64`, including the private block-codec, buffer-tool and KTX/Basis source closure. Update the inventory and notices together when that closure changes. The packaged Vkd3d corresponding source and binary receipts live in `licence/`; the checker validates the delivered source archive. Its Linux x64 inventory does not audit other platforms or every final-game distribution obligation.

For a desktop self-contained CharacterMovement publish, run `python3 -B tools/check_native_publish.py RID /path/to/publish` after publishing without platform-selection overrides. The check requires an executable host, the matching .NET runtime pack, and the three SDL packages plus the selected native font dependencies and the corresponding native libraries for that RID. For Linux, it also checks the private ICU library’s ELF architecture, unique SONAME, exact engine C export closure in tools/check_native_publish.py::TEXT_EXPORTS (currently twenty-five) and absence of a global ICU dependency; GNU `nm` and `readelf` are required for this inspection. The Linux WebP check verifies ELF architecture, SONAME, encoder/decoder entry points and a relocatable static codec closure with only libc/libm dependencies. It rejects encoder-only assets before a long runtime qualification. The native audio check also verifies FAudio ELF architecture, SONAME, shared SDL3 dependency and an internal managed binding. The private ENet check also verifies ELF architecture, SONAME, exact twenty-nine exports including file/GZIP codecs and the shared memory ledger, plus zlib/Zstandard/libc/ELF-loader dependencies. Foreign publishes must exclude all Linux private libraries. Native text execution is verified on desktop, Android native apps, Apple simulators and Chromium as recorded in [Platform verification](platform-verification.md); physical Apple devices remain unsigned build-only. A foreign package check alone does not establish text execution. The check does not verify execution on the target OS or replace its license audit.

All native SDL, font, text/audio/ENet assets belong under `runtimes/<RID>/native` in engine build output, NuGet package entries and project-reference publishes. The CharacterMovement audit rejects root copies and inspects the selected runtime directory; the license audit includes nested ELF files. Verify that no foreign RID or architecture is copied, then verify loading with `LD_LIBRARY_PATH` unset and a working directory outside the output. The platform package targets preserve that layout in RID builds and publishes; the resolver also supports older flat layouts. NativeLibraryTests verifies the project-reference directory contract without claiming foreign-platform execution.

## Private native package

Runtime source builds also generate built-in rendering/physics shaders from source.
The pinned host toolchain is prepared once under `tools/shaders/obj/`; generated
resources stay under the runtime intermediate output and are embedded in the DLL.
Run `python3 -B tools/shaders/check_runtime.py` after shader build/delivery changes.
The fresh native-consumer audit prepares and copies these host tools before blocking
native compilers, then verifies the ordinary source build. NuGet game consumers
need neither shader tools nor shader sources. See [the shader build contract](../tools/shaders/README.md#built-in-runtime-shaders).

The managed engine project restores no native packages. Each executable explicitly selects `Electron2D.{Platform}` for its target; all native SDL, engine, font, audio and codec assets must be restricted to that build/publish RID. Follow [native delivery](native-packaging.md) for published versions and the bootstrap feed required by unpublished development versions. The reusable native workflow audits all 18 target source receipts and binary identities before packing; Build and Tests restore the same feed. `tools/native/test_consumer.py FEED RID` verifies fresh source, ProjectReference and NuGet builds, public text/audio/ENet calls and self-contained publishes with native commands blocked and `LD_LIBRARY_PATH` unset. It requires only the selected platform package and the managed engine package. Run it after delivery/dependency changes; it also checks manifest SHA-256, asset paths and absence of managed backend DLLs. Notices come from the matching platform package. Run `tools/native/test_audio_delivery.py` and `tools/native/test_webp_delivery.py` after explicit desktop asset-selection changes; these invoke the actual targets against stale copies, missing files and paths with spaces. Explicit MSBuild source mode supports Linux/macOS; Windows and cross-target native production use their dedicated scripts.

The macOS producer uses that package project with `NativePackagePlatform=MacOS`; run `tools/native/test_package.py` for architecture/identity/source-pin/global-codec rejection and inspect the matching-runner Mach-O receipts. Its private FreeType closure must execute `tools/font-native/test_freetype.py LIBRARY` against the bundled WOFF2 before staging. Earlier producers/package audits passed, but connected consumers failed decoding with the upstream FreeType; the corrected consumers and full suite remain required, not implied by binaries. The OpenSSL source hash, relocatable crypto dependency and ad-hoc signing are part of its binary audit. `TLSSystemTrustTests` verifies OS-anchor acceptance and unknown/missing-certificate rejection without changing system trust stores. The macOS oracle job places its independent Homebrew OpenSSL executable on PATH; it must not replace the private runtime libraries.

## CI matrix

Physics query/CCD compiler-profile changes must retain unchanged optimized Vulkan module hashes and verify native resource/workgroup reflection for each operation or ordered stage. Shape queries compile all four operation constants from one numerical source; verify every operation and mixed batches, preserving iteration limits, result segments and one submission without extra readback. Run `python3 -B tools/shaders/check_runtime.py` for variant invalidation, cleanup and embedding, then the focused GPU pipeline, shape, motion and CCD contracts. Offline DXIL compilation is a compiler check; it cannot replace the complete Windows and Metal execution gates. Optimized portable motion reflection retains eight readonly buffers, one uniform and 64x1x1 workgroups. Initial/continued recovery and translation expose three writable buffers; final output exposes four because it also reads collider centers. The unused center binding is removed by optimization in the first three stages; host buffers and their binding positions remain unchanged. Ordered motion passes must bind exactly three writable buffers before the four-binding final stage; an extra pass binding can reach an uninitialized native descriptor even when pipeline creation succeeds. The portable motion phase definitions are profile-specific; its generation self-check covers definition selection, include invalidation and removed-module cleanup. The manual macOS runtime verification repeats the unchanged complete suite for both ARM64 and x64 on the registered Apple Silicon host, using native source receipts and retaining the original failure status. Its optional allocation_trace mode first calibrates the versioned native profiling ABI on the target RID, then retains general-cast object/stack traces and managed assemblies in a separate artifact. The input defaults to false; a profiled run is diagnostic evidence. See [allocation measurement](components/physics-allocation-measurement.md#general-cast-allocation-diagnostics-on-macos-2026-10-23).

An ARM64 Windows native crash reruns the same complete executable under CDB, without the historical DTLS-only selector. The diagnostic retains symbolic exception context/module identities and an eight-minute child deadline inside the ten-minute CI step. It cannot hide or replace the original failed full-suite status.

The manual Windows GPU diagnostic records timestamps before and after each cold
pipeline creation in its temporary source copy, preserving original/instrumented
device hashes and the unchanged motion/compiler/generator source hashes. It runs
the current production DXIL shaders and the entire untraced executable without
a test selector. Its 65-minute fixture budget fits the ordinary 90-minute target
job budget while leaving time for CDB and artifact upload. Before the full run,
three standalone existing selectors exercise the complete public GPU compound,
public joint and functional motion cohorts. Each selector is process-local and removed in
`finally`; each raw exit/log is retained in an early focused artifact. Any focused
failure remains a failed job. The complete suite runs only after all three focused
outcomes succeed; a failed preflight cannot produce a green complete gate.
These controls consume the same overall 90-minute job budget and do not shorten
physical work. With `continue-on-error`, the API's step conclusion can be success
after a failed native exit. Read the raw exit/log and preserved final job result,
not that intermediate conclusion.

The optional false-default `recovery_trace` instruments only temporary motion
shader/query copies, retaining original/actual hashes. Its private result padding
encodes traversal/role flags, reducer counts/depths and a zero-count host fallback
sentinel, with no additional buffer or readback. Read the packing in the
[motion-query report](components/gpu-resident-motion-queries.md#native-recovery-trace).
The instrumented result is diagnostic evidence; repeat the ordinary native gate
after any correction.

Historical DXBC, optimizer and earlier result-lane source-copy controls are retired; their successful
focused results do not replace the complete production target gate.
For a stalled CDB replay, the debugger interrupts only one exact executable child
after six minutes and records every native thread stack at that breakpoint. The
process is checked again through its opened handle before interruption, and the
original eight-minute debugger budget still bounds cleanup. The replay uses
`CDB -hd` to retain the ordinary process heap: the preceding linear control stack
spent its deadline in `RtlDebugAllocateHeap` and WARP `ReducibilityAnalysis`. The
untraced fixture now runs directly under PowerShell and records the signed exit
code and its full 32-bit hexadecimal representation before preserving failure;
zero exit and an exact case-sensitive completion line are both required.
Portable motion initializes bounds from its first vertex after the existing
empty-geometry rejection, retaining the remaining vertex order. Translation loads
and saves its bracket around every candidate, and final sampling retains accepted
deeper contacts in the existing result prefix. Preserve the depth threshold,
source slot, rejected keys, six device passes and single final readback. Recovery
retains plane/key counts, maximum depth, accepted/rejected pair flags, accumulated
correction and convergence input in the existing prefix across geometry calls.
The first two prefix words are phase-local scratch; final sampling replaces them
with its actual contact. Portable geometry returns both contact snapshots by
value; recovery/final sampling consume both and moving refinement uses the first.
Retain both points and all existing ordering/filtering rules when changing that
boundary. Portable recovery invokes projection from the main stage after scan
returns, reloading its retained count/depth; ordinary Vulkan keeps projection
inside scan. Preserve the projection formula and error status at this boundary.
All four portable stages use offline optimization. Complete target execution must
use the production shader source without diagnostic result-lane writes.

Compressed-audio allocation diagnostics use `ELECTRON2D_TEST_AUDIO_COMPRESSED=cpu`
for the aggregate's managed mixing mode and `1` for complete native output.
Optional calibrated native markers retain all four MP3/Ogg active/idle intervals,
their exact owner-byte deltas and the original warmup/workload/zero threshold.
Prepare buffers and tags, then drain the shared collection/finalization helper
before the unchanged 50 active or 20 idle warm blocks; each measurement retains
64 blocks of 512 frames and the owner-only zero-byte gate.
The macOS allocation workflow saves these standalone traces and the complete
aggregate trace; one successful profile does not attribute another failed one.
Its complete runner saves a one-second native thread sample after five minutes
without output, repeating only while its own child remains silent. Sampling
failure is retained separately and cannot turn a failed or incomplete suite green.
The original 90-minute job budget, zero exit and exact completion line remain.
Joint-policy allocation diagnostics use domain `8UL << 32` around the original
128 edit-active physical frames, with backend, host-GPU and separate-thread roles
in the log. The existing `ELECTRON2D_TEST_PHYSICS_THREAD=1` selector repeats the
complete CPU separate-thread cohort before the full macOS suite, both ordinarily
and with calibrated object markers. Original warmups, counters and zero limits
remain unchanged; a passing isolated cohort does not explain a failed aggregate.
CCD measurement uses domain `11UL << 32`, with timing storage and role/tag setup
before the shared preparation and 64 warm steps. Retain all 128 reset-active
steps, owner/all-thread zero limits and serial, separate-thread and CPU-hosted
GPU roles. Ordinary and calibrated macOS diagnostics run the existing complete
continuous-contract and CPU-hosted GPU CCD selectors before the full suite.
The ordinary diagnostic also retains the complete GPU contact-creation/removal
contracts. Parallel contact endpoints use scalar shader fields with the same
80-byte result and 16-byte logical link rows; verify both creation and removal,
host-CCD scenes, generated resource bindings/offsets and revised Metal execution.
Contact creation/removal and collision bounds come from their existing 32-byte
uniforms. Keep the compiled contact-shader check against `OpArrayLength`: its
Metal translation needs an auxiliary size-table binding absent from these
pipelines. Preserve negative-ID/generation/capacity validation and verify actual
buffer capacities and uniform offsets; a local pass does not close native Metal.
The macOS diagnostic also runs the existing native particle shader cohort before
allocation profiling and preserves its raw log immediately. Retain both source
languages, oversized/zero-capacity queues, UINT_MAX counters, physically headerless
storage and the 32 prepared dispatches. Never replace actual buffer bounds with
authored/header metadata alone. A continued diagnostic step does not erase failure.
The revised Metal adapter retains original array-length queries and supplies real
dispatch byte ranges through its private readonly size table. Run both particle
shader profiles plus the server `gpu`, `subemitter`, `allocations`,
`material-subemission` and `material-allocations` cohorts. Keep the eight-storage/
four-uniform and same-submission rebinding regression, original module bytes,
direct/indirect dispatch and program/variant/binary ownership. Source translation
and local Vulkan success do not close revised Metal execution.
Contact-policy intervals use domain `9UL << 32` for the same 256 warmup/128
edit-active full steps, with scene/server, CPU-hosted GPU-stage and resident roles.
The macOS diagnostics repeat existing CPU/GPU contact-correction selectors both
ordinarily and with physical markers. No collection, workload or zero limit is
changed; retain failed aggregate values until their own object trace is available.
See [CDB command-line options](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/cdb-command-line-options).
The PowerShell self-check rejects missing, ambiguous, foreign and reused process identities.
Compiler exceptions also record the first DWORD and one symbolic type pointer of
the WARP exception object and its throwing stack; architecture-specific argument locations have executable
PowerShell checks. No general heap dump is captured. The current temporary
body-motion control uses explicit overlap, directed reach/eight refinements and a
common impact block, preserving the operation order and shared numerical helpers.
The bounded rolled control still fails the same missing-preheader check. Its
unrolled alternative expands to 713584 bytes/14254 blocks and takes 98.6 s to
compile locally; the linear control compiles in 9.9 s at 202052 bytes/4297 blocks.
Five local numerical/allocation groups pass, but driver and full target gates
remain separate. With the ordinary heap, the linear control exits `0xC0000409`
and repeats the missing-preheader exception; all 174 loops in its input DXIL have
valid preheaders. The public optimizer does not expose that internal pass.
The next control flattens the two piece loops into one: concave/concave candidates
are already excluded, so only one operand can have multiple pieces. Zero counts,
both operand orders and enumeration order remain unchanged, without multiplying
counts. Its offline DXIL is 201736 bytes/4294 blocks and compiles in 13.0 s.
The two diagnostic jobs compare the current DXIL path with explicit FXC/DXBC only
for this motion pipeline. All other compute pipelines keep the production
compiler. The latter control requires advertised DXBC support from both the
device and ShaderCross and records that selection; source receipts identify the
temporary compiler and device edits. Both jobs preserve their original failure
and retain ordinary native ownership. This comparison does not select a new
production compiler policy or package a test driver. The preceding convenience
control reports invalid DXBC after compilation and exits `0xE0434352`; its replay
ends with the same managed failure. Explicit bytecode compilation now records the
compiler error before pipeline creation can replace it, and the existing explicit
pipeline owner frees that bytecode on both success and failure. It identifies FXC error X3531 at the 128-iteration query sweep: the compiler wants to unroll a loop marked with a loop attribute. The current temporary control removes only that sweep prohibition under the portable motion definition; the 128-iteration limit and all eight refinements remain. Source receipts include the shared distance include before/after hashes. LF/CRLF and both compiler paths produce the same SPIR-V; the ordinary Vulkan control is byte-identical to the preceding linear/flat diagnostic. Offline DXIL is 201692 bytes/4294 blocks and compiles in 9.3 s. Target execution remains required.
The fixture stays under tools/ci and changes only the temporary
checkout copy. Source receipts include both motion shader hashes; this diagnostic
rewrite does not change the production shader or establish target acceptance.
The next Windows control uses Microsoft's test-only WARP 1.0.21, whose release
notes describe redesigned control-flow transforms. Its package and architecture
DLL hashes are pinned; NuGet and Microsoft Authenticode signatures must validate.
The temporary source probe also requires the actual loaded module to match the
app-local test DLL. This development/test driver is never added to runtime
projects, native packages or release consumers; its result cannot establish a
correction in the operating system's older WARP driver.
The manual diagnostic selects `win-x64`, `win-x86` or `win-arm64`, using the native
ARM64 runner for the latter and carrying the same validated RID through publish,
driver installation, source receipts and CDB. The default remains x64. Full
Windows RID jobs now install the same audited WARP version only in the published
test runner directory before executing the complete compound and full suites.
The package remains outside runtime/native/public consumer dependencies. Previous
system-driver failures remain recorded; the new software-driver baseline requires
its own complete ordinary CI results and does not establish hardware acceptance.
Windows full RID jobs have a 180-minute job budget; the manual full fixture has
150 minutes within a 180-minute job. The prior 65-minute x64 diagnostic still made
progress at its deadline. These budgets retain every operation and strict
allocation assertion, require the final completion marker and preserve timeouts
as failures.

The manual Linux GPU diagnostic first runs the unchanged public compound fixture
on the registered x64 host. A failed baseline is replayed with candidate-count and
impact-fraction logging in a temporary source copy; the shader manifest must stay
byte-identical. CPU/driver identities, both logs and source hashes are retained.
The trace covers separate and shared swept-pair completion, reads the status at
the mapped CCD offset and captures the shared batch inputs after completion.
Neither a traced replay nor a local pass replaces the failed untraced target gate.
A further control restores the exact original source bytes and repeats the
unchanged fixture with only the installed Lavapipe ICD selected. The original
baseline still determines the diagnostic job's failure status.
The SIMD control always logs the cold native device identity
from a temporary source copy and repeats the complete fixture at vector widths
128 and 256 using the same binary and separate empty shader caches. Each log must
confirm the requested width, zero raw exit and the exact compound completion
marker. These are diagnostic driver settings, documented by
[Mesa](https://docs.mesa3d.org/drivers/llvmpipe.html#environment-variables);
they do not change ordinary RID gates or replace the original workload. Hosted
CPU models can differ between runs, so a successful baseline still executes both
controls. Either an original failure or a failed SIMD control keeps the job red.

The native `pthread_key_create`/`pthread_key_delete` interposer records process,
key, result, live count and caller module/offset; a failed allocation includes a
bounded native stack. A separate owner-only trace file preserves application
stdout/stderr, including inherited child hosts. `python3 -B tools/ci/test_tls_trace.py` verifies real cleanup,
intentional leakage, exhaustion, errno and thread-destructor preservation before
use. A delayed-delete control verifies that immediate cross-thread reuse of a key
slot does not corrupt the counter; native mutation and bookkeeping share one short
gate, released before logging. The diagnostic uses the registry's Ubuntu 26.04
ARM64 host and its runtime prerequisites. The helper is outside all runtime/package projects. Traced execution is
diagnostic evidence; final platform acceptance still requires the ordinary
untraced full suite. Reused native packages must match current source receipts.

Run `39904064029` attributed 1,011 unreleased keys in the main process to the
system `libvulkan_gfxstream.so`; the later `libusb` assertion was the victim of
the exhausted 1,024-key process limit. An isolated Vulkan instance create/destroy
control reproduced one lost key per instance when the installed gfxstream ICD
was also enumerated, without loading SDL or the engine. Lavapipe alone retained
all 1,016 initially free keys over 2,048 successful instance lifetimes locally.
Linux headless CI therefore selects the installed Lavapipe
manifest explicitly through `select_lavapipe.py`, rejecting missing, ambiguous or
foreign manifests. The full runtime/GPU assertions remain unchanged. This is CI
driver selection, not a production workaround for the system gfxstream driver.
The manual Linux driver verification workflow now repeats the complete ARM64
suite without the interposer, recording the selected ICD and selector hash. Its
untraced result is still separate from final CI on the exact release revision.

The x64 macOS Tests job runs the unchanged `osx-x64` target on Apple Silicon through Rosetta for Metal availability. Keep native production and Build on their registered Intel host, verify the test app's x64 Mach-O architecture, and retain the full suite plus trimmed/NativeAOT checks. `tools/ci/test_checks.py` executes the actual Rosetta setup script for already available, installed, failed-install and wrong-architecture cases; these dispatch checks do not establish target execution or physical Intel GPU acceptance.

Fresh-process fixtures restart `Environment.ProcessPath`, adding the test assembly
only when the current executable is the `dotnet` host. Keep child arguments,
environment selection, redirected diagnostics, deadlines and failure assertions.
This preserves a self-contained apphost's architecture under Rosetta instead of
selecting a different SDK host from PATH. The independent decoder oracle and SDK
build checks still invoke `dotnet` for their distinct tool/assembly inputs.
Verify both framework-hosted and apphost launches with a rejecting `dotnet` on
PATH, and retain the complete target suite as the native execution gate.

One CI workflow prepares native packages once and shares them across separate Build and Tests matrices. Both consume `tools/ci/rids.json` through `python3 -B tools/ci/rids.py`; keep it synchronized with the accepted mappings in `Electron2D.csproj`. `tools/ci/test_checks.py` checks matrix/dependency/layout drift, mobile results and strict aggregate status; `tools/native/test_windows.py` covers real PE rejection/import libraries with the pinned PE auditor. Final Build/Tests checks reject failed, skipped, cancelled or missing dependencies. The RID workflow enables trimming/AOT analysis and selects the full self-contained Linux/macOS/Windows headless suite; all seven desktop profiles passed at `975c9693`. Trimmed/NativeAOT desktop contracts exclude Windows x86 AOT; Android emulators, Apple simulators and Chromium execute managed and target-native contracts. All eighteen rows passed in run `37412313488`, with a separate iOS x64 job rerun after a system-boot timeout. Physical Apple profiles build unsigned apps only. Read [exact scope and limits](platform-verification.md#automated-rid-checks); these checks do not establish rendering, production-host lifecycle or hardware acceptance.

Mobile and browser contract apps share `FontTestFixtures`; embed every font resource read by its type initializer, including OTF and TTC fixtures. `tools/ci/test_checks.py` checks the required logical resource inventory in all three consumer projects before the platform jobs run.

Windows RID jobs retain the host MSVC environment for shader tools, then restore `Platform=AnyCPU` for managed SDK inference. Otherwise the host's `Platform=x64` overrides the selected `win-x86` target. The CI regression executes that environment step and checks all three Windows RID platform targets through MSBuild.

Headless Linux test jobs install the Vulkan loader and Mesa Vulkan drivers, then select `offscreen` through both SDL video-driver environment names for the full runtime process. This supplies video-subsystem initialization for real windowless compute without a desktop session; it does not establish rendered-window acceptance. The CI regression executes the actual launch script for both Linux RIDs and verifies that Windows/macOS retain their host selection. An isolated `XDG_CONFIG_HOME/user-dirs.dirs` profile supplies desktop directory roles. Android target checks permit only the pinned private SDL bootstrap reference with compile/runtime/native assets excluded; target bundle checks retain native payload and duplicate-library validation.

Build/Tests job display names use each registry row's unique `label`, such as `Windows (x64)` or `iOS Simulator (ARM64)`, including the called workflow's leaf job. Keep the technical `rid` unchanged for toolchain selection, artifact identity and packaging.

ThemeDB's native type catalog is generated by `python3 -B tools/coverage/theme_types.py` from the exported assembly inventory. `tools/coverage/check.sh` rejects stale output with `--check`; regenerate the catalog with coverage when an exported engine-object type changes. Do not restore assembly-wide `GetExportedTypes` reflection in the runtime.

Canvas mesh graph input changes use the public VisualShader.Consumer `mesh-inputs-cpu`, `mesh-inputs-author` and `mesh-inputs-gpu` modes. Import source graphs with VisualShader.CustomImport so compiled provider factories are present. Retain both mesh custom channels, all eight encodings, absent channels, expressions/global/provider code, shared stage contexts, named varying transport and logical IDs across the 65535/65536 boundary. Particle identity tests must derive their oracle from the actual compute slot and create distinct ages before comparing draw orders; manual emission intake can assign slots in a different order. Linux software native pixels, strict full-trim NativeAOT, prepared active/idle managed allocations and failure-atomic reimport remain distinct checks. Broader native/platform and human gates remain separate.

Canvas point graph changes use VisualShader.Consumer `point-inputs-author`/`point-inputs-gpu` and root `--canvas-point-contract`/`--canvas-point-variants`. Preserve default-one initialization before authored setup, native topology across mesh/instance/particle paths, stage/read-only rejection and explicit device capability policy. See [canvas points](components/canvas-points.md) for source, pixel, module and allocation scopes.

## Public API and coverage

Before implementing a public/protected API change, read ADR 0004 and the owning decision, then check the entire connected family against the product rules: declaring/base types, parameter and result types, factories, accessors and siblings. Resolve conflicting decisions before coding; updating a local ADR or a coverage mapping alone does not authorize a divergence. Specific name-mapping lists do not exempt later types from the general rule.

The coverage renderer validates exported type names before pairing declarations. Redundant dimensional markers fail under ADR 0004, including previously unseen parameter/result types, generics and names such as `Texture2DArray`. Its three explicit exceptions are `Curve2D` and the two blend-space parameter-domain types. Change that exception registry only after an accepted architectural decision and update the ADR and regression check together. `test_render.py` exercises accepted and rejected names and preserves the `CanvasItem.GetWorld()` member contract. The Linux x64 CI Build job runs `tools/coverage/check.sh` against the current compiled surface and rejects stale snapshots as well as naming violations. This check establishes the enforced naming rule; semantic and lifecycle obligations still require the owning ADR audit.

Public enum identity follows [ADR 0051](decisions/enum-identities.md#adr-0051): one semantic value contract normally has one type, shared across owners when meanings and valid values agree. Alignment enums are the explicit exception: preserve their reference declaring owners, including separate nested container AlignmentMode types and namespace-level horizontal/vertical/inline types. The coverage validator checks these owners before alias pairing. Review new enum declarations against existing public types; matching numeric values with different semantics remain distinct. Update all consumers, XML, class pages and coverage when a type moves.

Process-wide service operations use static public access under [ADR 0095](decisions/singleton-services.md#adr-0095), backed by retained internal objects. Preserve lifetime, identity, property descriptors and native ownership; do not expose public `Instance` accessors.

Function, method and property names follow [ADR 0045](decisions/product.md#adr-0045): every acronym stays fully uppercase, including `PNG`, `JPG`, `GL`, `HLSL`, `GLSL`, `GPU`, `API`, `ID`, `UTF8`, and `FPS` (for example, `GetFPS` and `MaxFPS`). Ordinary words retain PascalCase. Existing mixed-case acronym spellings require migration with their callers, XML, class pages and coverage; this rule does not claim that all existing declarations have already been renamed.

Electron2D follows the current official Godot API for implemented 2D concepts where that contract fits accepted Electron2D decisions. Its public and protected API stays inside that semantic capability boundary under [ADR 0004](decisions/product.md#adr-0004). Compare types and inheritance, overloads, properties, events, notifications, constants and enum values, defaults, callbacks, units, coordinate spaces, state changes, lifecycle timing, ordering, errors, and threading. A matching name or compiling signature does not prove behavioral compatibility. Typed C# mappings preserve relevant behavior unless an accepted decision requires an adaptation. Backend convenience, implementation effort, and absence of a current caller alone do not justify narrowing the contract. An Electron2D-only declaration must identify the existing reference concept or library-host lifecycle it projects; otherwise keep it internal or in the executable consumer.

[The coverage index](coverage/index.md) owns the pinned upstream reference, full bidirectional declaration accounting, state vocabulary, blocked-prerequisite rules, completeness checks, regeneration commands, and implementation roadmap. Update affected rows in the same change as public API or behavior. Keep `inventory.md` as the map of implemented Electron2D types, not a second compatibility register. Treat unknown, unreviewed, missing, duplicate, stale, and unjustified extra rows as audit failures. Distinguish complete accounting from semantic compatibility.

`examples/` contains user-facing code for learning and building games. Example code and project files use only public Electron2D API, including bootstrap; implementation backends and their packages stay in the runtime project. Place API probes, injected events, failure fixtures, and conformance assertions in `tests/` or development tools, following [ADR 0027](decisions/product.md#adr-0027). Preserve vendored upstream formatting when updating its source; exclude all of `src/Vendor` from `dotnet format` while verifying Electron2D-owned code.

## Reference API correspondence

Deprecated reference API is excluded by [ADR 0004](decisions/product.md#adr-0004). Inspect the declaration and owning class for the pinned official `deprecated` attribute before inventorying implementation obligations, including an empty deprecation reason. A deprecated class excludes its own declarations; modern declaring base types are still audited independently. Use the applicable modern replacement, or omit the obsolete API when none exists. Do not preserve deprecated aliases, `[Obsolete]` public mirrors, constant-return members or no-op setters solely for compatibility. Existing deprecated surface must be removed with affected consumers, storage/schema, XML and coverage in its owning slice. This is a product exclusion, not a missing implementation or a blocked dependency.

The accepted target preserves the complete applicable Godot API and behavior under all existing decisions, including the typed C#, strictly 2D and naming adaptations. [ADR 0008](decisions/scene.md#adr-0008) maps Godot `Node` to `Node` and `Node2D` to `Entity`, with the same applicable API. Keep `CanvasItem` and the separate spatial/UI branches. Audit each member at its proper declaring layer and propagate the renamed types through every API position. Any further divergence needs an accepted decision; missing implementation remains a gap.

The physics census derives deprecation checks from the pinned metadata across its
complete families, inherited bases and selected consumers. `tools/coverage/test_render.py`
requires `Excluded`, no mapped public mirror and the exact pinned `deprecated` message
for every such declaration; an unmapped retired constant is not implementation work.
Current replacements retain their own implementation and verification requirements.

## Complete implementation slice

When asked to implement a type or concept, deliver a production-ready vertical slice rather than a compiling skeleton:

1. Read the current owning ADR, existing class/component/domain documents, complete upstream class reference and inheritance chain, and relevant coverage rows. Check declaration and owner deprecation before selecting the applicable API. Inventory properties, overloads, events, notifications, callbacks, defaults, state transitions, ordering, ownership, errors, and threading before coding.
2. Classify each item as implemented now, adapted to typed C#, blocked by an exact missing domain/backend/integration or product decision, or permanently excluded by a named accepted decision. For a blocked item, name the trigger and whether it enters the prerequisite's first slice or a separately approved capability. Do not add empty methods, inert state, or false compatibility claims.
3. Implement all behavior in scope, including disposal, callback failure, re-entrancy, thread affinity, and edge cases. Audit foreseeable 2D sibling types under [ADR 0035](decisions/core-math.md#adr-0035); do not add speculative families or empty aliases. Before first public release, correct proven mistakes instead of retaining them solely for compatibility, with boundary tests and updated documentation; avoid unrelated source changes.
4. Check positive, negative, boundary, and failure paths, including exceptions from user callbacks where those callbacks can affect engine state. Compare the compiled public surface and executable semantics with the inventory in both directions, including overloads, defaults, event signatures, constants, and enum values.
   For changed engine-owned hot paths, also check repeated active and idle work after explicit warmup against the zero-allocation rule in [ADR 0014](decisions/resources.md#adr-0014). Record the operation, capacity, backend, threads, and measured managed/native boundary; report unmeasured native or external allocations as a verification limit.
5. Update source XML and all affected living documents. Fix known P0/P1/P2 defects in scope and run relevant formatting, build, executable, coverage, and documentation checks before the task's atomic commit. Report implemented behavior, adaptations, blocked work, permanent exclusions, checks, and verification limits separately.

Focused viewport text replacement uses `ELECTRON2D_TEST_VIEWPORT_TEXT=managed`, `gpu` or `compatibility` with the Release runner. Preserve complete replacement versus native insertion, synchronous TextSet without replacement TextChanged, queued user changes, history/length/failure boundaries, embedded/native/direct-SubViewport focus and real root/child output. Rerun LineEdit/TextEdit/CodeEdit and native text delivery when changing their shared setters; TextEditTests includes fresh-process public replacement after scene restoration. Editing/history is cold allocation-capable work. These checks do not establish physical IME, foreign platforms or editor acceptance.

## Living documentation

Every production domain, component, and class, struct, interface, enum, or delegate has a current document in `domains/`, `components/`, or `classes/`. Test-only helpers and generated files need no separate class page. Keep `inventory.md` links synchronized when production types are added, moved, renamed, or removed. Update each affected page's `Last updated` date when its semantic content changes. Never describe proposed or absent behavior as implemented.

Class pages are complete references for Electron2D's actual typed C# API. Use this order, omitting only inapplicable sections:

1. Type name, source/declaration metadata, linked `Inherits` and `Inherited By`, purpose, and detailed `Description` of ownership, lifecycle, ordering, units or coordinates, threading, and caveats.
2. `Tutorials` or `Examples`, with a minimal idiomatic C# example for every nontrivial type; compile it when practical and label any partial snippet or required context.
3. Summary tables for every applicable API category: constructors, properties, methods and overloads, events, enum types and values, constants, operators, indexers, delegates, and protected extension points. Show complete C# signatures, modifiers, types, and defaults.
4. Per-member descriptions with stable anchors under `Property Descriptions`, `Method Descriptions`, `Event Descriptions`, `Enumeration Descriptions`, `Constant Descriptions`, and equivalent applicable sections. Explain parameters, returns, units, ordering, side effects, ownership, exceptions, threading, lifecycle timing, caveats, and related members as applicable.
5. Lifecycle and state transitions, invariants and errors, dependencies and interactions, verified tests, limitations, and relevant decisions. Include inherited behavior that materially affects use. Put excluded or dependency-blocked upstream counterparts in coverage or limitation notes rather than implemented API tables.

Component pages state scope, owned types, runtime flow, dependencies, invariants, current implementation, exclusions, and verification. Domain pages state responsibility, component inventory, public surface, dependency direction, shared invariants, limitations, and relevant ADRs. Keep links navigable. Update affected documents in the same change as the code; revise an owning ADR and decision routing only if the architecture changes.

## Source XML documentation

Document every public production declaration and protected extension point, including namespaces where supported, generic parameters, enum values, delegates, constructors, fields, constants, events, operators, and indexers. Adapt the upstream semantic contract to current Electron2D behavior across the inheritance chain. Do not claim unsupported editor, renderer, input, physics, serialization, networking, or other behavior.

Use `<summary>` for a concise contract; `<param>` and `<typeparam>` for every parameter; `<returns>` or `<value>` for results; `<exception>` for intentionally exposed exceptions; `<remarks>` for lifecycle, ordering, threading, adaptations, and limits; and checked `<see cref="..."/>` and `<paramref name="..."/>` references. XML, Markdown, tests, implementation, and coverage must agree. Compile generated XML with missing or malformed documentation warnings treated as errors. Keep the external reference name out of production code, comments, XML, and all `README.md` files as required by the root instructions.

Wiki XML lookup resolves method/type generic parameter positions and open constructed parameter types from the compiled snapshot before rendering. `python3 -B tools/wiki/test_generate.py` covers generic method and typed-key constructor identities; generation/check must preserve their actual XML summaries and member details. Generated output remains outside engine commits.

## Native ENet source and interoperability checks

Before committing an ENet integration change, run `python3 -B tools/enet-native/check_sources.py` to verify immutable ENet/FastLZ vendor hashes and license copies. Build `Electron2DENetOracle` with the same CMake project, then point `ELECTRON2D_ENET_ORACLE` at its absolute path when running `ELECTRON2D_TEST_ENET=1` or the full executable runner. The oracle links the stock Unix socket backend and is excluded from production native payloads. No oracle process establishes foreign-platform, routed-performance or user acceptance.

Release executable checks set `TieredCompilation=false` in the test project so warmed allocation intervals use a stable optimized JIT profile. Production hosts retain their own runtime configuration. This does not change allocation budgets, operation counts, warm-up/capacity requirements or native/foreign acceptance boundaries. Test runs with tiered compilation enabled are a separate runtime profile and must be labelled explicitly.

The full desktop test executable prepares CoreCLR's `System.Threading.LowLevelLock`
static constructor at process startup. A calibrated macOS trace attributes one
64-byte `System.Func` allocation to that constructor on a ThreadPool worker during
a later GPU-cast interval. This moves known one-time test-host initialization
before warmed measurements; counters, warmup frames and zero limits remain
unchanged. The internal type lookup fails explicitly if the tested runtime changes.
This preparation is confined to the test executable and does not attribute other
historical allocation deltas. See [the physical evidence](components/physics-allocation-measurement.md#runtime-lock-initialization-2026-10-23).

For strict allocation brackets on runtimes affected by [dotnet/runtime#134724](https://github.com/dotnet/runtime/issues/134724), complete any outstanding background collection with a blocking collection after fixture preparation and before warmup. The upstream [accounting fix](https://github.com/dotnet/runtime/pull/134855) concerns unused allocation-context bytes being counted as allocations. Collection-start counters alone do not detect a background collection already in flight. Preserve the measured operations and the zero-byte assertion; do not subtract a guessed allowance or retry until a sample passes. This setup belongs only in tests, not the runtime frame loop.

When explicitly forcing collection during allocation-test setup, drain finalizers
after the last collection before beginning the warm or measured interval. A preceding
drain cannot account for objects queued by a later collection. Ownership, registered consumer, physics-filter, report-only replay, CPU pool and TextLine
helpers share AllocationWarmupCheck, whose controlled second finalizer
pass detects premature return without changing allocation gates. This follows
[GC.WaitForPendingFinalizers](https://learn.microsoft.com/en-us/dotnet/api/system.gc.waitforpendingfinalizers?view=net-10.0).
Production hosts retain their ordinary GC policy under ADR 0014.

Cold synchronous registry-lock probes use bounded Task.Wait rather than Task.WaitAsync.
Keep the timeout assertion and worker result/identity check. Task.WaitAsync schedules
a BCL timer even when its awaited worker soon completes; later timer-worker startup
can overlap another fixture's strict all-thread measurement. The paired construction
and deadline controls are recorded in [allocation measurement](components/physics-allocation-measurement.md#bounded-registry-probe-waits-2026-10-22).
This is fixture isolation, not an allowance for bytes in a measured interval.

Fresh terrain-scene children retain their 30-second execution deadline and five-second kill cleanup bound. Preserve child stdout/stderr and PID/elapsed/exit diagnostics on rejected exits, missing success markers and timeouts. Archive loading, instantiation, attachment and query/re-paint progress must remain distinguishable. Keep parent backend selection separate from the child's default CPU world, and preserve failed aggregate receipts when isolated controls pass; see [the current investigation](components/physics-allocation-measurement.md#terrain-child-investigation-2026-10-22).

For fixed locale-table or selector changes, run `python3 -B tools/localization/generate.py PINNED_LOCALES_H --check` and `python3 -B tools/localization/check.py PINNED_LOCALES_H --runner RELEASE_TEST_DLL`. The oracle additionally requires the matching pinned `translation_server.cpp` alongside the table; both input hashes are enforced. It compiles the original locale constructor over a bounded standard-library shim and evaluates plural expressions directly in C++, independently of generated C# selectors. Include source data/algorithm notices under the existing ReferenceData license. Native language-menu tests exercise GPU and compatibility, while foreign globalization and general catalog-import gates remain separate.

For canvas MSAA changes, run the focused Release runner with `ELECTRON2D_TEST_MSAA=managed`, `gpu` and `compatibility`, plus `ELECTRON2D_TEST_MSAA_COMPOSITION=1` and `ELECTRON2D_TEST_MSAA_LIGHTING=1`. Check actual edge coverage at all supported counts, resolved screen/group/clip/light consumers, independent targets, Never-clear history, source republishing/storage, capability/lifetime failures and 64 active/64 idle dimension-stable zero-managed-byte intervals. This establishes the exercised Linux profile, not foreign GPU support or native allocator totals.

For BC/DDS source dependency changes, run `python3 -B tools/image-bcn/check_sources.py`; a repeat import from both exact pinned checkouts with `tools/image-bcn/update_sources.py --check` must preserve all delivered bytes. Namespace/visibility/target compilation adaptations do not authorize codec algorithm changes.

### EXR source and consumer checks

For EXR source changes, run `python3 -B tools/coverage/check_exr_sources.py` to verify the private source closure, original/adapted fingerprints and MIT license copy. The manifest pins the provider and the allocation correction submitted upstream. Run the executable suite with `ELECTRON2D_TEST_IMAGE_EXR=managed`, then `gpu`, `compatibility` and `compatibility:hdr` for native texture/capture and actual precision-rejection cleanup. The managed suite restores an HDR texture archive in a fresh process.

Build `tests/Electron2D.Tests/Fixtures/ImageEXROracle.cpp` with the host OpenEXR development library. Its `generate` command writes 192 precision/channel/storage/compression vectors; its `decode` command writes canonical expected data for each vector. Pass that directory to the executable suite's `--image-exr-oracle` command. To verify saving independently, emit the twelve saver role vectors with `ELECTRON2D_EXR_ORACLE_DIR`, decode them through the native oracle and compare the canonical payload after its 16-byte header with each `.expected` file. These probes remain test consumers and do not enter runtime or native packages. `tools/native/test_consumer.py` exercises public EXR HDR round trips in fresh source/NuGet consumers with native compilers blocked; the self-contained Linux notice audit expects 92 engine notice files.

For ProgressBar changes, run the Release executable with `ELECTRON2D_TEST_PROGRESS_BAR=managed`, `gpu` and `compatibility`. Check fractional style pixel rules, percentage/domain numeral policy, shared Range, pause/visibility/edited-subtree transitions, exact fresh scene factories, every native fill-mask pixel and 64 prepared active frames. These are local control/authoring/rendering gates; custom Theme archive, native semantics and foreign platform acceptance remain their own obligations.

For project Theme archive/startup changes, run `ELECTRON2D_TEST_PROJECT_THEME=managed`, `gpu` and `compatibility`. Check six categories/defaults/nulls, group and variation order, alias graph ownership, fresh-process scene loading, startup priority, custom font restoration, preparation failures and actual global-style rendering. These gates do not establish foreign execution or nonunit built-in icon scaling.

CanvasModulate focused checks use `ELECTRON2D_TEST_CANVAS_MODULATE=managed`, `gpu` or `compatibility` with the Release test runner. The managed profile also launches `--canvas-modulate-archive` in a fresh process. Optional `ELECTRON2D_CAPTURE_DIR` retains native pixel images. GPU lighting assertions do not imply compatibility lighting support.

Foldable focused validation uses `ELECTRON2D_TEST_FOLDABLE=managed`, `gpu` or `compatibility` with the Release runner; the managed profile launches `--foldable-archive` in a fresh process. `ELECTRON2D_CAPTURE_DIR` retains native evidence. Include TabContainer validation when changing shared BarPosition.

For ordinary texture placeholders, run `ELECTRON2D_TEST_TEXTURE_PLACEHOLDER=managed`, `gpu` and `compatibility` with the Release test runner. The managed profile includes fresh-process resource/scene archives. Native profiles require real diagnostic pixels, zero-size redraw, borrowed RID lifetime and 64 prepared size-update/render frames. Set `ELECTRON2D_CAPTURE_DIR` to retain rendered evidence. Automatic unknown-texture recovery and foreign execution are separate gates.

For typed missing-texture recovery, run ELECTRON2D_TEST_MISSING_TEXTURE=managed, gpu and compatibility. The managed profile creates and restores a custom texture in fresh processes, checks missing known-image payloads, external revival, identity/metadata/caches and legacy versions. Run the existing missing-resource and resource-archive profiles as adjacent regressions. Native profiles exercise missing and restored textures with real pixels and 64 prepared active frames per state. Inspect descendant process driver maps as well as the parent when enforcing a software-only GPU profile.

ZIP focused verification uses `ELECTRON2D_TEST_ZIP=managed`, `gpu` or `compatibility` with the Release runner. Managed tests exercise standard .NET interoperability and sparse ZIP64 offsets on Linux/macOS; native profiles round-trip a bundled scene/resource archive into actual texture pixels.

For video changes, run ELECTRON2D_TEST_VIDEO=managed and the gpu/compatibility profiles in the executable test project. Set ELECTRON2D_VIDEO_OUTPUT to retain rendered/PCM evidence. tools/ogv-native/test_video.py LIBRARY OUTPUT compares 27 independent profiles and multiple tracks; Electron2DVideoAllocationCheck accepts those .ogv paths and measures prepared replay. Preserve encoder regressions, exact nine-export payload audits and current source/package checks when changing this shared closure.

For imported image-array changes, run `ELECTRON2D_TEST_COMPRESSED_ARRAY=managed dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll` after building with the matching five-export KTX bridge. The default suite also runs these contracts, including fresh-process archive restoration after the original asset is removed. Run the `native` profile with GPU and Compatibility separately; it verifies actual layer/mip texel fetches, full native readback, HDR, reload lifetime and the explicit shader capability gate. Native bridge checks include independent legacy/KTX2 mip oracles, CRC-repaired corrupt payloads, unaligned input and mixed cold initialization. Use a complete matching seven-library cohort for a full suite or package consumer; published development bundles can be older than source ABI requirements.

For block-decoder changes, build the matching five-export KTX asset and run `ELECTRON2D_TEST_IMAGE_BLOCKS=managed` plus `gpu` and `compatibility` profiles. The four floating-point Compatibility roles each have an explicit `compatibility:FORMAT` rejection profile on the exercised driver. Native `Electron2DBlockCheck` uses the shared/static/sanitizer corpus through `tools/ktx-native/test_blocks.py`. The [block component](components/image-block-decoding.md) records precision, source ownership, budgets and the independently generated corpus.

The BaseButton routed-input allocation comparison completes a blocking collection and drains finalizers before warmup, keeping its exact equality against independently copied positional events. Shader/material native mutation checks invalidate the custom texture after cold scalar-type validation, so their callback is exercised during actual submission rather than only during assignment. Neither setup changes production policy or allocation budgets.

Global shader parameter changes use ELECTRON2D_TEST_SHADER_GLOBALS=managed, gpu, hlsl, gpu-hdr-msaa and hlsl-hdr-msaa. Preserve the base/override distinction, bool metadata in all three uniform roles, copied column-major matrix inputs, physical row/column layouts, all 25 executable value/image roles, owner/session/image lifetimes and 64 active/64 idle zero-owner-managed-byte frames. Importer check.py reproduces all four versioned global-stage fixtures. Software Vulkan pixel evidence does not establish hardware performance or native allocations.

The linked-scene profiles use ELECTRON2D_TEST_LINKED_SCENES=managed/gpu/compatibility. Keep fresh-process external/relative/bundled archives, local-resource fallback identity and cycles, inherited subscription omission, topology rollback, legacy scene layouts and versioned metadata queries in the connected check set. Native evidence must name the actual renderer and audit software-driver selection, including child processes.

Mesh index updates use `ELECTRON2D_TEST_MESH_INDICES=1` with the Release runner for 16/32-bit boundary/transaction/storage and fresh-process checks. `ELECTRON2D_TEST_MESH_INDICES_NATIVE=gpu` or `compatibility` exercises public owned-RID updates and layout queries, retained/MultiMesh pixels and 64 active/64 idle complete owner frames. The managed index microfixture reserves a 1 MiB no-GC interval after warmup and checks both owner and process-wide counters; native full-frame intervals use ordinary GC. Preserve strict zero-allocation update intervals, independent LOD buffers, typed per-buffer offsets and rollback after a later invalid record. These profiles do not certify native allocations, hardware performance or foreign platforms.

For agent path diagnostics, run ELECTRON2D_TEST_NAVIGATION_DEBUG=1 with the Release test runner, then ELECTRON2D_TEST_NAVIGATION_DEBUG_NATIVE=gpu and compatibility in separate processes. The managed profile launches fresh scene/project-settings children and checks all four scene navigation teardown families. Native profiles require actual path/default/custom/visibility/result/canvas pixels, no navigation progression and 64 active/64 idle complete owner frames with zero managed bytes; use DOTNET_TieredCompilation=0 for the measured intervals. ELECTRON2D_CAPTURE_DIR optionally retains PNG evidence outside those intervals. Exercise software drivers explicitly on hosts whose hardware gate remains open. These checks do not establish global map diagnostics, native allocator totals, foreign execution or owner acceptance.

For packed mesh import/export, run ELECTRON2D_TEST_MESH_PACKED=1 and the separate ELECTRON2D_TEST_MESH_PACKED_NATIVE profiles gpu, hlsl and compatibility with the Release runner. Managed coverage includes exact raw channel/shape/LOD/skin records, index-width boundaries, independent target masks, copy/invalid-input contracts and a fresh material-free JSON child. Native profiles must verify public batch/append/export, explicit shape count, atomic failure, borrowed/off-owner/custom callback guards, actual shader/channel/material/LOD/skin pixels and palette-aware bounds, then 64 active/64 idle prepared owner frames at zero managed bytes. Use DOTNET_TieredCompilation=0 for measured intervals; ELECTRON2D_CAPTURE_DIR saves diagnostic PNGs outside them. Recheck adjacent mesh/frame/compression/morph/LOD/index/custom and skeletal profiles because the packed producer shares their storage. Source archives, foreign/hardware execution and native allocations are distinct evidence; prepared native callbacks may record again during ordinary startup resizing.

Manual mesh visibility uses `ELECTRON2D_TEST_MESH_BOUNDS=1` and native `ELECTRON2D_TEST_MESH_BOUNDS_NATIVE=gpu` / `hlsl` / `compatibility`. Preserve the geometric/visibility distinction, packed bounds, zero reset, stored copy semantics, palette/morph precedence and both hardware/expanded culling. Native proofs include actual pixels and 64 active/64 idle ordinary-GC owner frames, not source compilation alone.

For procedural mesh surfaces run `ELECTRON2D_TEST_PROCEDURAL_MESH=managed`, then native `gpu`, `hlsl`, `gpu-modern`, `hlsl-modern`, `gpu-homogeneous`, `hlsl-homogeneous`, `gpu-lit`, `gpu-snap`, `gpu-hdr`, `gpu-msaa` and explicit `compatibility` rejection. Validate all 32 dumped composed variants independently with spirv-val. Preserve actual generated/reference pixels, homogeneous W, non-position live attributes, independent strip instances, authored visibility, counts/legacy/fresh archives, callbacks and 64 active/64 idle complete owner frames. Rerun shared mesh/importer/morph/packed/LOD/frame/compression/indices/builders and composition/authored-vertex regressions. Software Vulkan is distinct from hardware/foreign/native-allocation acceptance.

For grouped skeletal physics changes, run the public consumer with `--group-bones` and the root executable with `ELECTRON2D_TEST_GROUP_BONES=cpu|gpu|registered`, including `-threaded` and `-pool-owner` profiles. Verify binding loss/repair against actual modes and queries, shared cold setup/subscriber failure, propagated notifications, selected publication retry and attachment retirement, then 128 complete moving frames with zero owner/all-thread managed allocation. `--native-group-bones` uses `ELECTRON2D_GROUP_BONES_RENDERER=gpu|compatibility` to verify actual body-following pixels through an ordinary Engine window. Physics selection is independent of rendering; NativeAOT and software Vulkan functional evidence do not establish hardware throughput or native allocator totals.

### WebRTC source and transport checks

Run `python3 -B tools/native/test_package.py` to verify the immutable libdatachannel source manifest and required private exports. Build `BuildElectron2DNativeAssets` with restore, `RuntimeIdentifier=linux-x64` and `Electron2DBuildNativeFromSource=true` to produce native assets and source receipts; the WebRTC bridge exports only its 23 engine functions. Run `ELECTRON2D_TEST_WEBRTC=1` with the Release test runner for native/extension/multiplayer/scene contracts. The `browser` profile prints a loopback signaling URL for independent-browser wire verification. Retain native logs as evidence because the browser may observe closure after the host exits. Rebuild/check package receipts after any producer input changes. Routed STUN/TURN, foreign execution and native allocation remain separate verification gates.

Native popup checks use `ELECTRON2D_TEST_NATIVE_POPUPS=gpu|compatibility` separately under SDL Wayland and X11. Preserve actual native parenting/output, parent retirement and callback failures, exclusive/origin/mouse/touch routing, global-position rejection, alpha presentation margins, native menu consumers and fresh-process public scene execution. Transparent GPU popup requests must remain explicit failures until the SDL GPU presentation prerequisite executes. Native event construction and cold authoring are outside prepared render allocation measurements.

## Shaped-buffer interface checks

Use `ELECTRON2D_TEST_TEXT_SERVER=1` for ownership, copied sources/features, metadata, actual native shaping, alias/reflow, owner/reentry/error and prepared scalar-query checks. `ELECTRON2D_TEST_TEXT_SERVER_RENDER=gpu` or `compatibility` exercises real native owned/borrowed/paragraph glyph output, baseline/outline/raster/clipping/both axes and prepared active/idle frames. Repeat native profiles on each claimed display protocol. Font/TextLine/TextParagraph consumers share this kernel and require related regressions when it changes. These checks do not establish physical input, native allocation totals, foreign-platform or owner acceptance.

For standalone Unicode changes, run TextServerUnicodeTests and the four software TextServerUnicodeRenderingTests profiles, then the affected font/text/editor consumers. Reproduce the native whole-string oracle with assertions enabled after building the text-native CMake target: compile tools/coverage/text_unicode_oracle.cpp with TextUnicode.cpp.o and every ICUCommon object from that build, using the vendored common include directory and the same U_STATIC_IMPLEMENTATION/U_HAVE_LIB_SUFFIX/U_LIB_SUFFIX_C_NAME=_electron2d definitions; pass src/Vendor/ICU/data/TextBreak78.dat to the executable. It checks all valid scalars, mixed normalization strings and locale casing. A filtered data package without ulayout.icu must fail its missing-layout mode. Run python3 -B tools/update-icu-text.py --check and tools/native/test_package.py; a data change additionally requires byte-identical archive reproduction. Updating the C ABI requires rebuilding matching native assets; historical package/platform results cannot establish new exports.

Layered device framebuffer changes use `ELECTRON2D_TEST_DEVICE_MULTIVIEW=1` and `canvas` on software Wayland/X11. Preserve direct/strided-indirect per-view vertex and fragment indices, copied attachment descriptions, isolated depth, input and MSAA resolve layers, empty clears, untouched extra layers and borrowed lifetimes. The readonly graphics image path additionally requires `ELECTRON2D_TEST_DEVICE_GRAPHICS=1` with changed GPU source pixels and logical Image bindings. Run the shared device passes, sparse, MSAA, depth, push, specialization, source, transfer, texture, compute and retained canvas profiles. The source profile requires a complete built shader importer selected by `ELECTRON2D_SHADER_IMPORT_TOOL`. Eight-bit texture filtering checks allow source-format quantization. Prepared device and canvas intervals require zero owner-thread managed bytes; software execution does not establish hardware, foreign-platform or native allocator acceptance.

Independent device sampled-resource changes require `ELECTRON2D_TEST_DEVICE_SAMPLERS=1`, plus `GLSL` and `HLSL` canvas values under software Wayland and X11. Build the full ShaderImport tool and select it with `ELECTRON2D_SHADER_IMPORT_TOOL` for the explicit authoring/capacity/depth checks. Validate exported modules from `ELECTRON2D_DEVICE_SAMPLER_OUTPUT` with spirv-val. Warm every changing set and direct/indirect route before zero-owner-managed-byte intervals. Recheck existing device and sampler-array managed/native/operations/vertex/modern/capacity profiles because they share opaque-resource lowering. Public enum vocabulary and unchanged declaration counts must not be reported as newly completed API rows.

Formatted texel-buffer checks use `ELECTRON2D_TEST_TEXEL_BUFFERS=1` with the Release runner and the selected ShaderImport tool. This authors both languages, verifies 21 independent encoded-format vectors per language, raw/queued transfers, native image-store byte parity, signed/unsigned and concurrent atomics, graphics pixels and warmed changing recording/readback. `GLSL` or `HLSL` selects retained Canvas producer checks; run each on Wayland and X11 when claiming both. Export derived modules with `ELECTRON2D_TEXEL_OUTPUT` and validate them independently. Preserve the element-count factory contract, hidden metadata, copied descriptions, actual eight-buffer capacity and original public bytecode. Software pixels and managed-owner brackets do not establish hardware, foreign-platform or native-allocator acceptance.

Polygon boundary coverage uses ELECTRON2D_TEST_POLYGON_AA=managed, gpu or compatibility. Native runs pin software Vulkan/OpenGL and exercise actual edge/texture/alpha/skin/HDR/MSAA/snap/repeat output; optional ELECTRON2D_POLYGON_AA_SHADER selects CanvasHLSL or CanvasGLSL fixtures. The runtime selector forwards existing polygon/stroke/line/parallax profiles without running the aggregate suite. Retained-command fixtures drive explicit native frames during readiness so asynchronous compositor configure redraws cannot change their authored snapshot sequence. Cold inversion/native allocation, foreign platforms and human acceptance remain separate.

File save-and-swap changes use `ELECTRON2D_TEST_SAVE_SWAP=managed`, `gpu`, `compatibility`, `ogv-gpu` and `ogv-compatibility` with the Release runner. Preserve exact Write selection, virtual/symlink/permission/temp ownership, independent concurrent writers, partial I/O cleanup versus completed replacement recovery, PCK/OGV producer cancellation and abrupt-child-exit evidence. Run default file/resource-pack/movie regressions as well. Native profiles require matching OGV and codec assets and actual software Wayland/X11 movie output; they do not establish power-loss durability or foreign execution.

PCRE2 text changes require the focused ELECTRON2D_TEST_REGEX=1 runner, independent tools/coverage/regex_oracle.py vectors compared with --regex-oracle, and the public-only tests/RegEx.Consumer managed/gpu/compatibility profiles. Preserve code-point regions, duplicate/source-order groups, native replacement, retained results, deadlines/resource failure and concurrent owner retirement. Build/run Electron2DRegexCheck normally and with ASan/UBSan/leak detection. Verify immutable source with tools/update-pcre2.py --check (and --archive for reproduction), source/package/native symbols and existing text/font regressions. The private text ABI now includes six regex exports; source-native assets must match. Linux source and software display checks do not establish foreign, old-bundle, native allocator or release acceptance.

Visual graph changes use the public `tests/VisualShader.Consumer` CPU/author/read/gpu
commands and `tools/shaders/check_graphs.py --tool <ShaderImport> --consumer <consumer.dll>`.
Verify all scalar/vector operations, width conversions, explicit default absence,
owned outputs/replacements and borrowed aliases through fresh archives. The SDK
check covers nested/spaced paths, identical output timestamps, changed source with
unchanged timestamp, cyclic/truncated failures preserving the old artifact,
recovery, publish and Clean. Exercise actual imported programs under software
Wayland/X11 and HDR/MSAA in CLR and strict full-trim NativeAOT; measure prepared
64 active/64 idle complete frames separately from cold author/import work. Keep
unsupported graph producers and editor/domain prerequisites exact in coverage.

For unary graph functions, cover every scalar selector and every vector selector
at widths two/three/four. Use independent arithmetic oracles, signed/HDR results,
round-even ties and valid inverse/log domains. Add fourth-component witnesses
before claiming complete vec4 behavior: an RGB output alone discards W. Preserve
Operator/OpType/Function properties across source, callers, stored descriptors,
compiled API/coverage and docs; reject enum sentinels without mutation.

Point-containment allocation investigations use ELECTRON2D_TEST_POINT_CONTAINMENT=cpu|gpu or the public consumer --point-containment [--gpu]. Failure output includes exact owner/all-thread bytes, all case parameters and GC deltas. Optional ELECTRON2D_POINT_ALLOCATION_TRACE loads a separately calibrated Linux CoreCLR begin/end profiler with the matching CORECLR_ENABLE_PROFILING/CORECLR_PROFILER/CORECLR_PROFILER_PATH and fresh E2D_ALLOCATION_TRACE prefix. Do not combine it with E2D_TRACE_STATE. Validate positive/empty controls and native boundaries before interpreting measured object events; retain other-thread control allocations and original failed gates. See [the measurement protocol](components/physics-allocation-measurement.md#in-fixture-point-query-tracing-2026-10-21).

Point-gravity diagnostics use ELECTRON2D_TEST_POINT_GRAVITY=cpu|gpu, optionally ELECTRON2D_TEST_THREADED_COLLIDER=1. They share PhysicsAllocationTrace and the same profiler environment. Tag offsets 32/33 identify the two wide-coordinate intervals and 34 the authored-reset interval; query tags and calibration ranges stay unchanged. Keep all 128 complete measured frames and strict owner/all-thread zero-byte gates. Focused passes do not attribute or replace an aggregate failure; see [the wide-coordinate investigation](components/physics-allocation-measurement.md#wide-coordinate-point-field-aggregate-failure-2026-10-22).

Vector graph dataflow checks must retain Compose's dormant Z/W defaults across
narrowing, whole-table replacement, copy and fresh archives. Refraction eta is a
scalar independent of vector width; removing it must remain distinct from an
explicit zero. Verify every decompose output, parallel producer-to-consumer ports,
all vector widths, valid normalized refraction inputs, total internal reflection,
exact representable grazing boundary, direct alpha and UV-driven vertex/fragment
pixels. Invalidated output edges must fail import without replacing a valid
artifact. Recheck the earlier arithmetic/unary graph consumers after changes to
the shared default-slot/width implementation.

Resident body-motion changes also run `python3 -B tools/shaders/check_runtime.py`
to check all four portable phase variants, dependency invalidation and removal.
Exercise `ELECTRON2D_TEST_GPU_MOTION_QUERIES=1` and adjacent shape, one-way,
priority and compound profiles. Metal/DXIL four-attempt recovery, translation and final-rest
passes share one submission and existing device scratch; verify target execution
and strict zero-byte intervals separately from offline compilation. Windows GPU
diagnostics now runs the production DXIL shaders with only cold-pipeline logging.

For resident pair/contact completion, run ELECTRON2D_TEST_GPU_RESIDENT_CONTACTS=1 with the Release runner, plus the adjacent solver, one-way, CCD, sleep, spatial/shape/motion-query and checkpoint profiles. Preserve the shared/separate submission control, independent error records, bounded capacity recovery, cold empty-world capacity/release checks and strict allocation gates. PhysicsPipelinePerformance accepts ELECTRON2D_PHYSICS_PERFORMANCE_SEPARATE_CONTACT_SUBMISSIONS=1 only for GPU physics and records separateContactSubmissions; CPU use rejects before simulation. This is a stage control inside a complete physics path. Compare standalone CPU/GPU full scenes separately and keep concurrent diagnostic times distinct from accepted hardware throughput and native-window FPS.

PhysicsPipelinePerformance measurements retain the backend process sequence as part of their cohort: ELECTRON2D_PHYSICS_PERFORMANCE_BACKEND=both executes CPU then GPU in one process; cpu/gpu select one backend per invocation. Run each standalone control in a fresh process. Keep those histories distinct, preserve all physical/feature/allocation gates, and require standalone controls before generalizing a paired ratio to ordinary single-backend games. The [scene gate comparison](components/gpu-resident-reports.md#scene-transform-publication-gate-2026-10-21) records a substantial paired/standalone GPU timing difference whose process-history versus time/environment cause remains unisolated. Full physical frames, native window FPS, sampling diagnostics and native allocator totals retain their separate scopes.

For complete headless scene measurements, use precise all-thread allocation counters at the whole measured-batch boundaries. A zero cumulative delta covers every step and the intervening instrumentation without repeated runtime suspensions. Rendered measurements retain per-step precise brackets to distinguish physical work from native input/render intervals. Preserve both strict zero-byte gates and the report's `physicsAllThreadsSampling` attribution; see [the counter protocol](components/gpu-resident-reports.md#headless-allocation-counter-brackets-2026-10-28).

Numeric graph ranges/masks verify every selected layout and pair of layout transitions, including mixed scalar controls, missing-default fallback rules and transactional integer overflow. Keep integer-to-integer conversions exact without an intermediate float. Retain equal-threshold Step, extrapolated Mix, reversed Remap, every W output, vertex/fragment and UV mask evidence. Native arithmetic outside its defined domain (including zero-width Remap) must still import and preserve its source; verify that boundary separately from portable pixel assertions. Preserve the original failed oracle when correcting an unsupported nonfinite expectation.

Integer graph checks must observe all 32 result bits before any float conversion. Retain byte extraction, high-byte witnesses, exact typed archive defaults/absence, every signed/unsigned operation and function, both stages and UV-derived computation. Max is a valid binary operator; EnumSize and unary Max are count sentinels. Keep division/modulo zero, signed minimum divided by minus one, out-of-range shifts and unsupported negative-modulo execution out of portable framebuffer assertions; verify source/import/fresh-load boundaries separately. Retain normalized/HDR integer channel tolerance tight enough to detect a one-byte difference and rerun the preceding graph consumers after compiler/fixture changes.

For comparison/conditional graph changes, preserve the inactive scalar tolerance independently of live input count and preserve Switch condition presence during operand conversion. Exercise every applicable type/function/reduction, both branch choices, strict and zero/negative tolerances, exact integer bytes, vector W and UV-dependent decisions. Verify that a stale edge into a hidden tolerance port fails import without replacing a valid target. Nonfinite classifier fixtures name their concrete producer arithmetic and tested backend; do not infer cross-backend domain behavior from those pixels.

Matrix graph changes must cover all sixteen components, both multiplication orders, component operations, homogeneous 0/1 vector products, all decomposition outputs, determinant and non-affine fourth-row data. Use nonsymmetric independent-oracle fixtures to detect transposed literals; verify exact portable row-order bytes separately. Normal matrix/nonmatrix connections reject; forced authoring remains available and invalid import preserves the target. Keep numeric matrix data separate from scene transforms and renderer matrix ABI producers.

For graph parameter changes, cover every numeric role, direct and cross-stage references, shared declaration aliases, default absence and retained disabled values, all qualifiers and explicit/automatic slots. Exercise all sixteen slots together and verify compiled member offsets independently. Keep exact signed/unsigned bytes, all matrix components and Color alpha observable. Verify range/enum/Color descriptor metadata, malformed/duplicate/wrong-owner records and transactional stage/target preservation. Run typed set/reset during prepared active frames and retain geometry; global values remain registry-owned.

UV-to-integer byte witnesses must keep sample values away from integer truncation boundaries. The 64-times UV fixture adds a quarter-unit bias before conversion, retaining the independent expected center value 34 under different framebuffer widths; do not relax its exact byte tolerance to hide interpolation rounding.

Derivative graph changes require all scalar/vector layouts, selector/default conversion and copy/archive checks, affine UV rates in every component and an independently evaluated fractional antialiasing edge. Verify default/fine/coarse SPIR-V opcodes as well as pixels, reject reachable vertex graphs and malformed or misplaced derivative capabilities without replacing either shader stage. Shared VisualShaderFloatOpType changes also exercise MultiplyAdd and recreate old-schema source fixtures before reimport. Native derivative behavior at discontinuities is not an analytic CPU oracle.

Sampled-image graph changes require ordinary/array shape and sampler-port guards,
implicit coordinates/LOD, material/global/default absence, all image hints,
filter/repeat and explicit sampler override/reset. Preserve node/resource aliases
through fresh source and compiled archives, including big-endian sources. Verify
screen/channel ownership, missing normal/specular fallbacks, HDR transfer before
filtering, linear float sources and independent layers/mips. Clear device images
on the GPU between frames and verify refreshed views. Keep pixel readback outside measured intervals; take the final allocation
snapshots before readback. Retain original failed gates. Reject malformed/duplicate/wrong-owner image records, incompatible
screen/qualifier/shape/default inputs and capacity excess without replacing the
target or either stage. Recheck numeric graphs, sampler arrays, authored vertex
and lighting/glyph consumers after shared reflection/backend changes.

UV/color graph changes require both stages, all selector transitions and default
presence through copies and fresh archives. Verify stored-but-implicit UV, absent
pivot restoration, negative/zero/polar controls and per-vertex interpolation.
Keep all color branches and transfer thresholds observable, including signed/HDR
values; bound F16 target conversion per channel rather than assuming CLR rounding.
Execute UV/image/color material chains with live parameter update/reset and retain
zero-allocation frame intervals. Undefined divide/center boundaries require
import/load evidence without portable pixel assertions.

Compiled custom visual graph changes also run the public Custom CPU/authoring
consumer, the compiled registered import host and tools/shaders/check_custom.py.
Check source/default/option budgets, explicit zero/inactive choices, derived
schema ordering, copy/factory aliases, reachable type scope, both-stage function
setup, numeric/image inputs and native return/discard. Verify failed imports and
SDK callback deadlines preserve output bytes/timestamps. Run
python3 -B tools/shaders/check_host.py for the shared external-producer boundary.
Keep custom runtime consumers independent of tool/backend assemblies. Software
rendered and AOT evidence remains scoped to its exact source cohort and profiles.

For Custom graph dependency changes, run the root --visual-shader-custom-topology
probe and the Custom author/import/render corpus. Cover last incoming edges,
selected versus overridden cycles, callback count and disconnected providers in
the other stage; ordinary graph and provider traversal must select the same edges.

For material placeholders and recovery, run `ELECTRON2D_TEST_MATERIAL_PLACEHOLDER=managed`, `gpu` and `compatibility` with the Release runner. Managed verification includes a fresh-process restoration child, all source material kinds, worker policy capture, shallow/deep/local graph identity, missing-payload bundles/external revival, cache rollback/replacement, concrete subtype rejection and known versions 1–7. Run missing-texture, missing-resource, resource-archive, deferred-scene and resource-base profiles after archive role changes. Native profiles require missing/restored pixels and 64 prepared material RID switches; use `ELECTRON2D_CAPTURE_DIR` to retain images. Zero owner-thread managed bytes do not establish native-heap, physical GPU or foreign acceptance.

Native host-memory changes use `ELECTRON2D_TEST_HOST_MEMORY=1` and the public-only
`tests/HostMemory.Consumer` contract, also included in the aggregate root suite.
Run finite/unlimited RLIMIT_STACK child profiles and strict full-trim NativeAOT.
The optional Linux C fixture in `Fixtures/OSMemory.c` denies sysinfo or prlimit64
only on disposable test threads; `ELECTRON2D_MEMORY_FIXTURE` selects its absolute
shared-library path in a separate root-test process. Independent fields must
survive native failures, and zero/overflow arithmetic must retain its sentinel
contract. See [host memory](components/host-memory.md) for exact platform meanings
and qualification limits.

Clipboard image changes use the public-only `ClipboardImages.Consumer` and its
private Mutter `run_native.py` producer/reader driver. Exercise all MIME roles,
preferred-format failures, absent/file-list/empty/oversize offers, copied ownership,
predicate retirement and actual image-to-texture pixels on each claimed renderer.
Use strict full-trim NativeAOT separately. See [clipboard image reproduction](components/clipboard-images.md#verification-and-reproduction).

After changing native sampler creation or copied sampler settings, follow [native sampler border checks](components/sampler-borders.md#executable-checks), preserve version-one shader metadata reading, and rerun native layout pin checks plus the public CLR/AOT consumers. Window presentation and foreign-platform execution remain independent gates.

When changing sampler coordinates, run [the coordinate consumers](components/sampler-coordinates.md#executable-checks), including operation failures and derived shader checks; preserve reading both older metadata versions and validate the actual bound view. Match the complete private render ABI in produced bundles.

After changing device limit production or validation, run the public `tests/DeviceLimits.Consumer` native consumer, related compute/graphics regressions, native bridge check and package export tests; preserve the distinct logical-set/profile/native boundaries in [device limits](components/device-limits.md).

After changing format-filter queries or configured sampling, run [native format checks](components/format-filter-support.md#executable-checks) in ordinary and controlled masked children, plus connected sampler consumers. Keep the immutable cache within the physical-device lease and match all six private render exports in produced bundles.

Native HDR output checks share `tests/Electron2D.Tests/WindowOutputTests*.cs` with
`tests/WindowOutput.Tests`. Select `ELECTRON2D_TEST_HDR_OUTPUT` for managed/fresh
scene, transfer/screen pixels and native CPU/GPU/compatibility profiles. Build the
matching native render bridge when current main changes its exports. Controlled
HDR metadata, actual native formats and physical HDR-display acceptance remain
separate; see [native output](components/native-hdr-output.md).

For native alerts or message-box marshalling, run [the public managed/failure/native consumers](components/native-alerts.md#executable-checks) in CLR and strict NativeAOT. Preserve the -1 sentinel when a native provider leaves selection unwritten, owner-thread guards, NUL validation and partial-marshalling cleanup; inspect native text pixels separately from callback success.

For native mirror-clamp addressing, run the public `mirror` and `mirror-canvas` sampler consumer profiles in CLR and strict NativeAOT, then repeat with the SDL3_DYNAMIC_API failure shim and `unavailable`. Require observed extension refusal, working ordinary sampling and recovery after failed configured preparation. [The mirror-clamp contract](components/sampler-mirror-clamp.md#executable-checks) owns reproduction and qualification limits.

Writable graphics storage: build/run `tests/DeviceGraphicsStorage.Consumer` in Release with native overrides. Run `graphics`, `source`, `edges`, `aliases`, `precision` and `canvas`; repeat with strict trimmed NativeAOT. `edges` covers GPU-produced indirect batches, zero count, same-pass readonly consumption, two views/subpasses, depth and invalidated borrowed sets. Use the test-only `mask-features.c` shim for independent vertex/fragment/both feature refusal before shader/binary publication. Prepared cycles assert zero owner-thread managed bytes. Recheck native render memory/ABI guards, specialization/push, readonly images/texels, depth/MSAA, ordered passes and multiview because pass closure is shared. Native allocations, performance and foreign platforms remain separate gates.

Formatted graphics checks extend DeviceGraphicsStorage.Consumer with texels, texel-source, texel-formats, texel-atomics and texel-canvas, on CLR and strict NativeAOT. Run texel-core, texel-core-formats and texel-core-atomics through test_shader8_mask.c with ELECTRON2D_TEST_MASK_SHADER8=api, plus texel-unavailable-vertex/fragment/all through mask-features.c. Export public fixture modules with ELECTRON2D_GRAPHICS_TEXEL_OUTPUT. ELECTRON2D_TEST_TEXEL_BUFFERS=1 additionally verifies derived capability removal/preservation; ELECTRON2D_TEXEL_OUTPUT exports raw and full graphics transformations for spirv-val. These profiles preserve HLSL fixed-array compilation limits and separately exercise its derived SPIR-V specialization.

For attachmentless framebuffer or pass-state changes, run the public graphics-storage consumer modes `empty`, `empty-source`, `empty-canvas` and the controlled `mask-empty.c` / `empty-unavailable` and `empty-failure` profiles in CLR and strict NativeAOT. Rebuild the render bridge, run its C ABI/extent/pass/memory check and package export tests, and repeat connected ordinary graphics, MSAA, ordered passes and multiview because pass begin/end is shared. See [attachmentless graphics](components/local-compute.md#attachmentless-graphics).

For primitive discard or shader-stage/container changes, follow [the public/native checks](components/raster-discard.md#executable-checks). Rebuild the private bridge and run discard source/binary/canvas, independent stage refusal and missing/failed native adapter profiles in CLR and strict NativeAOT. Recheck graphics specialization, push, sampled variants, MSAA, passes/multiview and version-one binary regressions; run the native concurrent creation/reference rollback checks and package export tests.

For copied graphics line width, follow [the executing contract](components/wide-lines.md). Run wide/source/canvas and physical-feature/device-creation/native-pipeline refusal profiles in CLR and strict NativeAOT; preserve ordinary graphics/storage siblings, primitive discard, per-view/pass and sampled component variants. Static native clamping/precision must not be presented as dynamic line-state or foreign-backend acceptance.

[Native sample controls](components/multisample-state.md) execute copied masks, sample shading/fraction and alpha-to-one, with independent feature refusal, sample-rate shader publication guards and real resolve/storage/canvas consumers. Keep nonempty mask vector shape, copied variant state and the native/platform qualification limits together.

Specified application launch uses `ELECTRON2D_TEST_OPEN_WITH_PROGRAM=1` with the Release executable runner. It starts a real consumer apphost, verifies literal ordered paths and nonblocking child lifetime with a controlled gate, and rejects invalid complete argv before publication. Rerun `ELECTRON2D_TEST_OS_PROCESS=1` when changing its shared process owner. macOS .app/open-launcher and foreign-platform/sandbox acceptance require their native hosts.

Animation movie completion uses `ELECTRON2D_TEST_ANIMATION_MOVIE=portable`, `gpu` or `compatibility` with the Release runner; `ELECTRON2D_ANIMATION_MOVIE_OUTPUT` selects its output directory and optional `ELECTRON2D_ANIMATION_MOVIE_CASE` selects one failure/control profile. Native profiles require a real window host and verify the final pose/PCM and PNG/WAV/AVI/OGV closure. Rerun scene animation/blending and existing movie/encoder profiles for changes to this shared completion/stop path. Final successful-step quit still records that step; before-step quit and failing steps do not append output.

For the Linux FileChooser adapter, run `python3 -B tests/Electron2D.Tests/check-file-portal.py` after building the linux-x64 root runner. It starts a private bus without activation directories and verifies actual Wayland clients against controlled native D-Bus responses, including absent/old interfaces, early/different handles, malformed results, owner loss, callback errors and GPU/Compatibility FileDialog consumers. Its final bus-loss child retains ordinary libdecor and verifies failure delivery plus the native GIO policy/teardown restoration. Set ELECTRON2D_PORTAL_TEST_OUTPUT to retain receipts. Separately run `python3 -B tests/Electron2D.Tests/check-file-portal-live.py` with that output path and ELECTRON2D_PORTAL_FOLDER pointing to its fixture folder to verify the installed portal via uniquely titled AT-SPI selection/cancellation. That live harness uses observed Nautilus chooser action names and is a local host profile, not a portable native-UI driver. Repeat theme portal checks when the shared libdbus helper changes. See [native file portal](components/native-file-portal.md) for exact scope and foreign/native-heap limits.

Native file-option changes require the existing `check-file-portal.py` private-service matrix and `check-file-portal-live.py` with `ELECTRON2D_PORTAL_LIVE_OPTIONS=1` against the installed chooser. Preserve the basic live profile too. The options profile changes real native controls and checks returned values independently; its current GTK menu exposes blank item names, so the harness locates unique checked/expandable/checkable native states after opening the observed options control. Protocol fixtures still validate exact IDs, labels, captions and defaults. Verify request edits, cancellation defaults, repeated presentation, pending-policy guards and root/empty-caption custom fallback on both current renderers. Retain the [native boundary and qualification limits](components/native-file-portal.md#native-checkbox-and-choice-controls).

Canvas follow-scale policy changes run `ELECTRON2D_TEST_CANVAS_SCALE=gpu|compatibility` on the native host and compatibility under the dummy/software driver. Preserve authored-unit parent snapping, zero/negative/fractional values, complete RID attachment matrices, root/nested output, capture/owner/session guards and the two strict 64-frame retained allocation intervals. Keep canvas layer, coordinate/snap and parallax regressions; the existing RenderingRuntimeTests flags require the outer `ELECTRON2D_TEST_RENDER=1` entry selector. See [the canvas contract](components/canvas-rendering.md#canvas-follow-scale-preview).

For native text entry, run [the dummy, native and running-engine checks](components/native-text-input.md#executable-checks). Preserve exact initial-text round trips, copied accepted strings, cancellation without callback, native parent retention, modal pump guards and callback cleanup/reentry.

For device timestamps, run [native ABI and local/renderer checks](components/gpu-timestamps.md#verification). Preserve completed-only publication, independent clock units, positive captured capacity, empty/used list boundaries, reset/read/fence lifetime and prepared active/idle allocation checks.
