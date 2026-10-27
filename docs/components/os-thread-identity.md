# Thread identities and native names

The retained OS service supplies `GetThreadCallerID`, `GetMainThreadID` and
`SetThreadName` under [ADR 0113](../decisions/host-platform.md#adr-0113).

## Identity and lifecycle

These are opaque process-local engine identities. The reserved main identity is
one; ordinary callers receive distinct identities starting above it. A first call
on a worker cannot designate that worker as main. Identity reads do not create an
Engine lifecycle, window or worker pool.

Engine's existing owner acquisition publishes the main role before manual-loop
initialization or native-window startup. Initialization, scene readiness, frame
hooks and finalization therefore observe that role on the actual owner. Rejected
concurrent startup leaves the active role intact. Normal shutdown and failed
startup/finalization clear it before the next lifecycle can start. A later
library lifecycle reports its actual caller even on a different thread; retained
services keep their independent owner restrictions. For example, AudioServer
teardown rejects a caller different from its previously selected configuration
owner. That failure still retires the Engine role. Outside the role, a caller retains
its ordinary identity. Asynchronous host cleanup clears the shared role even if
a faulty host continuation arrives on another thread.

IDs do not grant scene or native access. Existing managed-owner and processing
group guards remain authoritative. IDs are neither native kernel identifiers nor
persistent values across processes. The public Thread start/join/priority and
safety-check contracts retain their separate implementation dependency.

## Native names

`SetThreadName` names only the calling native thread. It leaves the managed
`System.Threading.Thread.Name` property alone. Linux/Android use the caller's
pthread, Apple uses its current-thread naming operation, and Windows uses
SetThreadDescription on the current-thread pseudo handle. The operation borrows
the caller; it owns no thread handle or process lifetime.

Null and embedded-NUL input reject before submission. POSIX errors become typed
argument exceptions with the native error retained; Windows errors become explicit
operation failures. Native byte-length rules apply without silent truncation;
Linux accepts fifteen UTF-8 bytes, including an empty name. Unsupported profiles
throw NotSupportedException. Naming is a cold operation and may allocate.

## Verification

The public-only [OSThreads.Consumer](../../tests/OSThreads.Consumer/OSThreads.Consumer.csproj)
reuses [OSThreadTests](../../tests/Electron2D.Tests/OSThreadTests.cs). Its managed
profile covers worker-first and twelve simultaneous first-use callers, actual
pool callback identity, lifecycle-owner transfer, rejected concurrent startup,
initialization/finalization failures and prepared moving-scene frames. Its Linux
name oracle reads `/proc/thread-self/comm` independently of the implementation,
including repeated Unicode/empty names, the fifteen-byte boundary and failed
sixteen-byte ASCII/multibyte edits preserving the previous name.

Native profiles exercise ordinary Window readiness, the before-frame hook,
startup failure and service retirement through the real requested renderer.
Source-consumer receipts belong under `bin/os-thread-identity/2026-10-27/`.
Foreign-platform execution, native heap totals, isolated performance and package
qualification require their own checks.

The Linux CLR and strict full-trim NativeAOT consumers passed 33 managed/native-name
checks plus nine native owner/retirement checks on each of Wayland compatibility
and software GPU. Each managed profile measured 128 complete moving-scene frames
at zero owner-thread managed bytes. The NativeAOT executable SHA-256 is
`cc765253d4e5597431cd2c6a92cb2e1cc68597b135f86f099f8e3f83bdf47ece`.
These source-consumer results do not qualify distributed packages.
