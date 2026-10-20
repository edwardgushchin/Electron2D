# Compiled project batch execution

Last updated: 2026-10-20

## Responsibility and boundary

`tools/Batch/Batch.csproj` builds `Electron2D.Batch`, a separate development tool and compiled-scenario SDK. It consumes only public runtime capabilities. The runtime never depends on this tool or a game assembly. The controller owns project files, build processes, deadlines, bounded logs and reports; [BatchHost](../classes/BatchHost.md) owns a single child scenario. The CLI is the common operation boundary available to future visual frontends; the current editor startup screen does not implement these operations itself.

This first SDK uses a local source SDK directory and ordinary .NET 10 executable projects. Source builds require a native closure matching the current runtime. The controller forwards ordinary build environment properties such as `Electron2DAudioNativeLibrary`, `Electron2DTextNativeLibrary` and `Electron2DENetNativeLibrary` to owned builds; rendered recording requires the offline-audio exports introduced by its runtime slice. A stale packaged native payload fails with a runtime diagnostic and cannot establish rendered acceptance. This development tool does not silently rebuild or replace installed native packages. Distribution as an installed SDK, project import/export, a visual project editor, mobile/browser controller hosts and provider-specific adapters remain absent. A compiled scenario is trusted user code with normal process permissions; this tool is not a security sandbox.

## Project operations

After building the tool, invoke `dotnet tools/Batch/bin/Release/net10.0/linux-x64/Electron2D.Batch.dll` followed by one operation:

| Operation | Arguments | Result |
| --- | --- | --- |
| `new` | `DIRECTORY --sdk SDK_DIRECTORY` | Creates `Game.csproj`, `Program.cs`, `scenario.json`, `electron2d.project.json` and `.gitignore` atomically in a new directory. |
| `open` | `DIRECTORY` | Validates the version-one manifest and its project-relative `.csproj`; activates no game or window. |
| `build` | `DIRECTORY --output NEW_DIRECTORY` | Runs a Release build and obtains its executable TargetPath through the SDK build system. |
| `run` | `DIRECTORY --output NEW_DIRECTORY [--request REQUEST_FILE]` | Reads the explicit request or project `scenario.json`, builds, then launches the compiled executable in a separate process. |

Project and output creation preserve existing paths. A new output directory is reserved atomically, and report replacement inside it is atomic. Each project has one live `.electron2d/job.lock` file lease; a leftover file without an open lease does not block a job. Builds reuse `.electron2d/build`, with shared compiler/node reuse disabled for owned jobs. The controller does not run a stale executable after a failed build. Spaces in paths are passed as individual arguments. Manifest and scene references stay inside the chosen project and automatic paths reject symbolic links.

Only the final JSON result goes to controller stdout. Build, target-query and application stdout/stderr have separate log files, each limited to one MiB plus a truncation marker; excess output is drained. `report.json` is the durable result, `run.json` is the child result and `request.json` preserves the exact request. Child results must match the job nonce, request hash, built assembly hash and actual process exit code.

## Three modes

- **Authoring:** Configure registers compiled schemas; the factory creates a detached source root or a requested scene file is loaded. The scenario's Author hook performs typed edits, then ResourceSaver.SaveScene writes the selected scene. A new factory scene uses `scene.e2dscene`; an existing automatic destination is preserved until explicitly selected through `scene`. No SceneTree, Ready, process, physics, native window or renderer is started by the host. User factory/hook code must preserve this detached contract. The existing serializer owns atomic file replacement and scene-save notifications.
- **Headless:** An ordinary non-Window root enters a real SceneTree and Engine.Start/AdvanceFrame/Stop lifecycle. The public scheduler runs fixed/process lanes at the requested cadence. Input.ParseInputEvent commits global state and delivers before the specified step. The host supplies no native window, renderer or simulated image. A Window root requires rendered mode; viewport/window-dependent project operations retain their runtime capability requirements. The public SceneTree constructor performs initial readiness before Engine.Start attaches it, as in ordinary manual embedding.
- **Rendered:** A Window factory root is used directly; another source root is attached below an ordinary host Window. The requested canvas dimensions configure that window. Engine.Run uses actual rendering, fixed movie cadence and synchronous recording audio. A public before-frame overload injects transport input before the first physics/process step, without warmup simulation. The source root preserves ordinary canvas/viewport routing; offscreen scene content still needs its authored presentation/input composition. A custom MovieWriter observes completed screen-encoded RGB8 frames and saves requested PNGs. This is native window rendering; it is not a windowless GPU host.

The requested FPS also selects physics tick frequency, with zero jitter correction and time scale one. Scene pause, processing, physics, deferred work, input transitions, exceptions and teardown execute through runtime services. There is no alternative scene or physics loop. Configure/factory/Author/BeforeStep/AfterStep and Dispose are compiled C# callbacks, not reflected member dispatch.

