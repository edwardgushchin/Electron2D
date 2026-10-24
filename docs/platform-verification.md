# Platform verification matrix

Last updated: 2026-10-23

## Current release checks

The complete ordinary Linux x64 suite at `1401a95ff` passes in 617.6 seconds
with the audited native closure. The complete ordinary macOS ARM64 suite at
`4a80378d0` passes in 37 minutes 43 seconds, including the final separate-thread
CPU cohort. Its x64 job in run 39991319732 fails later in separate-thread
joint-policy edits at 0/9,024 owner/all-thread bytes. The preceding ordinary x64
run 39976121450 at `c847dc38d` reached the CPU sleeping-Area field
check, then expired at the 90-minute job budget; it is not a successful gate.
The manual macOS runner now retains native thread samples during prolonged
silent intervals without replacing the original exit/marker checks.

Profiled run 39987153000 at `3fa842b13` completes ARM64 successfully. Both
architectures pass all four MP3/Ogg active/idle owner-byte checks; their physical
intervals contain no object events. The x64 aggregate instead fails later in
separate-thread joint-policy edits at 2,784/19,336 owner/all-thread bytes. That
interval lacked a physical marker and remains unattributed. The joint fixture
now retains exact physical boundaries, while standalone ordinary/traced controls
remain separate from complete target acceptance. The next profiled x64 run
39996059802 at `87042214a` fails earlier in public GPU contact-policy edits at
0/64 bytes, before the late joint-policy interval. That contact interval now has
physical markers without changing its steps or zero thresholds. Profiling does
not replace an ordinary gate or explain an older untraced allocation delta.

Windows diagnostic run 39997310087 at `dbbfc9c2b` passes the complete public GPU
compound fixture, including scene/raw/Area slots, momentum, private history and
128 cycles at 0/0 bytes. Its temporary optimized portable motion candidate seeds
query bounds from the first vertex and retains sweep/final-contact state in the
existing device buffer. The first real result has safe/unsafe fractions
0.49609375/0.5 and travel 9.921875, matching the local control. The complete
instrumented Windows control, run 39999737324 at `7e186ac72`, later fails
`PhysicsServerJointTests.VerifyJointExceptionContributions`: its combined
exception-snapshot/zero-motion recovery assertion fails after disabling the final
joint contribution. The signed runtime exit is -532462766 (`0xE0434352`). Its
first-query diagnostic receipt describes the earlier vetoed request and cannot
identify which half of the later combined assertion failed.
The fixture now separates snapshot count and motion membership and logs the same
query's travel/depth/normal. Standalone complete public joint and functional motion
cohorts run before the full manual Windows executable, retaining their own raw
exits and logs; failure in any cohort remains failure. The full executable retains
its unselected original workload and the overall 90-minute job budget.
The source contains the portable bounds, sweep and final-contact retention at
`1401a95ff`, without temporary shader rewrites or result-lane observations. Its
separate complete Windows run 40003829560 remains in progress. Neither the focused
instrumented pass nor the passing local Linux suite closes Windows acceptance.
Raw focused logs from run 40006886721 at `1823f7745` then show zero GPU recovery
with zero remaining exceptions, and zero direct recovery travel versus CPU
(0, -1.8069474). Both native exits are -532462766 (`0xE0434352`); API step success
under `continue-on-error` does not supersede these failures. Recovery state now
uses the existing prefix across geometry calls, preserving its numerical work.
Run 40009263183 at `cb7ddac24` repeats both zero-travel failures after storage
retention; its full executable is correctly skipped and the final job fails.
Portable geometry now returns both contact snapshots explicitly for consumption
by recovery/final sampling. That new candidate still needs native and complete
release gates.

Profiled macOS run 40002310019 at `e942c331a` completes both full suites:
ARM64 in 31 minutes 9 seconds and x64 in 53 minutes 57 seconds. Each root trace
has 5,923 balanced boundaries, successful stacks and `SHUTDOWN recursive:0`.
CPU/GPU contact-policy and serial/separate-thread joint-policy intervals each
report 0/0 bytes with no physical allocation events. This is before the portable
recovery changes; historical failed deltas remain unattributed and the ordinary
final-source matrix is still required.
The explicit-contact-pair Windows candidate at `a5749f3a9`, run 40012351791,
repeats the zero-recovery failure in both focused processes. Optional temporary
recovery tracing now observes the traversal/contact/reducer boundary through
existing result padding; its output is diagnostic, not production acceptance.
Run 40016536236 at `8cdedf1b3` fails both focused processes. Its trace retains
one accepted recovery plane and positive depth 2.0799999237 but never enters
projection, then marks the unchanged correction converged. The role-only
specialization at `eec1e6dce` has identical initial-stage HLSL after temporary-name
normalization and is retired as a correction. Portable recovery now calls
projection after scan returns; its complete ordinary Windows run and final source
matrix remain required.
The ordinary role-only run 40018224090 repeats both zero-recovery failures,
retaining unchanged source hashes and skipping the full suite after preflight.
Run 40019553649 at `04a1071f4` passes both public-joint and functional-motion
preflights with raw zero exits and unchanged production motion source hashes
(artifact 12530257587). GPU joint-filter recovery matches CPU travel 13.12215;
64 warmed public scene/compound motion queries retain 0/0 managed bytes. Its
full executable begins at 20:39:17 UTC; completion and the final matrix remain open.

