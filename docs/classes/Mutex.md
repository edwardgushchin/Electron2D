# Mutex

Last updated: 2026-10-13

`public sealed class Mutex : ElectronObject`

A caller-owned recursive mutex protects shared managed data. Construct it unlocked with `public Mutex()`. The current browser bootstrap rejects construction with `PlatformNotSupportedException`; desktop/mobile .NET monitors supply the implementation. Linux desktop is the exercised profile.

## Operations

| Member | Contract |
| --- | --- |
| `void Lock()` | Waits for another owner's release, then acquires; the owner can acquire recursively |
| `bool TryLock()` | Immediately returns false if another thread owns it; success adds an acquisition, including recursion |
| `void Unlock()` | Releases one acquisition on the same managed thread |

Every successful Lock/TryLock needs one Unlock. Release in a finally block. Wrong-thread or excess releases throw `SynchronizationLockException` without changing ownership. An interrupted blocked Lock throws `ThreadInterruptedException` without retaining the monitor or a pending acquisition. Recursion beyond Int32 capacity throws `OverflowException` and releases only the failed attempt.

Monitor acquisition/release publishes shared managed data. This mutex is process-local, unnamed and synchronous. It grants no scene or native-render access to a worker. Do not hold a lock across asynchronous continuations that can change the managed thread.

## Lifetime and errors

Retain the mutex through all participants, balance releases, join their work and then Dispose. Disposal validation rejects an observed held mutex or in-flight acquisition with `InvalidOperationException` before this caller starts disposal. This is validation, not permission to race new operations with disposal. Acquisition rejects once disposal starts; a raced monitor entry is released before throwing `ObjectDisposedException`. An already accepted owner may still Unlock if disposal raced its acquisition, so it can complete finally cleanup. New acquisition on that disposed object stays prohibited.

The managed monitor needs no named OS handle or extra native asset. It is not a resource and stores no scene/asset data. Ordering/fairness and scheduling latency are unspecified. Warm uncontended acquisition/release allocates no managed owner bytes; contended/native runtime allocations remain a separate gate.

## Verification

SynchronizationTests checks recursion, immediate contention failure, wrong/excess release, a concretely blocked contender, balanced release, held-object disposal rejection, interruption cleanup, and four concurrent writers with 8000 retained updates. It checks 1024 warmed recursive/signal operations and an actual WorkerThreadPool pipeline. Native GPU/compatibility consumers publish protected data through SceneTree.Defer and verify pixels/teardown. Other platforms, native allocator budgets and broad scheduling/load acceptance remain unverified.

See [synchronization](../components/synchronization.md), [worker execution](../components/worker-pool.md), [Core](../domains/core.md), and the separate [coverage register](../coverage/classes/Mutex.md).
