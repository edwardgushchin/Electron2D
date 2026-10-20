# Owned window frame-delta smoothing

Last updated: 2026-10-21

## Scope and selection

This Core component connects the measured Engine.Run/RunAsync host clock to the existing process/physics synchronizer. It adds two executable settings, not a new scheduling lane. Compared with a global compiled-class catalog, it directly changes live simulation/render time. Shared RenderingDevice texture views still require a portable native format/subresource-view adapter and retained alias ownership. The existing monotonic clock, VSync observation and fixed-step synchronizer make this slice executable now.

## Public workflow

```csharp
ProjectSettings.Set(ProjectSettings.DeltaSmoothing, true);
var window = new Window { Size = new(640, 360) };
window.Ready += _ => OS.DeltaSmoothing = false; // Immediate policy for this session.
Engine.Run(window);
```

ProjectSettings.DeltaSmoothing is the advanced stored Boolean application/run/delta_smoothing definition, initially true, registered in every independent project registry. Ordinary persistence and feature overrides apply. Each owned startup samples the effective project value into OS.DeltaSmoothing before readiness. Later project edits apply to the next session; the thread-safe static OS property changes the next host sample. Its typed service descriptor is nonstored with true as its revert default. Neither setting forces VSync.

## Types, flow and invariants

[FrameDeltaSmoother](../classes/FrameDeltaSmoother.md) is internal scalar state retained by Engine. A successful new loop attachment resets it. Ordinary owned hosts sample measured elapsed time after native input and the host callback. Eligible input passes through this filter before the existing twelve-frame jitter distribution, catch-up cap, TimeScale, physics/process callbacks and canvas clock. FPS, Performance timing windows and MaxFPS deadlines retain original measured time.

Only effective classical VSync participates: unsupported native requests may fall back to Enabled, so the loop reads WindowGetVSyncMode rather than the request. Disabled, Adaptive and Mailbox effective modes bypass; disabled OS policy and intervals over one second bypass without mutating history. Returning to an eligible mode resumes history. Unconverged/bypassed input retains the original fractional host seconds, without microsecond truncation leaking into callbacks.

The estimator groups 60 accepted microsecond samples, starts at rates of at least 50 Hz and needs three matching rate estimates. A separate 64-frame measurement permits smoothing only within a strict 5 percent band of the estimate once confident. Intervals below 1000 microseconds contribute to measurement but not estimation. A confident estimate quantizes elapsed time to at least one whole integer-microsecond refresh period and carries signed remainder. The reference consensus/locking and rate-adjustment rules are retained; a large established cadence change can disable filtering until measured cadence agrees again. A pause over one second does not rewrite confidence. This is a jitter policy, not a hard real-time or native presentation guarantee.

Manual Start/AdvanceFrame/Stop and explicit headless batch stepping use supplied elapsed time. Fixed movie entry points retain exact 1/FPS input and never sample the filter, even with the OS property and classical VSync enabled. Stop retains completed metrics. No editor-context flag is manufactured: editor-hint production remains a separate missing domain.

## Verification and limitations

[HostDeltaSmoothingTests](../../tests/Electron2D.Tests/HostDeltaSmoothingTests.cs) is selected by ELECTRON2D_TEST_HOST_DELTA=1 together with ELECTRON2D_TEST_FRAME_MONITORS=managed, gpu or compatibility. It checks project defaults, independent registry save/load/reset and feature overrides, static/descriptor access, cross-thread policy, confidence, bypass/history/reset, explicit manual physics/process equivalence and 16384 prepared scalar samples with zero owner/all-thread managed bytes. It writes 32768 samples across sixteen steady, jittered, boundary, pause, rate-change and switching profiles for [verify_delta_smoothing.py](../../tools/coverage/verify_delta_smoothing.py). The tool compiles the actual nested C++ timer from pinned commit ed1daf0bf001b61586d9930840f2f1394092c079; every output must match. Reference files and harness remain ignored evidence, with source and trace SHA-256 in its report.

Local Linux Wayland/X11 GPU and Compatibility profiles run 399 actual processed/presented frames, observe actual estimator sampling, record whether cadence converges, switch the live OS policy and effective VSync, and read cyan pixels. Stable convergence and quantized outputs are mandatory in the deterministic kernel/oracle checks; a real host may correctly bypass smoothing when its cadence is unstable. Native confidence is reported per run rather than assumed from backend availability. A subsequent eight-frame writer checks exact 59 FPS process input and untouched estimator, real red RGB output, PCM block sizes and ordinary recording cleanup. The software Vulkan Wayland host can retain Enabled after a Disabled request; this expected fallback is recorded separately. Existing frame-monitor, physics clock, low-processor, manual-render and movie profiles cover connected regressions.

Evidence lives under bin/host-delta-smoothing/2026-10-21. Native allocation totals, physical GPUs, foreign platforms, real editor context, hardware cadence/latency and human acceptance are unverified. Per-sample zero allocation is a scalar-kernel result; reflection used to inspect native test state is outside that measurement. See [ADR 0016](../decisions/core-object-runtime.md#adr-0016) and [platform verification](../platform-verification.md).
