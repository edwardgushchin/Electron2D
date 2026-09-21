# Timer

Last updated: 2026-09-21

## Declaration

- Source: [`Timer.cs`](../../src/Scene/Main/Timer.cs)
- Namespace: `Electron2D`
- Declaration: `public class Timer : Node`
- Domain: [Scene](../domains/scene.md)
- Component: [Scene tree](../components/scene-tree.md)

## Responsibility and ownership

`Timer` is a reusable Node-based countdown. It advances in one selected `SceneTree` frame lane, emits a typed timeout event when its remaining time reaches zero, and either stops or reloads. The parent Node or active tree owns it through ordinary hierarchy lifetime; the timer owns no thread, clock, task, or native handle.

Use [`SceneTreeTimer`](SceneTreeTimer.md) instead for a lightweight tree-owned one-shot delay that is not part of the Node hierarchy.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `Timer()` | Creates a stopped process-lane timer with a one-second wait |
| `TimerProcessCallback ProcessCallback { get; set; }` | Selects physics or process frames; changing it while running preserves the countdown and moves internal scheduling |
| `double WaitTime { get; set; }` | Finite positive duration in seconds, default `1`; changing it does not reset the active countdown |
| `bool OneShot { get; set; }` | Stops before the next timeout when `true`; otherwise reloads before delivery; default `false` |
| `bool Autostart { get; set; }` | Starts during the next ready delivery and then resets to `false`; default `false` |
| `bool Paused { get; set; }` | Pauses only this countdown while preserving remaining time; default `false` |
| `bool IgnoreTimeScale { get; set; }` | Uses Engine's original elapsed delta instead of the scaled callback delta; default `false` |
| `double TimeLeft { get; }` | Non-negative seconds remaining; zero while stopped and after a repeating frame overshoots by at least one full residual period |
| `event Action<Timer> Timeout` | Synchronous owner-thread delivery with the timer as sender |
| `bool IsStopped()` | Reports whether no positive remaining time is observable |
| `void Start()` | Starts or resets from `WaitTime`; requires active tree membership and does not unpause |
| `void Start(double timeSeconds)` | Validates, stores, and starts from a finite positive duration; this typed overload avoids a sentinel argument |
| `void Stop()` | Stops without timeout, sets `TimeLeft` to zero, and clears `Autostart`; valid while detached |

Inherited hierarchy, lifecycle, transform, visibility, process policy, group, path, notification, property, translation, and disposal behavior comes from [`Node`](Node.md). Stored property descriptors include `ProcessCallback`, `WaitTime`, `OneShot`, `Autostart`, and `IgnoreTimeScale`. `Paused` and `TimeLeft` are runtime-only; `TimeLeft` is read-only.

## Complete protected API

| Member | Current behavior |
| --- | --- |
| `GetPropertyDescriptors()` | Appends all seven countdown descriptors with the storage policy above |
| `CreateSceneInstanceFactory()` | Makes exact `Timer` instances packable; a derived runtime type must provide its own static exact-type factory |
| `OnNotification(int what)` | Calls inherited handling, starts on ready when requested, and consumes internal process notifications |
| `Dispose(bool disposing)` | Clears timeout subscribers and active timer state before inherited Node teardown |

## Lifecycle and ordering

The timer begins stopped. `Start` requires membership in an active `SceneTree`, resets `TimeLeft`, and enables only the selected internal frame lane unless locally paused. `Stop` disables both internal lanes. Detaching does not destroy configured or countdown state; ordinary Node reattachment/lifetime rules apply.

Ready delivery starts an autostart timer after inherited ready handling and clears `Autostart`. Setting `Autostart` after ready has no immediate effect. `RequestReady()` followed by a later attachment can provide another ready cycle.

On a matching eligible frame, the timer subtracts either the scaled Node delta or Engine's original delta. A direct `SceneTree.Process`/`PhysicsProcess` call has no separate time-scale source, so both values equal the supplied delta. At zero or below, a one-shot timer stops before `Timeout`; a repeating timer adds the current `WaitTime` before `Timeout`. At most one event is emitted per frame, even when one delta spans several periods. Overshoot is retained internally, so later frames catch up one event at a time and public `TimeLeft` remains clamped to zero while the internal residual is non-positive.

