# Rendering changes and low-processor scheduling

Last updated: 2026-10-20

## Scope and selection

This component connects retained [RenderingServer](../classes/RenderingServer.md) change reporting to [OS](../classes/OS.md), typed [ProjectSettings](../classes/ProjectSettings.md) and the existing Engine.Run/RunAsync window loop. GPU and Compatibility retain their existing canvas, texture, viewport and presentation implementations.

The candidate comparison selected this executing renderer integration over automatic device sensors (owning mobile acquisition/capability/permission/lifetime adapter absent) and live Script association (typed initialization/owner/replacement contract unresolved in ADR 0091). The slice supplies three service operations and two startup settings, unlocking idle window scheduling. Missing invalidation code belongs to this slice rather than an external dependency.

## Public workflow

```csharp
ProjectSettings.Set(ProjectSettings.LowProcessorMode, true);
ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, 6900);
var window = new Window { Size = new(640, 360), Title = "Retained canvas" };
// Add ordinary scene children before starting the owned loop.
Engine.Run(window);
```

Each owned window startup samples effective project settings before readiness. During the session, OS.LowProcessorUsageMode and OS.LowProcessorUsageModeSleepUsec provide thread-safe process policy access. Project edits apply to the next window session. The sleep interval is nonnegative microseconds, initially 6900; negative values fail before assignment. Actual timer precision depends on the platform scheduler.

RenderingServer.HasChanged is an owner-thread observation requiring an active native session. Reading it does not acknowledge work or advance animation. A successful presented frame acknowledges its captured revision. Failed recording/submission and mutations after capture, including completion callbacks and off-owner resource notifications, remain pending. ForceDraw bypasses automatic skipping and RenderLoopEnabled; ForceDraw(false) retains a presentation obligation.

## Change producers

Node mutation validation and retained invalidation report conservative changes, including equal assignments, detached node changes and changes whose pixels are identical. Resource revisions report changes even when public signals are blocked or batched; disposing nodes/resources also invalidates. Ordinary Image snapshots are copied data rather than retained texture producers. A custom retained producer must publish its existing resource Changed contract. No polling of unreported custom fields is introduced.

Retained canvas, viewport, light, occluder, skeleton, mesh, texture, material, instance and global parameter operations share existing renderer mutation gates. Renderer-device command recording invalidates output, including image writes and pending compute/readback. Process-wide project-setting changes conservatively invalidate because active rendering policies can consume them. Native expose, restore, show, pixel size and display-scale events request output.

Static frames stop submitting while scene processing, physics, timers, input and settings delivery continue. Camera tracking skips an equal internal viewport-matrix publication while retaining its own tracking/parallax updates; a static camera therefore does not manufacture a change every process tick. The rendering clock still consumes scaled process steps across skipped iterations; disabling RenderLoopEnabled retains its existing frozen-clock behavior. FramePreDraw/FramePostDraw and frame completion callbacks belong to actual submissions. A queued completion request itself requires a frame. Movie recording renders each capture frame and bypasses the extra low-processor wait.

Time consumers remain executable: submitted fragment/vertex TIME uniforms, retained animation intervals, AnimatedTexture deadlines, active Always viewports, differing physics presentation endpoints and changed physics contact snapshots. Interpolation checks use the same scene-versus-server transform selection as replay, including a scene item that has only a retained color override. Settling endpoints invalidate their final pose. Paused/zero-speed/single-frame animation and a zero-duration loop do not independently request time updates.

## Waiting and failure

The owned loop waits for the greater of its remaining MaxFPS period and configured low-processor interval. It pumps native events in bounded wait portions and observes quit; a large configured interval does not block window close for the whole interval. Async hosts retain their owner context and yield through the existing timer. Manual Engine.AdvanceFrame neither draws implicitly nor applies this sleep policy.

There is no public dirty-reset switch. Completion acknowledgement occurs only after successful backend EndFrame, before visibility/readback/post-draw observers. Native session teardown drops its own revisions and callbacks. OS policy retains process lifetime but the next owned startup reapplies project values.

## Verification and limits

`ELECTRON2D_TEST_LOW_PROCESSOR=managed`, `gpu` and `compatibility` select [LowProcessorRenderingTests](../../tests/Electron2D.Tests/LowProcessorRenderingTests.cs). The [shared public contract](../../tests/Portability/LowProcessorContract.cs) also runs as `--low-processor` in the public portability consumer. Tests cover settings/defaults/descriptors/off-owner access, manual simulation without waiting, unavailable/foreign-owner renderer access, conservative and signal-blocked resource changes, retained item writes, actual pixels, failed recording recovery, completion mutation, nonpresented output, animation deadlines, texture content, idle and active owned loops, interpolation, shader time, expose, disabled optimization and responsive close with an Int32.MaxValue interval.