A scenario calls Complete to request success after the current completed step. Reaching the step budget without completion fails. Early scene quit, callback/assertion failure and unavailable capabilities cannot return success. A rendered request that quits before its final image completes cannot claim a completed step or capture.

## Request and result protocol

The UTF-8 JSON protocol uses version `1`, camel-case fields and named enum values. Unknown fields, unsupported versions, invalid collection values and null models reject. A protocol file is limited to eight MiB.

| Request field | Default and validation |
| --- | --- |
| `version` | `1`; exact match required. |
| `mode` | `Headless`; Authoring, Headless or Rendered. |
| `steps` | `60`; 1..1000000, maximum completed steps. |
| `fps` | `60`; integer 1..240, process and physics cadence. |
| `width`, `height` | `320`, `180`; 2..4096 pixels per axis. |
| `backend` | `gpu`; gpu or compatibility, with automatic fallback disabled. |
| `seed` | `1`; unsigned 64-bit seed for the borrowed context generator. |
| `deadlineMilliseconds` | `30000`; 100..3600000 for scenario execution after build. |
| `scene` | Null; an existing project-relative typed scene instead of calling the factory. Authoring updates this explicitly selected file. |
| `captureSteps` | Empty; at most 64 unique one-based steps inside the budget, Rendered only. |
| `inputs` | Empty; at most 65536 records in nondecreasing step order. Authoring rejects input. |

An input record has `step`, exactly one of `action` or `key`, `pressed`, optional `echo` (key only), and `strength` (default 1, finite 0..1). Action names must be registered by Configure. Key names use the runtime Key enum. Same-step records retain file order. The first transport supports named actions and logical keys; scenario code can use other typed public input events in BeforeStep without universal payloads or a second dispatcher.

Results name operation, job, mode, project, platform, engine version, engine/project assembly SHA-256, request/optional source-scene SHA-256, actual renderer/driver, seed, FPS, budget and completed steps. Typed observations hold one numeric, boolean or text value; assertions carry a name, outcome and explanation. Artifacts carry kind, absolute path, completed step and SHA-256. Diagnostics have stable codes, messages and exception types. Reporting allows 4096 entries, names up to 256 characters, text up to 4096 characters and a conservative four-MiB observation/assertion JSON budget. Diagnostics are bounded independently. This allocating observation/capture work is outside engine hot-path allocation measurements.

| Exit | Meaning |
| --- | --- |
| 0 | Completed authoring or explicitly completed simulation. |
| 2 | Invalid command/project/request/input or preserved existing destination. |
| 3 | Build/target failure. |
| 4 | Runtime, early-exit, malformed child result, report/log or cleanup failure. |
| 5 | Scenario assertion failed. |
| 6 | Step budget exhausted. |
| 7 | A requested mode/capture/job-control capability is unavailable. |
| 124 | Scenario or bounded build/query process deadline exceeded. |
| 130 | Controller cancellation. |

Build and target evaluation have separate five-minute and thirty-second limits. Cancellation retires owned processes. On Linux, scenario jobs use the platform session launcher and terminate their own process group, including inherited workers after the leader exits. Build helpers disable compiler/node reuse to avoid owning a shared daemon. The controller gives process/pipe shutdown a bounded grace period; termination/reporting failures remain failures. Controller scenario execution currently rejects other platforms with capability exit 7 until a complete native job owner is implemented there. New/open/build use ordinary .NET tooling but remain verified only on Linux; direct BatchHost entry has no controller isolation. Windows requires its job-object lifecycle, and other POSIX hosts require a verified process-group lifecycle before enabling controller run.

## Executable verification and limits

`tests/ProjectBatch/test_batch.py` exercises real CLI-created projects, open/build/run, a focused C# change, detached authoring, fresh-process typed scene loading, deterministic input/state, explicit completion, native images and failure recovery. It checks preserved project/scene/output files, invalid versions/fields/input ordering, headless capture rejection, callback/assertion/build failures, bounded steps/logs/reports, deadlines, cancellation and inherited-worker cleanup. `--native` adds actual GPU and Compatibility frames and an independent standard-library PNG decoder/hash/pixel check. Native test environments select software Vulkan/GL explicitly; headless tests use a missing Vulkan driver and dummy video.

The existing `ELECTRON2D_TEST_MOVIE_WRITER=portable`, `gpu` and `compatibility` profiles include [EngineBeforeFrameTests](../../tests/Electron2D.Tests/EngineBeforeFrameTests.cs), which verifies all six callback overloads, original delegate shapes, readiness/input ordering, skipped pending steps on quit, synchronous/asynchronous callback failure cleanup and per-run indices. A prepared synchronous window profile measures 24 warmup plus 64 complete owner-thread intervals with zero managed bytes. Recording readback, tool reports, native/device allocations, physical GPU, foreign platforms, packaging and human game/visual acceptance remain separate. Fixed steps and a generator seed do not promise bitwise equality across platforms or eliminate external native input.
