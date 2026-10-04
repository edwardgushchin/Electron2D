# HashingContext

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.HashingContext`. **Inherits:** [ElectronObject](ElectronObject.md). **Source:** [HashingContext.cs](../../src/Core/Networking/HashingContext.cs).

## Description

Computes MD5, SHA-1 or SHA-256 incrementally over caller-owned chunks.

Start prepares a native-backed BCL context. Update borrows its span and allocates no engine buffers. Finish closes the current computation; a too-small destination preserves it. Operations/disposal require the constructing thread. Snapshot results allocate; Start is preparation and native provider costs are external.

[Cryptographic operations](../components/crypto.md) and [ADR 0094](../decisions/networking.md#adr-0094) define algorithms, typed validation, owner/lifetime and preparation boundaries. Shared [HashType](HashType.md) and separate [AESMode](AESMode.md) preserve public selector domains. No backend handles are exposed.

## Example

Public API excerpt. CryptoTests exercises generated identities in an actual native TLS exchange; constructing TLSOptions alone does not establish a connection. AES is raw block processing without padding or authentication.

```csharp
using var hash = new HashingContext();
hash.Start(HashType.SHA256);
hash.Update("first chunk"u8);
hash.Update("second chunk"u8);
Span<byte> digest = stackalloc byte[32];
hash.Finish(digest);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public HashingContext()` | Creates an idle hashing context. |

## Constructor Descriptions

<a id="member-c2c9d98d73e3"></a>
### .ctor

`public HashingContext()`

Creates an idle hashing context.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Byte[] Finish()` | Closes the current digest and returns copied bytes. |
| `public System.Int32 Finish(System.Span<System.Byte> destination)` | Closes the current digest into caller storage. |
| `public System.Void Start(Electron2D.HashType type)` | Starts a digest computation. |
| `public System.Void Update(System.ReadOnlySpan<System.Byte> chunk)` | Appends one nonempty chunk. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-0952b906be01"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-5a84e64c4ee6"></a>
### Finish

`public System.Byte[] Finish()`

Closes the current digest and returns copied bytes.

Returns: 16, 20 or 32 bytes for the selected algorithm.

<a id="member-a060bd0c227b"></a>
### Finish

`public System.Int32 Finish(System.Span<System.Byte> destination)`

Closes the current digest into caller storage.

destination: At least the selected digest length.

Returns: Bytes written.

System.ArgumentException: Storage is too small; the computation remains active.

<a id="member-51f4eacdcdcb"></a>
### Start

`public System.Void Start(Electron2D.HashType type)`

Starts a digest computation.

type: MD5, SHA1 or SHA256.

System.InvalidOperationException: A computation is already active.

System.ArgumentOutOfRangeException: The selector is invalid.

<a id="member-680a35361abe"></a>
### Update

`public System.Void Update(System.ReadOnlySpan<System.Byte> chunk)`

Appends one nonempty chunk.

chunk: Bytes borrowed only during this call.

System.ArgumentException: The chunk is empty.

System.InvalidOperationException: No computation is active.

<a id="member-2180bc004b21"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[CryptoTests](../../tests/Electron2D.Tests/CryptoTests.cs) verifies known digest/RFC HMAC/NIST AES vectors, split/in-place/invalid input and lifecycle, RSA/EC interoperability, generated resource save/load and Linux TLS trust using an independent SslStream client. Prepared caller-span intervals measure managed allocations only. Provider-native costs, asymmetric/setup/snapshot work, foreign platforms, routed traffic and human acceptance remain separate. See [the component](../components/crypto.md) for exact supported widths and empty-input contracts.