The native animation deadline probe uses one-shot playback so presentation or scheduler delays spanning a complete animation cycle cannot invalidate the expected final frame. It also accepts a frame already advanced by the initial draw, while still requiring pending work when that draw has not consumed the deadline. The local native profiles use software Vulkan and SDL software Compatibility. They measure 128 prepared idle iterations with an attached static camera, without new drawn frames or owner-thread managed allocation, while physics continues. A separate 64-iteration prepared active color-update/native-draw loop also allocates zero owner-thread bytes. A registered movie writer receives eight actual RGB frames and PCM blocks despite unchanged output and an Int32.MaxValue interval; this checks capture cadence and pixels, not encoded file interoperability. They do not establish hardware power savings, native allocation totals, foreign platform execution or human acceptance. Broader tests and final source-cohort evidence are recorded with the completed slice; a focused native pass alone is not a full-engine acceptance claim.

The final source cohort passed the ordinary executable suite in 1672.43 seconds with software Vulkan and SDL_VIDEO_WAYLAND_ALLOW_LIBDECOR=0 in that test process. The initial libdecor GTK reinitialization failure and the interrupted pre-camera cohort remain separate evidence, not passing runs. Strict public JIT and NativeAOT contracts passed; NativeAOT used linker/compiler warnings as errors and loaded no CoreCLR. Six PDB cohorts matched 3055 source/project inputs. These are local checks, not complete platform or agent-tooling acceptance.

## Decisions

[ADR 0016](../decisions/core-object-runtime.md#adr-0016) owns scheduling; [ADR 0019](../decisions/core-data-io.md#adr-0019) owns typed startup settings; [ADR 0028](../decisions/rendering.md#adr-0028) owns the existing canvas backends; [ADR 0095](../decisions/singleton-services.md#adr-0095) owns retained static services. This adds executing producers and no new service lifetime or rendering backend.

Worker [render-thread requests](render-thread-callbacks.md) conservatively increment the session change revision and drain before automatic idle-frame selection, including render attempts with drawing disabled. A callback error remains observable even after a nested manual draw consumes that revision.

## Visible texture policies

RenderingServer.TextureSetForceRedrawIfVisible(RID, bool) configures a weak session policy on a live ordinary/layered image or logical proxy, including borrowed resources. No storage ownership or pixel mutation changes. It is recomputed from submitted visible output, including aliases before native flattening, atlas/current animation sources, SVG raster origins, CanvasTexture channels, shader ordinary/layered/fixed-array bindings, light masks, device/drawable wrappers and sampled viewport dependencies. Distinct logical aliases remain distinct when batching geometry sharing one native image. Drawing-only MeshTexture identities reject; configure their sampled BaseTexture image.

Unused, hidden, culled, fully clipped or offscreen output does not independently request another frame. Disabled viewport dependencies stop consuming inner image policies; their visible completed viewport texture can still carry its own flag. Destination identity retains its flag across storage replacement, while a consumed source does not transfer its flag. Disabling a flag settles to idle after the pending mutation frame. RenderLoopEnabled and viewport update gates still apply; manual ForceDraw remains explicit. The policy expires at session retirement and does not keep an unused borrowed resource alive. A failed unsubmitted frame remains pending even when no user mutation accompanies its recording error.

Visibility uses viewport/client intersection, item visibility, submitted geometry, explicit bounds and scissor clipping. Authored/generated vertices and instances without explicit bounds are conservative, because GPU work can move beyond CPU base vertices. No shader branch or per-pixel occlusion analysis is performed. Other existing continuous producers can independently keep frames active.

This slice was selected over TextServerExtension (247 blocked declarations requiring typed provider dispatch, registration, lifetime and migration of native consumers) and shared device texture views (three blocked declarations requiring native format/subresource aliasing and retained view lifetime). The single visible-image policy uses the existing scheduler and texture families and adds an executing consumer across them. Pinned reference XML defines the visible-redraw behavior without deprecation; its RD setter is empty and GLES stores a flag without a demonstrated consumer. ADR 0028 explicitly executes that documented contract on both current backends.

`ELECTRON2D_TEST_TEXTURE_REDRAW=managed|gpu|compatibility` selects TextureRedrawTests. Native profiles cover borrowed/owned/stale/off-owner roles, nested proxy identity/source/retargeting, batched aliases, mesh/multimesh, SVG, channels, replacement, viewport dependency/update gates, GPU shader ordinary/layered/fixed-array and device/drawable bindings, actual red output, weak collection, cross-session reset, disabled rendering, manual drawing and callback-error retry. After warmup, 32 active full frames retain their recordings and allocate zero managed owner bytes; separate 32-iteration idle/hidden intervals submit no frames and allocate zero owner bytes. Software Vulkan/GL profiles execute under Linux Wayland and X11. Full-suite, native allocation totals, physical GPU, other-platform and human acceptance remain separate gates.
