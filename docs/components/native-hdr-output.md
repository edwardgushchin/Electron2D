# Native HDR output

Last updated: 2026-10-27

Window.HDROutputRequested selects native HDR independently of the authored
Viewport.UseHDR2D flag. An unassigned root reads
ProjectSettings.HDROutputRequested (`display/window/hdr/request_hdr_output`)
before opening its native window. An explicit false assignment takes precedence.
Scene copies and files retain the request without storing native identities.

## Request, support and active output

DisplayServer.WindowRequestHDROutput and WindowIsHDROutputRequested expose the
same request. WindowIsHDROutputSupported additionally requires an HDR-capable
GPU composition and current native HDR headroom. WindowIsHDROutputEnabled is
true only when that request has selected an active HDR swapchain. The existing
Feature.HdrOutput identity reports renderer composition availability, independently
of the current display's headroom. Queries require the display/scene owner and
neither create a renderer nor wait for GPU execution.

Unavailable native HDR retains the request and uses SDR until a native HDR-state
event enables it. Compatibility rejects a request before committing it; an initial
unsupported request closes the failed host. Hidden, unclaimed root windows retain
mutable requests without configuring a missing swapchain. The existing Wayland
Vulkan remap restriction remains an explicit capability boundary.

## Luminance and transfer

Reference/max luminance setters accept finite positive nits or negative automatic
selection, initially -1. Zero, nonfinite values and nonfinite resulting ratios
reject before commitment. Authored getters retain that policy. Current getters
read applied proportional nits while HDR is active, otherwise zero. The automatic
source uses SDL's SDR-white scale and HDR headroom, with an 80-nit SDR-white unit;
it does not claim a physical luminance measurement. Maximum linear output equals
current maximum/reference for HDR and one for SDR.

GPU presentation prefers extended linear sRGB when supported, otherwise BT.2020
ST2084. The final native shader applies reference/maximum luminance, the selected
color-primary conversion and transfer. VSync selection remains independent. SDR
uses its existing byte presentation and linear-to-sRGB transfer. Source HDR canvas
images remain linear half-float; movie capture continues to inspect their actual
image format before its SDR conversion.

A request forces effective half-float Window canvases while preserving authored
UseHDR2D and independent SubViewport policies. Removing it restores authoring or
the existing server override. Logical texture identities survive; native targets
change on the next submission. Readback still describes the last completed target.
The RenderingDevice screen stage is linear half-float for active HDR and encoded
bytes for SDR, independently of the final swapchain's packed/float format. Pending
screen contents retain their recorded color space across output changes and
convert on GPU, without a CPU image copy.

## Events and lifetime

Output values commit before Node notification 1013, Window.OutputMaxLinearValueChanged
and DisplayWindowEvents.OutputMaxLinearValueChanged. The scene notification stops
at window boundaries; embedded windows receive their containing output separately.
Native child windows retain independent requests, luminance and swapchains.
Unchanged maximum-linear values suppress events. Signal blocking leaves Node
notifications active. Observer errors retain committed values and independent
recipients. A reentrant change supersedes stale embedded/scoped delivery.

Native mode failure preserves prior policy and output. Retirement clears the
display's backend association and publisher; reopening a child creates a fresh
native identity while retaining its scene request. Metadata-observer retirement
stops traversal of the retired owner.

## Verification and limits

[WindowOutputTests](../../tests/Electron2D.Tests/WindowOutputTests.cs) shares its
source with [the focused executable](../../tests/WindowOutput.Tests/WindowOutput.Tests.csproj).
Select `ELECTRON2D_TEST_HDR_OUTPUT=managed|transfer|screens|cpu|gpu-separate|compatibility-gpu|native|project`.
The first profile also executes scene loading in a fresh process without an
available Vulkan ICD. GPU pixel checks compare seven independent extended-linear
and BT.2020 ST2084 cases, including negative inputs and reference/peak clipping.
Screen checks exercise pending images across both transfer directions. Window
profiles require the selected full physics backend without fallback, physical
motion/RID retention, independent children, events, failure/reentry and teardown.

Linux Wayland CLR and strict full-trim NativeAOT checks execute the native GPU and
compatibility paths. Prepared window profiles measure 32 warmup and 64 complete
process/render/presentation intervals at zero owner/all-thread managed bytes.
The native swapchain rejection injection is CLR-only; ordinary negative paths
also execute in NativeAOT. Positive HDR selection uses controlled native metadata
with real SDL composition requests, native rendering and shader pixels. The
unmodified native-metadata profile observes this host's actual SDR state.

Receipts are under `bin/physics-api-finish/2026-10-27/`. Controlled headroom and
native format acceptance do not establish an actual HDR display, physical nits,
foreign targets, native-heap allocation freedom, performance or owner acceptance.
The [SDL composition](https://wiki.libsdl.org/SDL3/SDL_GPUSwapchainComposition)
and [window metadata](https://wiki.libsdl.org/SDL3/SDL_GetWindowProperties)
contracts define the native inputs. [ADR 0044](../decisions/display.md#adr-0044)
owns the engine event/policy adaptation.
