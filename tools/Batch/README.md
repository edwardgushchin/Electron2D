# Electron2D project batch runner

Build `Batch.csproj` in Release, then run the `Electron2D.Batch.dll` in its output directory. This development tool uses a local source SDK and compiled .NET 10 scenario projects. Its source-build environment must select native libraries matching that runtime, including the offline audio export needed for rendered recording; existing `Electron2DAudioNativeLibrary` and related build properties are forwarded to child builds.

```text
new /path/to/project --sdk /path/to/Electron2D
open /path/to/project
build /path/to/project --output /path/to/new-build-report
run /path/to/project --output /path/to/new-run-report
run /path/to/project --request /path/to/request.json --output /path/to/another-new-report
```

Each output directory must be new. Controller stdout is one JSON result; compiler and application output live in separate bounded log files. `report.json` contains diagnostics, observations, assertions, hashes and artifact paths. Failed builds never run stale output. Run Ctrl+C to cancel an active job.

The generated scenario performs six real steps with an action press at step one and release at step four. Edit its compiled C# source and run again to exercise a change. `Complete` explicitly establishes scenario completion; the step budget alone does not imply success.

Copy its request and select `mode: "Authoring"` with no inputs/captures to save a detached `scene.e2dscene`. A later request with `scene: "scene.e2dscene"` loads that file in a fresh process; authoring with that explicit scene selects it for typed editing and saving. Headless mode has no native window/renderer and rejects captures. Rendered mode uses an actual selected backend, fixed cadence and PNG capture at `captureSteps`, for example `[1, 6]`. It is a native window host, not a windowless renderer. Source scenes retain ordinary viewport and input composition rules.

Controller scenario execution currently requires Linux and its system session launcher; it retires inherited worker processes. Other platforms reject run until their native job owner is implemented. Build/query/scenario execution have explicit deadlines; logs and reports are bounded. Compiled scenario code has ordinary process permissions, so use projects you trust. Installed SDK distribution, import/export, visual project tooling and other platform acceptance remain separate work.

See [the complete contract](../../docs/components/project-batch.md), [compiled scenario API](../../docs/classes/BatchScenario.md) and [integration checks](../../tests/ProjectBatch/test_batch.py).
