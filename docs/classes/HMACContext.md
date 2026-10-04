# HMACContext

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.HMACContext`. **Inherits:** [ElectronObject](ElectronObject.md). **Source:** [HashingContext.cs](../../src/Core/Networking/HashingContext.cs).

## Description

Computes HMAC-SHA-1 or HMAC-SHA-256 over incremental message spans.

Start prepares a native-backed context with a copied nonempty key. Update requires nonempty chunks. Finish releases the computation before reuse. Calls/disposal require the constructing thread; prepared updates and caller-span finalization reuse storage, while Start and snapshots are explicit allocating work.

[Cryptographic operations](../components/crypto.md) and [ADR 0094](../decisions/networking.md#adr-0094) define algorithms, typed validation, owner/lifetime and preparation boundaries. Shared [HashType](HashType.md) and separate [AESMode](AESMode.md) preserve public selector domains. No backend handles are exposed.

## Example

Public API excerpt. CryptoTests exercises generated identities in an actual native TLS exchange; constructing TLSOptions alone does not establish a connection. AES is raw block processing without padding or authentication.

```csharp
using var crypto = new Crypto();
byte[] key = crypto.GenerateRandomBytes(32);
using var hmac = new HMACContext();
hmac.Start(HashType.SHA256, key);
hmac.Update("message"u8);
Span<byte> authentication = stackalloc byte[32];
hmac.Finish(authentication);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public HMACContext()` | Creates an idle HMAC context. |

## Constructor Descriptions

<a id="member-91cce1750080"></a>
### .ctor

`public HMACContext()`

Creates an idle HMAC context.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Byte[] Finish()` | Closes HMAC and returns copied authentication bytes. |
| `public System.Int32 Finish(System.Span<System.Byte> destination)` | Closes HMAC into caller storage. |
| `public System.Void Start(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> key)` | Starts an HMAC computation with a copied key. |
| `public System.Void Update(System.ReadOnlySpan<System.Byte> data)` | Appends a nonempty message chunk. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-1ea6cde25125"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-33996b3eeea3"></a>
### Finish

`public System.Byte[] Finish()`

Closes HMAC and returns copied authentication bytes.

Returns: 20 or 32 bytes.

<a id="member-83132d6671d1"></a>
### Finish

`public System.Int32 Finish(System.Span<System.Byte> destination)`

Closes HMAC into caller storage.

destination: At least the selected digest length.

Returns: Bytes written.

System.ArgumentException: Storage is too small; the computation remains active.

<a id="member-92b03580c00f"></a>
### Start

`public System.Void Start(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> key)`

Starts an HMAC computation with a copied key.

hashType: SHA1 or SHA256; MD5 is not part of this HMAC contract.

key: Nonempty key borrowed during preparation.

System.InvalidOperationException: A computation is already active.

System.NotSupportedException: MD5 is selected.

System.ArgumentException: The key is empty.

System.ArgumentOutOfRangeException: The selector is invalid.

<a id="member-ffbefd5bf002"></a>
### Update

`public System.Void Update(System.ReadOnlySpan<System.Byte> data)`

Appends a nonempty message chunk.

data: Bytes borrowed only during the call.

System.ArgumentException: The chunk is empty.

System.InvalidOperationException: No computation is active.

<a id="member-e418d1abb451"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[CryptoTests](../../tests/Electron2D.Tests/CryptoTests.cs) verifies known digest/RFC HMAC/NIST AES vectors, split/in-place/invalid input and lifecycle, RSA/EC interoperability, generated resource save/load and Linux TLS trust using an independent SslStream client. Prepared caller-span intervals measure managed allocations only. Provider-native costs, asymmetric/setup/snapshot work, foreign platforms, routed traffic and human acceptance remain separate. See [the component](../components/crypto.md) for exact supported widths and empty-input contracts.
