# WorkerThreadPool

Last updated: 2026-10-10

**Inherits:** [ElectronObject](ElectronObject.md)

- **Source:** [WorkerThreadPool.cs](../../src/Core/OS/WorkerThreadPool.cs)
- **Namespace:** Electron2D
- **Declaration:** `public sealed class WorkerThreadPool : ElectronObject`

## Description

Process-owned typed background jobs under [ADR 0104](../decisions/jobs.md#adr-0104). Public operations are static; Engine.GetSingleton<WorkerThreadPool>(nameof(WorkerThreadPool)) returns its borrowed retained identity for typed inspection. There is no public constructor or Instance accessor. The first submission prepares threads and bounded storage from typed ProjectSettings worker settings. Queries outside callbacks return -1 without starting workers.

Callbacks borrow their state until completion is waited. Synchronize shared state or give each worker independent data. Scene/render ownership remains in force: publish mutations with SceneTree.Defer. Every submitted ID requires a wait; completion queries do not release it. Concurrent waiters registered before retirement finish safely; after the final waiter, all access to that ID rejects with ArgumentException. Waiting rethrows callback failure after cleanup.

## Methods

| Declaration | Behavior |
| --- | --- |
| `public static long AddTask(Action action, bool highPriority = false, string description = "")` | Queues one callback; returns a unique positive regular task ID. |
| `public static long AddGroupTask(Action<int> action, int elements, int tasksNeeded = -1, bool highPriority = false, string description = "")` | Executes one attempt per index [0, elements); returns a unique positive group ID. |
| `public static long GetCallerTaskID()` | Current regular callback ID; -1 in group callbacks or outside the pool. |
| `public static long GetCallerGroupID()` | Current group callback ID; -1 in regular callbacks or outside the pool. |
| `public static int GetGroupProcessedElementCount(long groupID)` | Number of attempts already returned or thrown; excludes running indices. |
| `public static bool IsTaskCompleted(long taskID)` | True after the regular callback and caller context restoration. |
| `public static bool IsGroupTaskCompleted(long groupID)` | True after all attempts and all runners finish. |
| `public static void WaitForTaskCompletion(long taskID)` | Waits, retires completion ownership, rethrows the original callback failure. |
| `public static void WaitForGroupTaskCompletion(long groupID)` | Waits for every index; rethrows the lowest-index failure after cleanup. |

Groups require nonnegative elements. Zero elements completes immediately. Any negative tasksNeeded selects all workers; a positive value caps runners by worker/element count. Zero runners is accepted only for an empty group. Actions/descriptions cannot be null. Pending capacity exhaustion throws InvalidOperationException before enqueuing anything; IDs checked at signed 64-bit overflow never wrap.

High-priority callbacks precede queued ordinary callbacks without preemption. Ordinary slots reserve a worker for high priority where possible. Outside waits never execute callbacks. Worker waits help eligible newer jobs, preserving caller IDs across nested dispatch; unfinished self/older waits throw InvalidOperationException. Completed older jobs remain waitable. There is no forced cancellation: cooperatively cancel your own callback state and still wait for the ID.

The protected `ValidateDisposal()` override rejects caller disposal before changing state. The named service is permanent and borrowed; callers cannot dispose or unregister it. Threads receive no captured caller ExecutionContext or SynchronizationContext; callbacks remain responsible for ambient state they create themselves.

## Example

```csharp
var values = new float[128];
var group = WorkerThreadPool.AddGroupTask(i => values[i] = i * .5f, values.Length);
WorkerThreadPool.WaitForGroupTaskCompletion(group);
tree.Defer(() => ApplyValues(values));
```

The example owns its arrays and delegates; their construction is outside a hot interval. [WorkerRoutes](../../examples/WorkerRoutes/README.md) demonstrates prepared delegates, independently owned grids, per-frame groups and owner publication using only public runtime API.

## Verification limits

[Worker execution](../components/worker-pool.md) records managed edge cases, warmed zero-byte sender/dispatch checks and actual Linux Wayland GPU/compatibility route pixels and cleanup. Browser submission requires a threaded bootstrap and rejects explicitly. Foreign profiles, AOT, native/OS allocations and human acceptance remain unverified. Threaded ResourceLoader and asynchronous bake consumers retain separate ownership/progress/cancellation dependencies.
