# Managed synchronization

Last updated: 2026-10-13

## Scope and ownership

[ADR 0104](../decisions/jobs.md#adr-0104) provides caller-owned [Mutex](../classes/Mutex.md) and [Semaphore](../classes/Semaphore.md) over the accepted managed worker foundation. Both derive from ElectronObject under managed lifetime. Runtime source is [Mutex.cs](../../src/Core/OS/Mutex.cs) and [Semaphore.cs](../../src/Core/OS/Semaphore.cs); no vendor/backend type escapes into public signatures.

Mutex provides recursive same-thread acquisition/release and immediate TryLock. Semaphore provides zero-initialized UInt32 token storage, positive counted Post, blocking Wait and token-only nonblocking TryWait. Monitors serialize state and publish shared managed data. Invalid release, invalid/overflow post, interrupted wait and disposed state have typed exceptions. These are preparation/worker coordination operations; no hidden scene loop or async policy is introduced.

## Runtime flow and lifetime

Construct and retain primitives before submitting cooperating work. Producers protect shared data with balanced mutex releases, post signals, and publish scene changes through SceneTree.Defer. Callers wait every WorkerThreadPool task ID. Waiting in a worker occupies a slot, so producer dependencies must remain executable. After all participants finish and join, dispose the primitives. Validation rejects observed held/pending operations; it does not authorize disposal races. Race cleanup releases failed mutex entries and pulses semaphore waiters so invalid lifetime use does not strand them. Built-in permanent services do not own or dispose user primitives.

Browser constructors reject until a threaded/shared-memory runtime bootstrap exists. Desktop/mobile .NET monitors are the foundation; only exercised Linux profiles count as verified. No named/process-shared synchronization, caller-owned Thread lifecycle, cancellation/forced termination or editor control is supplied by these primitives.

## Verification

SynchronizationTests checks recursion, blocking/try operations, ownership, exact tokens/counts, capacity overflow, interrupted waits, disposal and four-thread protected writes. Prepared 1024-cycle operations and a real retained worker pipeline check managed allocation counters on owner/worker. A public native consumer uses both primitives, Defer publication and actual pixel readback on GPU/compatibility, then joins the task and tears down the window. Foreign platforms, native allocation and broad load/scheduling acceptance remain separate.
