# Semaphore

Last updated: 2026-10-13

`public sealed class Semaphore : ElectronObject`

A caller-owned counted signal coordinates cooperating managed threads. `public Semaphore()` starts with zero tokens. Browser construction throws `PlatformNotSupportedException` until threaded/shared-memory bootstrap exists; desktop/mobile .NET monitors supply the implementation. Linux desktop is the exercised profile.

## Operations

| Member | Contract |
| --- | --- |
| `void Post(int count = 1)` | Adds positive tokens and wakes waiters |
| `void Wait()` | Waits for an available token and consumes exactly one |
| `bool TryWait()` | Consumes one available token; returns false without waiting for a signal when count is zero |

Tokens accumulate when no thread is waiting. The available count is bounded to UInt32 capacity. Nonpositive Post throws `ArgumentOutOfRangeException`; overflow throws `InvalidOperationException` before changing the count. Wait handles spurious wake-ups under the same monitor protecting count. An interrupted wait throws `ThreadInterruptedException`, releases its waiter bookkeeping and consumes no token. TryWait does not wait for a token, although its short internal metadata lock still serializes concurrent accesses.

Signals and their monitor boundary publish shared managed data; use Mutex when multiple writers/readers also need a critical section. Ordering/fairness is unspecified. This is a process-local synchronous primitive, with no cancellation, async policy, named OS handle, or scene/renderer access permission.

## Lifetime

Retain it, finish and join every waiting participant, then Dispose. Disposal validation rejects observed in-flight waits before starting this caller's disposal. Callers must not start new waits concurrently with disposal. Cleanup clears tokens and pulses any waiter that raced validation; such a waiter rejects disposed state instead of remaining stranded. Post/Wait/TryWait after disposal throw `ObjectDisposedException` for otherwise valid arguments. Failed validation preserves existing operations.

The semaphore is an ordinary managed object, with no scene/asset storage or extra native package. Waiting inside WorkerThreadPool occupies a worker slot; guarantee that the signal producer can execute and cannot depend on every blocked worker or blocked scene owner.

## Verification

SynchronizationTests checks zero initialization, positive post counts, exact token consumption, three concretely blocked waiters with counted wake-ups, invalid counts, atomic overflow, interruption and disposal. It checks 1024 warm operations and a real worker pipeline with all task IDs joined. GPU/compatibility scene consumers publish and draw the result. Other platforms, native allocator budgets and broad scheduling/load acceptance remain separate.

See [synchronization](../components/synchronization.md), [Mutex](Mutex.md), [worker execution](../components/worker-pool.md), [Core](../domains/core.md) and the separate [coverage register](../coverage/classes/Semaphore.md).