Final-source matrix [40021281291](https://github.com/edwardgushchin/Electron2D/actions/runs/40021281291)
at `2ba2f42fd` passes all six desktop native consumers. Its Windows ARM64 job
`120658099378` fails the complete public GPU compound preflight while mapping
resident shape-query results. The error includes a missing optional PIX runtime,
but that message has not been established as causal. CDB artifact `12532238180`
records the system `C:\Windows\SYSTEM32\d3d10warp.dll` and an access violation
in `d3d10warp!PixelJITProcessor::ExecuteJIT+0x2c`. The ordinary RID workflow does
not install the diagnostic's pinned driver. A paired ARM64 control now runs the
same compound, joint and motion cohorts with app-local WARP 1.0.21 and confirms
its actual loaded identity. That control and ordinary target acceptance remain
pending; neither an exception location nor a driver version proves root cause.

That paired ARM64 run, `40027608406` at `eaa144311`, fails all three raw focused
exits with `0xC0000005`; the full executable is correctly skipped. Artifact
`12533368686` confirms actual app-local WARP 1.0.21 selection and unchanged motion
source hashes. Its CDB artifact `12534315453` reaches
`UMDevice::CopyDescriptorsSimple`. Native reflection and SDL's descriptor-copy
path expose a host mismatch: the first three motion stages declare three
writable buffers but were submitted in a four-binding pass. The host now binds
three until final output, which retains four. Corrected ARM64 and final ordinary
matrix execution remain required.
The separate x64 full run `40019553649` reaches its 65-minute fixture deadline
while continuing cold pipeline creation (artifact `12534012054`, 9,274 log lines).
It has no zero exit or completion marker and does not establish full acceptance.
Full Windows RID checks now use the audited app-local WARP 1.0.21 test fixture
with a 180-minute job budget. Manual diagnostics allow 150 minutes for the full
executable within a 180-minute job. No tests or zero-byte limits are removed;
complete results on this software-driver baseline remain required. The test DLL
is absent from engine packages and public consumers.

Linux x64 job `120658099003` in matrix `40021281291` fails the complete compound
CastRay check at `x=60`, velocity `(1200, 0)`, instead of stopping at the wall.
Diagnostic [40029824468](https://github.com/edwardgushchin/Electron2D/actions/runs/40029824468)
at `3bddfb403` reproduces the same failure in its original, traced and restored
explicit-Lavapipe processes (artifact `12533903315`). The four substeps find
6/2/4/2 candidate pairs and each returns status zero, fraction one. This excludes
an empty broad phase as the immediate cause. The host has Mesa 26.0.8, LLVM 21
and an AVX-512-capable AMD EPYC 9V45; prior local controls on the same Mesa version
use a Ryzen 5700X without AVX-512 and do not explain the hosted failure. The next
diagnostic compares 128/256-bit vector widths with the complete unchanged
fixture, actual device identities, binary/source receipts and cold shader caches.
CPU SIMD dependence remains a hypothesis; no ordinary driver policy is changed.
Run `40030984267` at `426798799` passes the baseline on an EPYC 7763 with the same
Mesa package version (artifact `12534059802`). Its conditional SIMD controls are
skipped, so that pass does not explain the EPYC 9V45 failure. The diagnostic now
executes both explicit-Lavapipe SIMD controls even after a passing baseline and
preserves failure from either control. A separate local 256/512-bit comparison
uses the isolated CI Mesa library without changing installed system packages.

The failure-only temporary replay also records raw device inputs immediately
before CCD dispatch: bodies, shapes, geometry, vertices, candidate pairs, centers
and active position corrections, with scalar settings. It uses separate completed
readbacks without changing those buffers or shader bytecode. The source receipt
hashes the temporary helper, which is removed before untraced controls. A traced
pass alone cannot clear a baseline failure. The local prepared-helper build and
complete compound execution pass with 22 settings records and 154 raw buffers;
body/shape/pair/center lengths match their recorded counts and strides.

Run `40035835749` at `d5c677614` reproduces the same CCD failure on another
AVX-512-capable CPU, EPYC 9V74 (artifact `12536933573`). The default, traced,
restored-Lavapipe and explicit 256-bit controls reach `x=60`, velocity `(1200, 0)`.
The explicit 128-bit control instead fails earlier in compound contact generation
with invalid geometry/contact state; both SIMD processes exit 134 and report the
requested device width. Reducing SIMD width therefore does not clear this gate.
The first compiler-profile control at `c64fdd639` also selects portable shape-query
modules because its C# `or` pattern joins them to CCD. Its local complete compound
pass on Mesa 26.0.8/LLVM 21 in 96.3 seconds, including 128 cycles at `0/0 B`, is a
combined-profile result, not a CCD-only comparison. The shader manifest remains
identical. The corrected temporary selector splits the CCD condition from the
driver-qualified shape-query family. It records selected CCD and shape-query
module names and rejects any portable shape-query module on Vulkan, retaining
the same numerical GLSL, iteration limits, inputs, bindings and full workload.
An executable C# predicate probe checks 18 driver/module cases. The corrected
local full compound control passes with all four original Vulkan shape-query
modules and portable CCD explicitly observed, retaining 128 cycles at `0/0 B`.
A local 512-bit Lavapipe control on the AVX2-only host
also completes the unchanged fixture in 352.6 seconds with zero measured bytes;
width alone does not reproduce the hosted failure.

The isolated control `40038187291` at `2dd9c3cbe` fails on Intel Xeon Platinum
8573C (artifact `12538870164`). Its default, traced, restored, SIMD128, SIMD256 and
CCD-only portable processes retain the same `x=60` failure. Artifact `12538437863`
from EPYC 9V74 run `40036438679` also retains four failing CCD input snapshots.
Across the first three substeps, both hosts' settings and all geometry, vertices,
shapes, pairs, centers and corrections are byte-identical to the passing local
control. Bodies differ only at offset 184: the mover's flags are `90124` instead
of `90508`, losing CCD bit 256 and reporting bit 128 before the CCD shader runs.
The host still records one CCD body. This moves the investigation to body edits;
changing CCD compiler profiles is not a fix. The completed compiler-profile probe
is removed from the workflow; the next raw snapshot additionally retains the
resident command buffer, without changing numerical work or production shaders.
A separate unchanged GPU-fixture process disables managed JIT SIMD intrinsics
with `DOTNET_EnableHWIntrinsic=0`; the native Vulkan driver keeps its normal CPU
capabilities. This distinguishes managed staging from native command execution.
Its raw exit and full completion marker remain required, and its failure also
keeps the diagnostic red. This setting never applies to ordinary CI or production.

Body-command run `40040622719` at `972a17963` reproduces the failure on EPYC 9V45
(artifact `12539590909`). For the first three substeps its complete 880-byte
resident command prefix is byte-identical to the successful local control, with
SHA-256 `408406d155e78c6a6e8e29d8256b4a5c2fafee6497bd6b62c97b452e56799e7d`.
The mover command includes CCD bit 256 while the resulting body flags omit it;
the managed-intrinsics-disabled process also fails. Scalar header-policy/current-flag
edits at `30e956813` pass the local controls but run `40043371390` still returns
CastRay `x=60`, velocity `(1200, 0)` and flags `90124` on EPYC 9V74 (artifact
`12541025364`, ZIP SHA-256 `47bbf66314e1f607e2590f809b33bf315e38b288149c419bd07fc12cbc320af1`).
Baseline, traced, restored and managed-scalar logs reproduce the prior failure;
the scalar edit candidate is removed. Failure-only body/active-command snapshots
after individual body dispatches now distinguish edit application from subsequent
motion stages. They add copy passes within the existing submission/fence, are
capped at 24 per small CCD world, and do not establish untraced acceptance. Exact
source restoration is checked before controls. The policy regression now reads
flags both before and after integration after the original cold public compound
workload. The public path remains first so the new store regression cannot warm
the process before that first-use contract.
See [the flag investigation](components/gpu-resident-bodies.md#scalar-body-edit-flags).

Run `40044866241` at `e5c8fbd4b` passes its complete compound and policy
cohorts on EPYC 9V74, including SIMD128/256 and managed-intrinsics-disabled
controls (artifact `12541151680`, ZIP SHA-256
`4cef12828f1bc414672f35dfd5dcc11786641b52a9a9d3442b217dcd2d2d399e`).
That runner invoked the new resident policy regression before the public compound
path. This changes process warmup before the previously failing cold case; no
root-cause correction or cold acceptance is inferred. The compound selector now
restores the original public-first order, with the flag regression afterwards.

Windows x64 run `40036769271` at `78754df51` passes the focused compound,
joint and motion cohorts, then its complete suite fails in
`PhysicsPointGravityFailureTests.FiniteLimit(GPU)`: field reduction rejects the
maximum finite point force. Artifact `12539963873` retains 11,608 lines and raw
exit `-532462766` (`0xE0434352`), with no completion marker. The log SHA-256 is
`c1c1a454e88101ba66ab0738c9db0ac7c4409016ac763ff85d6c596d7875f814`.
This is a managed field-validation failure after continued pipeline progress,
not another timeout. Offline HLSL/DXIL inspection shows `ldexp` lowering through
an `Exp` operation on exponent 128 before the mantissa multiply. The candidate
reconstructs this top exponent as `ldexp(mantissa, 127) * 2`; exact limits on both
axes/signs and the complete unchanged range/overflow cohort remain required.
Corrected native Windows execution is still open.

The profiled macOS run `40035786188` also completes its full x64 suite at
`d5c677614` (job `120694782183`, artifact `12539342858`, 7,009 lines, completion
marker and raw exit 0). The full-runtime log SHA-256 is
`f123b3f9d7ebc9b15c70f956b311c2ff2cc8c0674fb4261c8b660085a98d3968`.
Active/idle Ogg intervals report zero owner bytes. This complements the ARM64
diagnostic result; final-source untraced platform acceptance remains separate.

The earlier profiled macOS x64 job `120680942051` in `40031605911` completes the
full suite at `426798799` (artifact `12538297123`, 7,009 lines and final completion
marker). Its idle-Ogg interval and owner/separate-thread joint policy intervals
all report zero bytes. This is a profiled complete result on that revision; it
does not attribute the earlier nonzero deltas or replace the final untraced gate.

The corrected Windows ARM64 run `40029790832` at `3bddfb403` passes all three raw
focused exits with app-local WARP 1.0.21 (artifact `12535550765`). Windows x86 run
`40031574214` at `426798799` repeats those three zero exits, including shape cast
and 128 compound cycles at `0/0 B` (artifact `12536460017`). Their full untraced
executables remain separate gates. Ordinary system-WARP x64/x86 jobs in the older
matrix instead return shape-cast fractions `1/1`.

The same older matrix finishes with all 18 Build profiles successful and failed
Tests. Its macOS ARM64 idle-Ogg interval reports `3360` owner bytes; macOS x64
separate-thread joint edits report `2712/19264` owner/all-thread bytes. Profiled
ARM64 run `40031605911` fails earlier at GPU SegmentShape/CapsuleShape casts with
`0/64 B`. Its physical trace identifies a worker's one-time CLR lock constructor,
so the test entry point now prepares that constructor before measurements; the
[allocation report](components/physics-allocation-measurement.md#runtime-lock-initialization-2026-10-23)
retains the exact scope. This does not attribute the older Ogg or joint deltas,
and corrected complete platform execution remains required.

Final complete CI, public package restoration, rendered consumers and release
publication remain pending. The current declaration register is not a substitute
for those gates; no published 0.2.0-alpha release is claimed by these checks.

## Earlier release checks

Fresh-process desktop fixtures now restart the current executable and add their
assembly only for a framework `dotnet` host. Run `39890302781` reached terrain
scene persistence on the x64 macOS apphost, then failed because its child selected
an ARM64 SDK host and rejected the published x64 `libhostpolicy.dylib`. The same
host-selection problem reproduces locally with a rejecting PATH host under an
apphost, while the framework-host control retains its own executable directory.
Scene/asset, font, animation, rendering, navigation and time consumers share the
correction. SDK compilation and the independent decoder keep their separate host
selection. Local launch checks cannot replace corrected x64 macOS execution.
Run `39899252158` progressed through those child workflows and reached CodeEdit,
where the test sent Control+Shift+Enter although the action uses Command on
macOS. The fixture now uses the existing command-or-control remapping; its
indentation assertion and runtime input behavior are unchanged. Corrected target
execution remains required. The separate ARM64 full suite still reports 64
all-thread managed bytes in the contact-history measurement, with zero owner
bytes; that allocation failure remains unresolved.
Manual run `39913767182` at `4d05b1fa` passes fresh CodeEdit and public GPU
contact-history checks on both macOS ARM64 and x64. ARM64's complete suite passes
the earlier contact-history interval, then fails Button active frames with 12288
owner-thread bytes. x64 instead fails CapsuleShape with 0 owner/64 all-thread
bytes. The 64-byte source remains unresolved. The Button failure reproduces on
Linux with `en_US.UTF-8`: six per-grapheme regional language substrings per frame
account for all 192 bytes/frame across 64 frames. Stack-backed priority keys and
ordinal span lookup remove those strings while preserving prefix/case/tier rules.
Fresh complete macOS execution of that correction is still required.

Windows control `39913764076` confirms app-local WARP 1.0.21 and passes all four
shape-query kernels and geometric casts with zero allocations. Body-motion
pipeline creation still aborts in `ScopeNestedCFG::runOnFunction` after sharing
one traversal call. The smaller DXIL is not a driver fix. A temporary diagnostic
now bounds that state machine to its existing maximum eleven geometry calls and
fails explicitly on exhaustion. This control is not a production numerical limit
change or current Windows acceptance. Bounded control `39915318856` also fails
at the same return offset. Disassembly of the exact pinned DLL identifies the
`0x80aa000a` throw block reached when loop-preheader lookup returns null; the
single-backedge check uses a distinct throw site. Input DXIL already has canonical
natural loops, so the preceding WARP transformation remains under investigation.

Run `39890302781` also aborted on Linux ARM64 in `libusb` after
`pthread_key_create` failed when the next display opened. Local Linux x64 controls
retain stable native TLS capacity after initialization across 1024 gamepad cycles,
512 Mesa device cycles, 512 device/pipeline cycles and 512 completed zero-count
physics dispatches. Full traced run `39904064029` attributed 1,011 live keys to
the system `libvulkan_gfxstream.so`, with no matching deletion; `libusb` failed
after all 1,024 slots were consumed. Isolated Vulkan instance lifetimes reproduce
one lost key per instance with gfxstream also enumerated, without SDL or the
engine. Lavapipe alone retains its initial capacity. Headless Linux CI now selects
the installed Lavapipe ICD explicitly. Untraced ARM64 run `39909054513` at
`6e65b171` completes all 1085 log lines with `Electron2D checks passed.` and the
unchanged GPU/allocation assertions. This establishes the scoped headless runner
correction; the system gfxstream driver remains unchanged and the final release
revision still requires its complete matrix.

This matrix records execution on hosts and devices available during this audit. A successful build or packaged native library is not a runtime check. A blocked backend is a measured result for this build and device, not a claim that an entire platform can never support it.

| Runtime tested | SDL_Renderer canvas | SDL_GPU canvas | Shader pixels | Physics |
| --- | --- | --- | --- | --- |
| Linux Wayland, current host | Pass: compatibility pixel suite | Pass: GPU pixel suite | Pass: HLSL and GLSL material suite | Pass: managed Box2D suite and native interpolation pixels |
| Linux X11 through XWayland, current host | Pass: compatibility pixel suite | Pass: GPU pixel suite | Pass: HLSL and GLSL material suite | Pass: native interpolation pixels; same managed Box2D runtime |
| Samsung SM-A256E, Android API 36, `arm64-v8a` | Pass: `opengles2`, red readback `(1,0,0)` | Pass: Vulkan device with optional clip-distance, depth-clamping, indirect-first-instance and anisotropy disabled; red readback `(1,0,0)` | Pass: pinned HLSL SPIR-V material, red readback `(1,0,0)` | Pass: dynamic rectangle rests at `Y=80.009`, vertical velocity `0` after 120 steps |
| MiTV-MSSP3, Android TV API 30, `armeabi-v7a` | Pass: `opengles2`, red readback `(1,0,0)` | Blocked: OpenGL ES 2 is advertised, Vulkan is not; SDL reports no supported GPU device even after optional features are disabled. `auto` falls back to `opengles2` and preserves the red pixel | Electron2D ShaderMaterial blocked before submission; standalone GLES2 fragment shader rendered red `(255,0,0,255)` with GL error `0` | Pass: dynamic rectangle rests at `Y=80.009`, vertical velocity `0` after 120 steps |
| WebAssembly in the isolated SDL 3.4.16/Emscripten browser probe | Pass: direct SDL_Renderer red canvas in Chrome | Blocked: Chrome has WebGL2 and a WebGPU adapter/device, but this SDL build lists zero GPU drivers and `SDL_CreateGPUDevice(SPIRV)` returns null | Electron2D ShaderMaterial blocked; standalone Chrome WebGPU WGSL fragment shader rendered a red frame | Pass: managed Box2D contact at `Y=80.009` after 120 steps |
| iOS/tvOS | Not run: native simulator contracts do not cover rendered canvas output | Not run | Not run | Pass: headless scene physics in all four simulator apps; no physical-device execution |
| Windows/macOS | Not run: headless CI does not cover rendered canvas output | Not run | Not run | Earlier full headless desktop passes; current alpha candidate still has the Windows pipeline and macOS full-history allocation gates described above. Rendered interpolation remains separate. |

<a id="character-movement"></a>
## CharacterMovement application

On 2026-10-06, the shared [CharacterMovement](../examples/CharacterMovement/README.md) application ran locally on Linux Wayland, the Samsung SM-A256E ARM64 phone, MiTV-MSSP3 32-bit Android TV and Chrome. The owner checked the phone, TV and browser and confirmed that they work. Android applications remain installed.

Desktop uses a fixed 800-by-600 window and constant logical character size. Mobile, TV and browser surfaces use native pixel resolution with a uniform canvas transform: the short side is 600 logical units, and the long side expands to the actual aspect ratio. The field fills the surface without bars, cropping or distortion. Phone touch drags the character; TV direction buttons and desktop/browser arrows use the same scene actions. The browser fills its document and has a user-gesture fullscreen button. App icons derive from the existing app-icon artwork.

The focused `ELECTRON2D_TEST_CHARACTER_MOVEMENT=1` Wayland check covers GPU and compatibility rendering, a yielding compatibility loop, fixed desktop size, arrows, injected touch, action input, focus cancellation, quit codes, startup rejection and owned cleanup. Five surface sizes check the aspect-ratio transform, including portrait and 4K. Chrome phone emulation checks portrait layout and touch dragging; a physical mobile browser was not checked. These automated checks are separate from the owner's physical acceptance.

The project selects all eighteen supported RIDs. Android and browser builds were executed locally; iOS/tvOS application definitions and icon catalogs require macOS/Xcode compilation. Windows/macOS and Apple application execution were not performed for this change. Existing headless CI results above do not prove that this new example runs on those hosts. No new GitHub CI run or package publication is part of this local change. The published NuGet `0.1.0-alpha` does not contain the new RunAsync entry point; mobile/Web example instructions currently use the repository engine source and existing platform-native packages.

`python3 -B tools/examples/test_character_movement.py` passed all eighteen evaluated RID/framework/package selectors and icon references without foreign execution. Final ARM64/ARM32 APKs each have 73 native/AOT files for only their selected ABI, one SDL core and no separate managed SDL wrapper. Minimal JNI bindings are generated internally in the Android engine assembly. The Android contract project compiled against that same internal adapter. Linux self-contained publication passed the native loader/export audit and the notice audit: 68 ELF files, 67 byte-identical engine notices and the additional IBM Plex Sans application notice. The full default local test runner and focused rendered example checks passed.

## Private native package delivery

[Native delivery](native-packaging.md) now separates ordinary managed builds from full native compilation. Local Linux x64 fresh-source and ProjectReference/NuGet consumers restore the three private libraries, execute public text/audio/ENet smoke checks and publish self-contained applications with CMake/Ninja/C/C++ commands blocked. These are executable packaging checks, not renderer, hardware audio or foreign-host acceptance. [Tests run 37278042834](https://github.com/edwardgushchin/Electron2D/actions/runs/37278042834) produced the audited combined x64/ARM64 package, passed the fresh consumer check and restored it in both successful full headless Linux jobs. Linux `0.1.0-preview.1` was published by [run 37281011805](https://github.com/edwardgushchin/Electron2D/actions/runs/37281011805); the matching runtime also passed an empty-cache public nuget.org restore, public text/audio/ENet execution and self-contained publish with native tools blocked. New development package versions and other native-platform obligations below remain open.

## Required native dependency builds

[ADR 0012](decisions/product.md#adr-0012) requires target-specific native dependency builds for every accepted runtime RID under [ADR 0021](decisions/product.md#adr-0021). All eighteen producers and seven `0.1.0-alpha` platform packages passed in run [37392360522](https://github.com/edwardgushchin/Electron2D/actions/runs/37392360522). The downloaded packages contain exactly eighteen RIDs and 155 private native files with matching binary SHA-256, version, minimum engine version and producer recipes. Windows receipts match their own audited Windows checkout recipe, not the Linux checkout's raw-byte fingerprint. Native runtime execution remains a separate gate below; rendering/physics observations above do not establish text or audio support.

| Target RIDs | Current text/audio boundary | Required continuation |
| --- | --- | --- |
| `linux-x64` | Private ICU and FAudio build, package and execute locally. | Retain reproducible builds, notices and regression gates. X11 host checks remain separate from Wayland evidence. |
| `linux-arm64` | Matching-runner native build, combined NuGet packaging and full headless text/audio/network suite passed in CI. | Retain reproducible packaging and target execution; rendering and hardware audio acceptance remain separate. |
| `win-x86`, `win-x64`, `win-arm64` | Complete MSVC producers, WOFF2 decode/raster checks, combined PE package and all three fresh ProjectReference/NuGet consumers passed for `0.1.0-alpha`. Run `37409993215` passed the complete headless suite and trimmed contract on all three architectures; x64/ARM64 also passed the NativeAOT contract. x86 has no supported NativeAOT compiler. | Retain exact wire/asset-byte, trust, pixel, lifetime, allocation and deadline assertions and audited packaging. Rendering/hardware acceptance remains separate. |
| `osx-x64`, `osx-arm64` | Complete producers/packages, fresh public consumers, full self-contained headless suites and trimmed/NativeAOT contract apps passed for `0.1.0-alpha`. | Retain target execution and audited packaging; rendering and hardware acceptance remain separate. |
| `android-arm`, `android-arm64`, `android-x86`, `android-x64` | All four target producers, closed ELF/16 KB audits, combined `0.1.0-alpha` package and native SDLActivity app contracts passed at `975c9693` in run `37412313488`. Tests cover actual process architecture, installed launcher/lifecycle metadata, font/ICU precision, TLS, ENet, captured PCM and repeated engine lifecycle. | Retain native emulator execution and metadata regressions; ARM uses native-bridge translation. Android TV/physical audio and production lifecycle remain separate. |
| `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64`, `tvos-arm64`, `tvossimulator-arm64`, `tvossimulator-x64` | All six Xcode producers, distinct CPU/device/simulator archive audits and both `0.1.0-alpha` platform packages passed. At `975c9693`, all four native simulator apps passed; both device profiles passed unsigned builds. iOS x64 required a separate CI job rerun after a first-boot setup timeout. | Retain native simulator execution and investigate flaky first-boot setup. Physical device execution remains unavailable without owner devices/signing. |
| `browser-wasm` | SDK-matched Emscripten production/auditing covers nineteen archives. Both source and fresh `0.1.0-alpha` package-only apps passed actual Chromium native font/ICU/image/FAudio and two engine lifecycles, including browser runtime-asset selection. | Retain the native browser contract; canvas presentation, WebGPU, input/storage and production host integration remain separate. |

The pinned MonoGame.Library.FreeType package has no `win-x86` asset. Windows now resolves its own target-built WOFF2-capable FreeType on all three architectures; ordinary package selection alone still does not establish execution.

Run [37397318977](https://github.com/edwardgushchin/Electron2D/actions/runs/37397318977) passed the native iOS x64 simulator app with the original boot/install/app deadlines. Android x64 passed APK/native payload audits but timed out after entering managed contracts, before native font/audio checks; the other three Android native apps passed. ContractChecks now reports each managed stage through the Android test log as well as the other contract hosts' console output. These diagnostics preserve assertions and the 120-second app deadline; the timeout is not accepted as successful execution.

The next [run 37399565766](https://github.com/edwardgushchin/Electron2D/actions/runs/37399565766) showed Android x64 completing all managed contracts and then timing out in native font precision, before TLS/audio lifecycle checks. Native font stages now have cold progress markers outside the measured intervals. On an app timeout the driver captures only the live test process's thread backtraces through [Android debuggerd](https://source.android.com/docs/core/tests/debug#get-a-stack-trace-or-tombstone-from-a-running-process), using bounded commands and a validated single PID, before owned-app cleanup. Full logcat/backtrace files are retained as the failure-only `android-timeout-<RID>` artifact. These diagnostics do not retry the app, change the 120-second deadline, omit foreign-thread tests or relax exact pixels/metrics and zero-allocation assertions; diagnostic output alone is not execution acceptance.

Android x64 passed the native app in run `37402225776` with the unchanged deadline and font/TLS/ENet/audio assertions; this observed pass does not identify a cause for its earlier timeout. The same run's actual Windows runtime DLL embedded the two scroll-hint SVGs with SHA-256 `6d8a18d586bb0223fe5957c38ec47b03006521a1b453d37d775aedc0939d9a59` and `37699e7425d6c60a6f9e1ce8bbdd67f9700dea80a0ac57d8d0ab2065fa603c16`. A local `core.autocrlf=true` checkout reproduced those exact noncanonical hashes; the pinned LF source hashes remain `fbe34532ea6643e2fe839833c3948f428e57e21ed56ee24fc528e68da68a5928` and `f88f0908ac86fb611b8fcaff552a1ec0ecf8cb118423d0f76b304658c23ea158`. The scoped `.gitattributes` rule now preserves LF for the complete runtime SVG icon family. A CRLF-checkout regression compares every icon's bytes; checksum expectations and source assets are not changed or normalized by the runtime.

In [run 37404576411](https://github.com/edwardgushchin/Electron2D/actions/runs/37404576411), Android x64 stopped after the headless-physics entry marker, while ARM64 produced no managed marker. Both had no single live test process when the unchanged 120-second app deadline expired. The filtered logs contain no cause for those exits. Timeout artifacts now also retain all system logcat buffers through a bounded 15-second command, including command failure/partial timeout output, without clearing device logs, retrying the app or changing execution assertions. A pass from a different run and added diagnostics do not resolve these failures.

Run `37407321875` passed ARMv7/ARM64/x86 native apps and retained Android x64 artifact `11388121386`. Its system log records `wm_relaunch_resume_activity` with configuration mask `80000000`, added system resource overlays, SDL Activity destruction/recreation and a zero-code VM exit while managed checks were still running. This establishes an `assetsPaths` Activity-recreation failure, not a font hang or native crash. Both test-only SDL hosts now declare `assetsPaths` in their native Android manifests and query their installed ActivityInfo before running. The C# attribute converter dropped `ConfigChanges.AssetsPaths`: a locally built APK still had mask `0x4f0`, so that candidate was rejected. Native manifest configuration preserves mask `0x800004f0` in the actual binary APK. The runtime check reads the existing sign bit on older Android too, without suppressing platform analysis or changing the minimum SDK. The [SDL Activity lifecycle](https://github.com/libsdl-org/SDL/blob/release-3.4.18/android-project/app/src/main/java/org/libsdl/app/SDLActivity.java) tears down native state on destruction and rejects recreation by default. No app retry, global configuration suppression or deadline change is used; current native execution remains required.

The same run's iOS x64 simulator timed out in `simctl bootstatus` after 300 seconds, reporting first-boot LaunchServices data migration before application install/launch. The other three simulator jobs passed. This setup failure is not an app pass, and no longer boot/app deadline or app retry is used to accept it; current simulator execution remains required.

Run `37409993215` exposed a separate Android manifest-merge failure before app execution: the explicit Activity entry retained the lifecycle mask but shadowed the C# attribute's export and launcher metadata. Both native manifests now own the complete exported MAIN/LAUNCHER entry, while the C# attribute supplies only its matching component name. A source-manifest regression covers both hosts, independent binary APK inspection verifies export/launcher and the exact lifecycle mask, and the installed ActivityInfo/launch-intent guards retain those checks at execution. No launch-permission bypass or app retry is used; current native emulator execution remains required.

Run [37412313488](https://github.com/edwardgushchin/Electron2D/actions/runs/37412313488), revision `975c9693cb66cb11a1359c684da56483354c641d`, passed all eighteen native producers, seven platform packages, six fresh desktop consumers, eighteen Build rows and eighteen Tests rows. Both aggregate checks passed. All seven desktop RIDs passed their full self-contained headless suites and trimmed contracts; the six supported desktop NativeAOT profiles passed. All four Android native apps and the source/fresh-package Chromium apps passed. All four Apple simulator apps passed, with unsigned build-only physical-device profiles. The first iOS x64 job timed out in system data migration before installation. Only that job and its dependent aggregate were rerun on a fresh runner; the other successful results and artifacts were reused. The rerun booted in about 180 seconds and reported simulator contract completion with the original 300-second boot, 180-second command and 120-second app limits. This pass does not fix or erase flaky simulator setup, establish physical-device/rendering/production-host acceptance, or prove public NuGet availability.

The subsequent exact-main [CI run 37417920131](https://github.com/edwardgushchin/Electron2D/actions/runs/37417920131), revision `17be5f4c3671420715cf70e7bab1e965707673e9`, passed all 71 jobs on its first attempt, including the same full target matrices and all four Apple simulator contracts without rerunning iOS x64. [Publication run 37420350558](https://github.com/edwardgushchin/Electron2D/actions/runs/37420350558) then rebuilt and published the eight `0.1.0-alpha` packages. All eighteen RID manifests and 155 native file hashes match the publicly downloaded packages. Fresh public restore and Linux x64 package-only text/audio/ENet execution, including a self-contained publish, passed with native compilers blocked and no `LD_LIBRARY_PATH`. [Native delivery](native-packaging.md#public-publication) records the independent publication checks and their limits.

Each continuation must deliver the pinned native dependency closure actually used by that target, including SDL, FreeType, HarfBuzz, the private ICU source/data ABI and FAudio with its engine-owned output bridge where those backends apply. Use `runtimes/<RID>/native` for native package assets and preserve the engine's one-managed-assembly boundary. Target loader or linker setup belongs to the engine/host integration; game consumers must not add backend references or manual native initialization.

Completion requires reproducible target builds or provenance-checked prebuilt artifacts; architecture, native imports/exports, private ICU identity/data and shared SDL identity checks; a fresh transitive application publish/bundle with matching notices and no foreign native payload; and public text/audio execution with lifecycle/error cleanup on the target host. Test actual multilingual boundaries and font output plus audio PCM/output behavior. A host build, a copied Linux `.so`, an evaluated RID, or successful package resolution alone cannot close this work. Remove the current platform guards only as part of a working, verified backend integration. Record build, packaging and execution evidence separately in this matrix; keep unimplemented targets explicit until those checks run.

## Reproduce the checked paths

- Linux display, GPU, fallback, and shader pixels: `SDL_VIDEODRIVER=wayland ELECTRON2D_TEST_RENDER=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` and the same command with `SDL_VIDEODRIVER=x11`. Both runs ended with `Rendering runtime checks passed`; the X11 run used XWayland, not a standalone Xorg session.
- Linux scene physics with native canvas interpolation: `SDL_VIDEODRIVER=wayland ELECTRON2D_TEST_INTERPOLATION_NATIVE=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release` and the same command with `SDL_VIDEODRIVER=x11`. Both ended with `Native physics-interpolation pixels passed: compatibility`. The ordinary managed test suite includes broader Box2D collision and lifecycle checks.
- Android device runs: build and use [Electron2D.AndroidProbe](../tests/Electron2D.AndroidProbe/README.md). Its five engine scenarios separate forced fallback, automatic fallback, GPU, shader, and physics; `gles_shader` checks the TV GPU separately. The engine project excludes nested generated Android Java/JAR/native files so switching ABI builds does not bind a prior test application's output.
- Web: the browser result came from an isolated `/tmp` source snapshot and WebAssembly host, with SDL 3.4.16 built through Emscripten 3.1.56 and a temporary browser-compatible projection of the SDL3-CS P/Invoke bindings. It is diagnostic evidence only. No browser host, transitive native package, nonblocking frame loop, input/storage lifecycle, or shader GPU backend has been merged into the runtime. A two-frame `Engine.Run` returned successfully but disposed its canvas before sustained browser presentation.

The Linux Wayland gate in [ADR 0021](decisions/product.md#adr-0021) remains the current release gate. The Android results cover these tested devices and paths; they do not establish lifecycle, controller input, audio, storage, signing, or store packaging. The Web engine result proves only the isolated fallback and managed physics probe; the separate [Chrome WebGPU shader probe](../tests/Electron2D.WebGpuProbe/README.md) rendered a red frame outside Electron2D. Chrome hardware acceleration is available. [ADR 0028](decisions/rendering.md#adr-0028) selects a separate WebGPU canvas backend with SPIR-V-to-WGSL translation, but it is not implemented yet. [SDL states that its GPU API currently has no Web backend](https://wiki.libsdl.org/SDL3/FAQDevelopment). Apple and desktop runtime claims require their own hosts and device checks.

## Automated RID checks

The `osx-x64` full Tests job uses the `macos-26` Apple Silicon host because the
Intel CI host in run `39858176735` rejected SDL GPU's Metal requirements. The
application and native assets remain x64; CI checks the application Mach-O
architecture and prepares Rosetta before execution. Build/native production
retain the Intel runner. [.NET 10 supports x64 execution through Rosetta](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)
and [NativeAOT cross-architecture compilation with Xcode](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/cross-compile#mac).
This configuration retains the full suite, trimmed and NativeAOT checks; its
execution still requires the corrected CI run. Rosetta execution does not
establish acceptance on a physical Intel GPU.

Linux full-suite jobs select SDL's `offscreen` video driver and install the Vulkan
loader plus Mesa ICDs. GPU physics initializes the video subsystem even when its
device has no window; an empty runner desktop must therefore have an explicit
video backend. A local no-DISPLAY/no-Wayland pipeline check on NVIDIA Vulkan
executes the production pair-table hash for 4,096 keys and both 26-pipeline
owner/solver-thread profiles. The pipeline driver can report an unavailable
headless-surface extension without preventing windowless compute. This targeted
result does not establish Mesa execution on both CI architectures, a complete
offscreen suite or rendered-window acceptance; those gates remain separate.

The single [CI workflow](../.github/workflows/ci.yml) owns the complete 18-RID [matrix](../tools/ci/rids.json), one shared [native producer](../.github/workflows/native.yml), and separate Build/Tests matrices using the same [target workflow](../.github/workflows/rid.yml). Both matrices restore the same audited native artifacts from that run; native compilation, package auditing and fresh consumer checks are not duplicated. The matrix generator rejects missing, duplicate, extra or differently mapped RIDs relative to `Electron2D.csproj`. CI runs on main pushes, pull requests and manual dispatch, with fail-fast disabled and superseded runs cancelled per ref. Its final `Build` and `Tests` checks require every respective dependency to succeed; failures, skipped and cancelled work cannot count as passing. The two README badges select those checks by name on `main`; a successful badge covers the checks below, not full platform acceptance.

| RIDs | Build | Tests |
| --- | --- | --- |
| `linux-x64`, `linux-arm64` | Runtime library and private native backends with trimming/AOT analysis | Library/native ELF checks, full self-contained headless suite, trimmed and NativeAOT contract apps |
| `win-x86`, `win-x64`, `win-arm64` | Target library and private native package with trimming/AOT analysis | PE/dependency/layout checks, full self-contained headless suite and trimmed contract app; NativeAOT contract app except Windows x86. All available profiles passed at `975c9693` |
| `osx-x64`, `osx-arm64` | Target library and audited private native package with trimming/AOT analysis | Full self-contained headless suite, trimmed and NativeAOT contract apps passed for `0.1.0-alpha` at `975c9693` |
| Four Android RIDs | Android target library with trimming/AOT analysis | APK build and execution in Google APIs emulators: x86/ARM on API30 x86, x64/ARM64 on API35 x64. ARM runs use native-bridge translation; the app checks actual .NET process architecture |
| Four iOS/tvOS simulator RIDs | Target library/private static references with trimming/AOT analysis | Ad-hoc-signed native font precision/TLS/ENet/audio lifecycle apps and temporary simulator token-checked completion passed at `975c9693`; iOS x64 passed a separate job rerun after a setup timeout |
| `ios-arm64`, `tvos-arm64` | Target library with trimming/AOT analysis | Unsigned test application build only. The owner has no physical devices; signing/device execution is not checked |
| `browser-wasm` | WASM target library with trimming/AOT analysis and audited static archives | Reflection-disabled ProjectReference and fresh NuGet app publish/execution in Chromium: native fonts/ICU, six image codecs, FAudio PCM/output and two lifecycles |

Linux jobs use Ubuntu 26.04, whose system OpenSSL supports the datagram BIO needed by DTLS; an explicit symbol preflight prevents falling back to an older backend. The earlier Ubuntu 24.04 run [37241595991](https://github.com/edwardgushchin/Electron2D/actions/runs/37241595991) failed because system OpenSSL lacked `BIO_s_dgram_pair`. Linux runtime jobs now consume the private native NuGet artifact and the pinned SDL package; source compilation and zlib/Zstandard development prerequisites belong to the separate native producer. These checks do not exercise Wayland/X11, rendering or hardware audio.

Trimming and AOT analyzers are enabled again, including Apple library builds. The earlier Apple run [37241418897](https://github.com/edwardgushchin/Electron2D/actions/runs/37241418897) reported IL2026 in ThemeDB and JSON/ConfigFile. ThemeDB now uses a generated compiled native-type catalog; typed JSON/configuration/settings use generated built-in schemas or explicit `JsonTypeInfo<T>` for application models when JSON reflection is disabled. The native resolver uses `AppContext.BaseDirectory`, including single-file NativeAOT apps. Successful analysis alone is not execution evidence.

All contract apps share [public API checks](../tests/Portability/ContractChecks.cs) for JSON model isolation/schema rejection, ConfigFile parse/reload, ProjectSettings base/feature overrides and validation rollback, native theme-name retention and headless scene physics. Android additionally executes [native text/TLS/audio/ENet lifecycle checks](../tests/Portability/NativeChecks.cs) and the existing font precision/raster regression on SDLActivity. Its driver audits all seven private libraries plus one SDL core for the selected ABI, rejecting duplicates/foreign ELF payloads. The headless audio profile uses SDL dummy output; these checks do not establish physical audio, rendering, input/storage or production application-host acceptance. Android result tokens reject stale/missing/failed completion; Apple drivers create and clean up only their own simulator; browser checks require a real WASM PASS result. APKs, Apple app bundles and desktop/browser outputs are retained as test artifacts.

Android notices are packaged transitively as `assets/licence/*`; Apple notices use resource publish metadata under `licence/`. The mobile drivers compare their bytes with the engine notice inventory and reject a source-only reference-data notice before execution. This bundle-content gate is separate from native backend and physical-device acceptance.

Local Android native checks passed on x86 (API30), x64 (API35) and ARM64 (API35 x64 native bridge). The x86 host reproduced broken floating-point Volatile reads/writes before native audio initialization; integer atomics now retain public timing/rate/fade values, synchronized gains and finite positive output latency. These runs use the downloaded preview.4 producer package plus the candidate transitive targets. The local ARM32 API30 bridge aborts in Android system ICU (`uenum_count_66`), but run 37326323360 passed the ARM32 native app on its CI image; the local failure does not prove a physical ARM device defect. Mobile failure reports retain bounded multiline exception details.

Run [37326323360](https://github.com/edwardgushchin/Electron2D/actions/runs/37326323360), revision d459232a, passed all eighteen native producers, seven packages and six fresh desktop native consumers. Android ARM32/x64/ARM64 native checks passed; x86 stopped at the floating-point fault corrected above. Every Windows full suite reached AudioResourceTests and stopped at its hard-coded Unix temporary path. Both macOS full suites passed UDP/DTLS and then stopped in secure ENet when a reused SocketAddress had been shortened by a prior receive. Portable temporary paths and shared/connected UDP receive preparation are corrected for the next run. No completed Windows/macOS full-suite acceptance is claimed yet.

Alpha run [37377030554](https://github.com/edwardgushchin/Electron2D/actions/runs/37377030554), revision `c43c332a`, passed all eighteen native producers and all seven platform packages, including nineteen audited WebAssembly archives. Linux x64 and all three Windows fresh consumers passed. Both macOS consumers passed their explicit-RID ProjectReference/NuGet checks, then failed in the default desktop build because the host-only asset filter removed the universal HarfBuzz `osx` asset. That layout is corrected with a four-host MSBuild regression. The failed native consumer gate prevented both Build and Tests matrices from executing, so this run proves neither current full-suite acceptance nor mobile/browser app execution.

Run [37382005552](https://github.com/edwardgushchin/Electron2D/actions/runs/37382005552), revision `9babfa3a`, completed with 59 successful and twelve failed jobs. All eighteen native producers, seven platform packages, six fresh desktop consumers, eighteen Build rows and Build aggregate passed. All four Apple native simulator apps, both unsigned device builds and the source/fresh-package browser native apps passed. Six desktop full suites failed when self-contained publication omitted the private Bouncy Castle test dependency; its PackageReference now explicitly sets `Publish="true"` while retaining `PrivateAssets="all"`. All four Android apps failed before execution because the native layout target moved the managed SDL host assembly into the native directory; the target now selects only native assets, with a failing-then-passing MSBuild regression. Local Android x64 APK build and native execution passed with the downloaded producer payload and candidate targets in an isolated package cache; fresh candidate production remains a separate CI check.

Windows ARM64 failed earlier in native DTLS handshake progress. Its CDB log and matching DLL show a small MSVC 19.51.36260 `/O2` frame calling `__chkstk` before saving LR, then saving the probe's return address at RVA `0x1145cc` instead of its caller's address. The custom ARM64 OpenSSL target no longer forces `/Gs0`; the standard stack-probe threshold remains enabled ([MSVC guidance](https://learn.microsoft.com/en-us/cpp/build/reference/gs-control-stack-checking-calls?view=msvc-170)). A Perl-evaluated compiler-option regression is connected to every Windows producer. Actual corrected ARM64 DTLS and all current full suites still require target execution. Full desktop CI also requires the executable's final `Electron2D checks passed.` marker, in addition to a successful exit, and retains its log as a RID artifact. Local framework-dependent and self-contained full Linux x64 suites passed after explicitly publishing the private test dependency. Remote main remains unmerged while Tests is red.

Revision `5e9e7c92` passed both [Build 37260737239](https://github.com/edwardgushchin/Electron2D/actions/runs/37260737239) and [Tests 37260737192](https://github.com/edwardgushchin/Electron2D/actions/runs/37260737192) on `codex/rid-runtime-checks`: each workflow completed all22 jobs, including every18-RID row and the private Linux native producer/package jobs. Both Linux full suites, all applicable desktop trimmed/NativeAOT apps, all four Android APKs, all four Apple simulator apps and browser WASM execution passed. The two physical Apple device profiles passed unsigned app builds and notice audits only. Windows/macOS still run the portable profile; foreign native backend integration and full engine acceptance remain open. README badges track `main`, which was not pushed as part of this verification.

The Apple host pins .NET SDK10.0.302/workload10.0.302.0, the [official Xcode26.6 release pair](https://github.com/dotnet/macios/releases/tag/dotnet-10.0.1xx-xcode26.6-10301). The installer takes its version from `global.json` only. The fully trimmed test app uses nullable UIKit launch options and transitive static native references/CoreBluetooth. Target-compiled engine imports select executable symbols; the old Electron2D-only dynamic P/Invoke override is removed. Both UIKit profiles run shared NativeFontPrecisionTests and NativeChecks after SDL main readiness. Local MSBuild archive/metadata/codec-identity tests and preprocessing-branch compilation passed, but first actual Xcode/native simulator execution remains required. Simulator builds use ad-hoc signing; physical-device bundles remain unsigned.

The Apple app explicitly sets `DynamicCodeSupport=false` and asserts that runtime feature before executing contracts. Otherwise the ARM64 AOT profile selected a JIT-generated BCL reflection invoker while opening a file and aborted during launch. Both ARM64 simulator apps passed with the AOT-safe reflection fallback; ordinary file I/O and trimming/AOT analyzers remain enabled. An explicit static registrar experiment did not fix that failure and was removed. See the [runtime feature switch](https://github.com/dotnet/runtime/blob/v10.0.4/docs/workflow/trimming/feature-switches.md) and [SDK default-ordering issue](https://github.com/dotnet/runtime/issues/108269).

The Apple driver selects from each runtime's `supportedDeviceTypes`, creates only its own temporary simulator and checks a per-run result file in the app's data container. Tokens reject missing/stale/FAIL completion; startup traces, launch stdout/stderr and bounded crash/log diagnostics identify early failures. The UIKit delegate queues contracts after launch returns. Console output alone is not accepted as completion, and every exit path attempts cleanup of the owned simulator. A shutdown timeout is reported and still attempts deletion; failed deletion remains a job failure. Run 37326323360 passed the iOS ARM64 simulator app contracts but failed during shutdown before deletion. Driver regression checks cover shutdown timeout after both PASS and FAIL, and reject a deletion failure; corrected target cleanup still requires CI execution.

The iOS x64 simulator in run 37392360522 exceeded its existing 300-second bootstatus limit before application installation; the preceding run exceeded the 180-second install limit. Other current Apple simulator apps passed. The driver now prints the selected runtime/device and command phase, preserves bounded partial stdout/stderr from setup timeouts, and still deletes its own simulator. Regression checks preserve both original limits and require one attempt, failure propagation and cleanup. This adds missing diagnostic evidence, not a simulator-startup fix or accepted iOS x64 execution.

Browser completion posts a bounded JSON result with a unique run token to the loopback test server; the driver waits up to120 real seconds and rejects stale/malformed/failure results. It does not treat an unfinished DOM snapshot as execution evidence. Chromium starts in an isolated process group that is stopped before profile cleanup, including child processes that outlive the launcher.

The connected browser native app links nineteen archives through the Web platform package and checks byte-identical notices under `wwwroot/licence` before launch. It retains the threaded fixtures' semantic/decoder/lifetime checks but omits foreign-thread and Parallel.For cases on its explicit nonthreaded profile. Font precision and independent ICU dictionary/locale/scalar/cache checks retain their strict 64-call zero-managed-allocation assertions. `MONO_INTERPRETER_OPTIONS=-tiering` fixes optimized interpreter code during this test: the default profile showed transient tier-up allocations within measured intervals, unchanged with Jiterpreter disabled. Temporary per-call/stage probes and a custom binary-search experiment were removed; no threshold or warmup count was relaxed. This is a test profile, not a production default or a general native-allocation guarantee. Image decoding and captured nonzero PCM do not prove canvas presentation, physical audio or raw-socket browser networking.

Run [37355448166](https://github.com/edwardgushchin/Electron2D/actions/runs/37355448166), revision `4e129553`, completed with 67 successful and four failed jobs. All eighteen producers, seven platform packages, six fresh desktop consumers, eighteen Build rows and Build aggregate passed. Both macOS full suites/trimmed/NativeAOT apps, all four Android native apps and all four Apple native simulator apps passed; Apple device profiles remain unsigned build-only. The three Windows full suites and Tests aggregate failed: x86/x64 at independent DTLS CLI handshake readiness, ARM64 during native cookie/handshake progress. These results use the preceding package family; the current `Electron2D.{Platform}` alpha candidate and connected browser-native app require their own complete CI run. Remote main has not been merged on the strength of these partial results.

The full headless Linux test process uses `DOTNET_TieredCompilation=0` to keep optimized JIT code fixed during warmed allocation measurements after an intermittent strict delay-DSP allocation assertion. Both full Linux jobs passed with this profile; the zero-byte assertions are unchanged. This is a test process setting, not a production runtime default or a general performance guarantee.

Local library/profile checks on 2026-10-05 covered eleven RIDs in the first matrix change. The revised implementation passed Release trimming/AOT analysis, the full Linux x64 executable suite, Linux x64 trimmed and NativeAOT contract apps with `LD_LIBRARY_PATH` unset, and browser WebAssembly contract checks in Chrome. All four Android contract apps passed: x86/ARM on an API30 Google APIs x86 emulator and x64/ARM64 on API35 Google APIs x64. ARM used native-bridge translation; each app verified its actual managed process architecture. API30 initially timed out in `am start -W` although the app had logged PASS: a headless Activity finishes before a displayed-window wait completes. Removing that window wait preserved the strict per-run completion deadline and both 32-bit runs passed. Apple, native Windows/macOS and Linux ARM64 execution are not established by these local checks.

## Native TLS transport profile

The next desktop test revision replaces DTLS oracle console-banner readiness with actual loopback UDP binding and exchanges exact six-byte payloads without newline translation. TLS/DTLS process cleanup and nonzero-exit stderr handling share one helper, covered by an invalid-option diagnostic regression. Cold native DTLS and ENet per-address/codec/phase markers narrow remaining crashes/timeouts; assertions and ten-second progress deadlines are unchanged. Focused Linux DTLS/TLS/ENet checks passed. Target execution remains required before closing the four failures below.

Run [37343850888](https://github.com/edwardgushchin/Electron2D/actions/runs/37343850888), revision 47ff8810, completed with 66 successful and five failed jobs, including the failed Tests aggregate. All eighteen Build rows and the Build aggregate passed. All four Android native app steps passed. macOS ARM64 passed the complete headless suite, trimmed and NativeAOT apps. Windows x86/x64 passed their native DTLS peer/allocation checks but timed out in the independent DTLS oracle; Windows ARM64 passed corrected audio and TCP/UDP checks then crashed before DTLS completion; macOS Intel passed DTLS then timed out in ENet. These four full-suite failures remain open. Its four Apple simulator results cover the preceding managed-only app, not the newly connected static native host.

Run [37335292748](https://github.com/edwardgushchin/Electron2D/actions/runs/37335292748), revision bf417e76, passed actual Android native app execution on all four RIDs. Windows x64/ARM64 then exposed stack overflow while rejecting a 257-node randomizer factory chain; Windows x86 timed out waiting for abortive TCP reset. Both macOS suites reached the independent SslStream TLS 1.3 client, which their BCL backend cannot support. The candidate fixes retain the original guard/reset fixtures, use iterative randomizer selection and a shared non-consuming FIN/RST probe, and force TLS 1.3 with independent OpenSSL in both roles. The revised full Linux suite passed; Windows/macOS confirmation is still required.

The next run [37337895686](https://github.com/edwardgushchin/Electron2D/actions/runs/37337895686), revision a08c0602, preceded those desktop fixes. Its Android x64 app timed out after starting managed contracts; Android ARM64 failed before app launch while installing a corrupt emulator image archive. Neither is accepted as a pass or a demonstrated native audio failure. Apple/Web static application integration remains open independently of successful native archive production.

StreamPeerTLS currently uses system OpenSSL 3 (`libssl.so.3`, `libcrypto.so.3`) on Linux, with TLS 1.2 minimum, optional TLS 1.3 negotiation and system/custom CA/name validation. No OpenSSL binary is redistributed. TLSTests verifies native Linux x64 streams and both-role independent SslStream interoperability; this transport does not use the display protocol. Other desktop/mobile/TV native packaging/backend integration and a browser-capable TLS host remain required future work; unavailable hosts fail explicitly. Resource key/certificate PEM/DER parsing uses BCL capabilities separately. OpenSSL native allocator totals, routed throughput and owner acceptance are not verified. See [TLS contract](components/tls.md).

Local TLS deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/e2d-tls-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_TLS=1 /tmp/e2d-tls-native-publish/Electron2D.Tests`. Both passed on the current host using OpenSSL 3.6.4; native TLS 1.2 and 1.3 were forced through independent SslStream peers.

## Native HTTP and compression profile

HTTPClient, HTTPRequest and StreamPeerGZIP execute on current Linux x64 through native TCP, the existing OpenSSL TLS backend and the .NET 10 compression PAL. HTTPTests exercises DNS address fallback, fragmented HTTP framing, forward and CONNECT proxies, independent SslStream HTTPS, scene/dedicated-worker completion, redirects, bounded incremental gzip/zlib, atomic file downloads, cancellation/timeouts and PackedScene configuration. Prepared 64-cycle HTTP body-span/poll and gzip member/decode/read/reset intervals each report zero managed bytes; native codec allocation totals and request/header/aggregate/file preparation are outside those intervals. These network checks are independent of display protocol and do not claim rendered/editor/agent-tool acceptance.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/e2d-http-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_HTTP=1 /tmp/e2d-http-native-publish/Electron2D.Tests`. Both passed, including private compression PAL resolution without a configured library search path. Other native targets require their runtime compression ABI and transport/TLS packaging checks; browser HTTP host integration, routed throughput, real authenticated proxies and owner acceptance remain separate. See [HTTP contract](components/http.md).

## Native WebSocket profile

WebSocketPeer runs native Linux x64 WS/WSS in client and server roles, using the existing TCP/HTTP/TLS backends and a prepared complete-message/frame queue. WebSocketTests interoperates independently with .NET WebSocket and ClientWebSocket, including TLS, subprotocols, native endpoints, fragmented messages, control/close and public Node/SceneTree polling. Wire fixtures cover malformed upgrades/frames, UTF-8/length/mask validation, pressure, heartbeat and borrowed/owned lifetime. Each 64-cycle warmed WS/WSS client mask/text/poll/span interval and server active/idle interval reports zero managed bytes; external OS/RNG/TLS native allocation remains unmeasured.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/e2d-ws-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_WEBSOCKET=1 /tmp/e2d-ws-native-publish/Electron2D.Tests`. Both passed. Network checks are display independent and do not establish rendering/editor/agent acceptance. Foreign targets/browser transport integration, routed conditions/throughput and human acceptance require separate gates. See [WebSocket contract](components/websocket.md).

## Native multiplayer transport profile

MultiplayerPeer/OfflineMultiplayerPeer and WebSocketMultiplayerPeer execute typed managed extension/local-authority and native Linux x64 WS/WSS cohort behavior. MultiplayerTests runs a server and two clients through public Node/SceneTree polling, verifies identity/next-packet metadata, broadcast/positive/exclusion/client routing, admission/limits/backpressure/deadlines, graceful/forced ownership and callback/error/reentry cleanup. Independent ClientWebSocket verifies the four-byte little-endian ID and raw application messages; an independent server supplies invalid/missing IDs. Each 64-cycle warmed custom packet/event and native WS/WSS active/idle interval reports zero managed bytes. Native OS/RNG/TLS allocator totals and connection/handshake preparation are separate.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/e2d-mp-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_MULTIPLAYER=1 /tmp/e2d-mp-native-publish/Electron2D.Tests`. Both passed. The compiled documentation consumer at `/tmp/e2d-mp-consumer` uses public API only; native tests establish the executed scene workflow. Network transport is display independent. MultiplayerAPI/SceneMultiplayer RPC/authority/path integration, scene synchronization/replication, ENet/WebRTC/browser backends, foreign packaging/execution, routed performance and human/rendered/editor/agent acceptance remain separate gates. See [multiplayer contract](components/multiplayer.md).

## Native cryptographic operations profile

Crypto/HashingContext/HMACContext/AESContext execute on the current Linux x64 BCL provider. CryptoTests verifies known/RFC/NIST digest/HMAC/AES vectors, 128/256-bit raw ECB/CBC chunks and in-place operation, lifecycle/capacity/owner/invalid-input edges, independent RSA/EC signature/encryption wires and generated resource save/reload. Generated RSA key/certificate resources establish a real native TLS server; an independent SslStream client verifies hostname/custom trust. Signing while TLS retains the same key leaves resource replacement/disposal guards enforced. Each 64-cycle warmed prepared digest/HMAC/random/comparison/AES span interval reports zero managed bytes. Provider-native allocation, key generation/import, asymmetric operations and snapshots remain outside that measurement.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/e2d-crypto-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_CRYPTO=1 /tmp/e2d-crypto-native-publish/Electron2D.Tests`. Both passed. The four class-page public examples compiled and ran in `/tmp/e2d-crypto-consumer`. The full managed executable runner passed; these crypto/network tests are display independent. Foreign platforms/provider policy, external native allocation, routed traffic and human/rendered/editor/agent acceptance require separate gates. See [cryptographic operations](components/crypto.md).

## Native typed scene multiplayer profile

MultiplayerAPI/SceneMultiplayer and Node/SceneTree execute on current Linux x64 WS/WSS: server plus two clients in independent scene branches, typed RPC codecs and local/remote/authority policies, original-sender server relay, custom bytes, bilateral authentication/monotonic deadline and callback/lifetime/invalid-packet recovery. SceneMultiplayerTests exercises managed provider/default factory hooks and public scene authoring/idle-frame polling. Each 64-cycle warmed WS/WSS typed RPC/process-frame interval and idle interval reports zero managed bytes. Setup/path discovery/membership/snapshots/error drain and application codec allocations are separate; external native allocation is unmeasured.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/e2d-scene-mp-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_SCENE_MULTIPLAYER=1 /tmp/e2d-scene-mp-native-publish/Electron2D.Tests`. Both passed. Five class-page public excerpts compiled/ran in `/tmp/e2d-scene-mp-consumer`; the full executable runner passed. Spawner/synchronizer/property-schema replication remains dependent. Other-host/routed/application-schema/human/rendered/editor/agent acceptance requires separate gates. See [typed scene multiplayer](components/scene-multiplayer.md).

## Native typed scene replication profile

MultiplayerSpawner, MultiplayerSynchronizer and SceneReplicationConfig execute on current Linux x64 WS/WSS through public Node/SceneTree branches. SceneReplicationTests verifies custom argument factories, automatic in-memory PackedScene templates, pre-Ready spawn properties, periodic and change-only delivery, whole-group 128-byte batching, visibility/RPC filtering, current-state late admission, authority/schema/old-sequence rejection, limits and remote cleanup/committed peer replacement despite throwing observers. Independent little-endian fixtures reject a malformed second property before either setter runs. Each 64-cycle warmed WS/WSS active sync/delta interval and idle interval reports zero managed bytes; setup, construction, membership, snapshots, errors, application callbacks and native backend allocation remain separate.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained -o /tmp/e2d-replication-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_SCENE_REPLICATION=1 /tmp/e2d-replication-native-publish/Electron2D.Tests`. Both passed. Six public class-page excerpts compiled and ran in `/tmp/e2d-replication-consumer`; the full executable runner passed. These checks establish programmatic authoring and headless native networking. File-backed scene templates require a concrete typed serializer/loader/saver; foreign packaging/execution, routed/application-schema, human, rendered, editor and agent acceptance require separate gates. See [typed scene replication](components/scene-replication.md).

## Native UPNP gateway profile

UPNP/UPNPDevice execute synchronous SSDP discovery, bounded per-device description/connection assessment and typed SOAP query/add/delete commands on current Linux x64. UPNPTests runs an independent native multicast responder and fragmented IPv4/IPv6 TCP gateway, validating exact mapping fields/escaping/faults, malformed/empty/non-IGD/invalid-control descriptions, DTD/wrong-action/body-budget failures, a stalled exchange's five-second deadline, owner/borrowed lifetime and public Node/SceneTree commands. Sixty-four metadata-query cycles report zero managed bytes; HTTP/XML/discovery/command cold work and native allocation remain separate.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained -o /tmp/e2d-upnp-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_UPNP=1 /tmp/e2d-upnp-native-publish/Electron2D.Tests`. Both passed, and the full executable runner passed. Two public class-page excerpts compiled and ran in `/tmp/e2d-upnp-consumer`. A separate public consumer performed read-only real IPv4 gateway discovery/status/external-address querying: Success, one device, a valid gateway and syntactically valid external address. No real gateway mappings were changed. Real gateway mapping acceptance/external connectivity, routed IPv6 multicast, foreign hosts, native allocation and human/rendered/editor/agent acceptance remain separate gates. See [UPNP gateway control](components/upnp.md).

## Native DTLS packet profile

PacketPeerDTLS/DTLSServer execute DTLS 1.2 on current Linux x64 system OpenSSL 3.6.4, using fixed datagram BIO pairs (OpenSSL 3.2+ capability), existing trust/identity resources and shared TLSStatus. DTLSTests verifies public UDPServer/Node/SceneTree packet workflows, address-bound cookie challenges, dropped-initial-ClientHello retransmission, exact 488-byte/empty/oversized/undersized edges, anti-replay/bad-MAC recovery, system/custom/unsafe/name policies, setup rollback, helper/peer/UDP lifetime, connection generation and owner/reentry guards. Independent OpenSSL s_server and s_client processes verify actual DTLS 1.2 application traffic in both roles; temporary generated test identities and processes are cleaned up. Each 64-cycle warmed active packet/span/poll interval and idle interval reports zero managed bytes; setup/import/cookies/snapshots/user callbacks and native allocator totals are separate.

Local deployment check: `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained -o /tmp/e2d-dtls-native-publish`, then from `/tmp` run `env -u LD_LIBRARY_PATH ELECTRON2D_TEST_DTLS=1 /tmp/e2d-dtls-native-publish/Electron2D.Tests`. Both passed, including the independent native processes; the full executable runner passed. Two public class-page excerpts compiled and ran in `/tmp/e2d-dtls-consumer` with the client's explicitly required UDP listener supplied around the excerpt. Other native/browser hosts, packaging and routed performance/native allocation, ENet integration and human/rendered/editor/agent acceptance remain separate. The tests establish headless native network behavior, independent of display protocol. See [native DTLS](components/dtls.md).

## Native ENet host/peer/multiplayer profile

The current Linux x64 private library supplies unchanged ENet1.3.18/FastLZ0.5.0 core, real IPv4/IPV6 endpoint-token socket routing, zlib/Zstandard codecs and the existing DTLS1.2 packet transport. ENetTests exercises all five compression modes, channel fragmentation, reliable retry after dropped UDP traffic, refusal, packet-slot saturation, tuning/statistics, raw datagrams, identity/routing/metadata on all three multiplayer modes, graceful/forced/reset teardown, callback errors/close/recreation, mesh ownership and SceneTree/SceneMultiplayer flows. SceneReplicationTests also executes its complete native spawn/sync/delta/visibility/RPC/lifetime scenario on ENet, including 64 active/idle zero-managed-allocation scene intervals. The separate Electron2DENetOracle process uses the stock Unix socket backend to verify ordinary ENet wire exchange and shutdown independent of the private socket port. Sixty-four warmed active and idle multiplayer intervals allocate zero managed bytes. The full executable runner passed; native allocation, preparation/snapshots and user callbacks remain distinct.

Historical deployment checks passed for the former host example, published self-contained for linux-x64 into `/tmp/e2d-enet-host-publish`, with both native/license audits; `dotnet publish tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release -r linux-x64 --self-contained true -o /tmp/e2d-enet-tests-publish`, then the deployed ENet selector from `/tmp` with `LD_LIBRARY_PATH` unset and the stock oracle supplied. The three public excerpts compiled and executed in `/tmp/e2d-enet-consumer`. No external application or owner acceptance is inferred.

The source manifest check verifies 20 unchanged ENet/FastLZ files and two original license copies. The publish profile requires 68 audited ELF files and 56 bundled notices, with libElectron2DENet.so only under runtimes/linux-x64/native, exactly thirteen engine exports, a unique SONAME and host libz/libzstd/libc/ELF-loader dependencies. Public class-page excerpts have a compiled executable consumer. Foreign/browser/routed/performance/human/editor/rendered/agent acceptance remain separate; no foreign execution is inferred from Linux tests. See [native ENet](components/enet.md).
