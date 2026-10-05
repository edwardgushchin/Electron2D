# AtomicFloatingPoint

Last updated: 2026-10-05

**Declaration:** `internal static class Electron2D.AtomicFloatingPoint` · **Source:** [AtomicFloatingPoint.cs](../../src/Core/Config/AtomicFloatingPoint.cs).

Internal bit-preserving scalar reads/writes for Engine timing snapshots and audio rate, latency, playlist fade and synchronized gain. Single values use integer Volatile operations; double values use Int64 Interlocked operations, including atomic access on 32-bit hosts. No public API, additional storage, locks or allocation are introduced.

The Android x86 test host reproduced incorrect floating-point Volatile reads/writes before native audio initialization. [ContractChecks](../../tests/Portability/ContractChecks.cs) verifies public timing/rate/fade round trips; [NativeChecks](../../tests/Portability/NativeChecks.cs) verifies live synchronized gains, finite positive latency and repeated native PCM lifecycles. Other platform execution remains governed by [platform verification](../platform-verification.md).