Internal timer processing precedes the same node's public `OnProcess`/`OnPhysicsProcess`. A timeout exception is retained while the public callback and later scheduled nodes are attempted. If the timeout handler disables that public lane, disposes, or detaches the timer, its public callback is skipped; enabling a previously disabled public lane does not inject a callback into the already captured turn. Frame-level failures are reported by `SceneTree` as an aggregate.

## Invariants and error behavior

- `WaitTime` and explicit start duration must be finite and greater than zero. Rejection preserves existing configuration and countdown state.
- `ProcessCallback` accepts only defined enum values. Rejection is non-mutating.
- `Start` while detached throws `InvalidOperationException`; `Stop` remains valid while detached.
- Starting while paused resets the countdown without resuming it.
- Tree pause eligibility still follows inherited `Node.ProcessMode`; `Paused` is an additional local gate.
- The timer emits no event from `Stop`, disposal, detachment, or a nonmatching frame.
- Very short waits remain frame-quantized; no wall-clock or sub-frame delivery is promised.

## Threading guarantees and non-guarantees

Attached mutation, start/stop, ready handling, countdown advance, timeout delivery, and disposal use the owning `SceneTree` thread. Detached configuration has the same unsynchronized caller-owned semantics as `Node`. Event subscription and reads do not become thread-safe. No worker, task, synchronization context, sleep, or native timer exists.

## Official reference coverage inventory

The stable reference API, implementation source, and complete `Node`/`Object` inheritance chain were checked on 2026-09-21.

| Reference area | Electron2D disposition |
| --- | --- |
| `process_callback`, `wait_time`, `one_shot`, `autostart`, `paused`, `ignore_time_scale`, `time_left` | Implemented as typed properties with matching defaults, runtime/storage roles, validation, lane changes, pause behavior, and original-delta support |
| `start`, `stop`, `is_stopped` | Implemented; the optional negative sentinel is deliberately replaced by `Start()` and `Start(double)` overloads |
| `timeout` signal | Implemented as `event Action<Timer>` with sender-first typed delivery |
| Ready-time autostart and reset | Implemented; runtime ready starts and clears the flag |
| Physics/process selection, tree pause, local pause, and time scale | Implemented through Node internal lanes, inherited process policy, and Engine dual-delta delivery; unscaled physics uses the original fixed step rather than a variable process step, preserving documented real-time behavior at any configured physics rate |
| One event per frame and short-duration frame quantization | Implemented and documented; no catch-up loop or wall-clock promise is added |
| Editor configuration warning for very short waits and editor-only autostart suppression | Deferred until an editor runtime and tooling-warning contract exist; no inert warning API is exposed |
| Inherited dynamic calls/properties/scripts/metadata and untyped signal APIs | Permanently replaced or excluded by Electron2D's typed object, property, and C# event decisions |

## Dependencies and interactions

`Timer` depends on `Node` internal frame lanes, `SceneTree` scheduling and pause eligibility, `MainLoop`'s current original delta, and `Engine` dual scaled/original delivery. `PackedScene` consumes its storage-enabled typed descriptors. It does not depend on SDL3-CS, rendering, input, audio, collision physics, scripting, file scene serialization, or editor code.

## Verification and known limitations

Executable checks cover identities/defaults, descriptor storage, detached behavior, invalid and non-mutating configuration, process and physics lanes, live lane migration, exact-zero and overshooting countdowns, repeating and one-shot event state, pause combinations, autostart, stop, owner-thread rejection, throwing callbacks, callback detachment, packed-scene restoration, Engine time scale zero in both lanes, and zero warmed managed allocation while stopped or running.

Verification uses deterministic supplied deltas on Linux. It does not establish host cadence, wall-clock accuracy, mobile/native scheduling, editor behavior, or loaded-scene performance.

## Relevant decisions

- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0008: Unified Node](../decisions/scene.md#adr-0008)
- [0014: Realtime allocation](../decisions/resources.md#adr-0014)
- [0016: Engine scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
