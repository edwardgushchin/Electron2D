# Network physics cost probe

This test-only host links the actual public PhysicsNetwork example and its existing backend counter/thread probe. Its isolated output uses the existing test assembly identity to read internal GPU counters. No runtime API is added. Unimplemented private partial hooks disappear from the ordinary example, including their arguments.

Build and self-check:

```sh
dotnet build tests/PhysicsNetworkCostProbe/PhysicsNetworkCostProbe.csproj -c Release -p:Electron2DBuildNativeFromSource=true -o bin/physics-network-cost-probe
dotnet bin/physics-network-cost-probe/Electron2D.Tests.dll --self-test
```

Run the original separate-process authority/client check and write one extra cost file per simulation participant:

```sh
ELECTRON2D_NETWORK_COST_DIR=/tmp/physics-network-costs dotnet bin/physics-network-cost-probe/Electron2D.Tests.dll --check --client-backend cpu --ticks 900 --late-join-tick 310 --client-poll-delay-ms 250 --unpaced --output /tmp/physics-network-check
```

Use `--client-backend gpu` with an explicitly selected Vulkan driver for the GPU early client. CPU participants still prove denied GPU startup. Add `--threaded` for the separate numerical lane; omit `--unpaced` for the original phase-paced check. Tick count, delivery impairments, warm allocation assertions, timeouts and tolerances are the example's unchanged checks. Select software Vulkan explicitly when hardware stress is excluded.

Each cost file contains nonoverlapping owner-side intervals for authority, prediction and replay preparation, whole tree physics frames, post-frame contract checks, sleep reads, snapshot capture, correction stages and presentation updates. GPU upload/readback/device-copy/uniform bytes, submission count and accumulated fence waits come from the independent resident store. CPU participants have zero GPU counters. Frame intervals include callbacks and scene work; they are not kernel timings. Initial setup byte/submission/wait counters are separate; initial wallMS is zero because setup wall time is outside this sampler. Session totals reconcile exactly to measured plus unattributed counters. Unattributed wall time includes polling, packet handling, sleeps and instrumentation outside brackets. Report capture precedes disposal.

One preallocated 4096-sample segment per region uses 655360 sample-payload bytes per participant, excluding array/object headers and the counter table. All counts and counter sums are retained; only the first bounded wall-time samples feed P50/P95/P99. Sample/unsampled counts disclose truncation. Samples include cold work and observer overhead; they are neither warmed distributions nor a matched CPU/GPU throughput comparison. Serialization/sorting occur after simulation. Brackets submit no GPU work, reject nesting, invalid order, foreign threads and counter rollback, and remain allocation-free after warmup. A cost file is accepted only after the original parent process check succeeds; files from failed cohorts are evidence only. The self-test covers known deltas, reconciliation, bounded retention, guard failures and strict zero owner/all-thread managed bytes.

See [measured scope and acceptance limits](../../docs/components/physics-network-example.md) for source-bound results. Native/driver allocations, retained history graphs, target hardware, real-window FPS and GPU advantage remain separate measurements.
