# AESContext

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.AESContext`. **Inherits:** [ElectronObject](ElectronObject.md). **Source:** [AESContext.cs](../../src/Core/Networking/AESContext.cs).

## Description

Incrementally encrypts/decrypts complete AES-128/AES-256 ECB or CBC blocks without padding.

Start prepares the BCL transform and copied key/IV. Updates require whole 16-byte blocks; no padding or authentication is added. Caller-span updates and IV reads reuse storage. Finish clears engine buffers and releases the transform/key schedule. Calls and disposal require the constructing thread.

[Cryptographic operations](../components/crypto.md) and [ADR 0094](../decisions/networking.md#adr-0094) define algorithms, typed validation, owner/lifetime and preparation boundaries. Shared [HashType](HashType.md) and separate [AESMode](AESMode.md) preserve public selector domains. No backend handles are exposed.

## Example

Public API excerpt. CryptoTests exercises generated identities in an actual native TLS exchange; constructing TLSOptions alone does not establish a connection. AES is raw block processing without padding or authentication.

```csharp
using var crypto = new Crypto();
byte[] key = crypto.GenerateRandomBytes(32);
byte[] iv = crypto.GenerateRandomBytes(16);
using var aes = new AESContext();
aes.Start(AESMode.CBCEncrypt, key, iv);
byte[] ciphertext = aes.Update("one full block!!"u8); // Exactly 16 bytes.
aes.Finish();
// Preserve the IV separately and authenticate ciphertext in the application protocol.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AESContext()` | Creates an idle AES context with prepared block/IV buffers. |

## Constructor Descriptions

<a id="member-3f5dd1740c92"></a>
### .ctor

`public AESContext()`

Creates an idle AES context with prepared block/IV buffers.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Void Finish()` | Releases the stream and clears IV/block/key storage; safe while idle. |
| `public System.Byte[] GetIVState()` | Gets a copied current CBC chaining IV. |
| `public System.Int32 GetIVState(System.Span<System.Byte> destination)` | Copies the current CBC chaining IV. |
| `public System.Void Start(Electron2D.AESMode mode, System.ReadOnlySpan<System.Byte> key, System.ReadOnlySpan<System.Byte> iv = default)` | Starts a raw AES stream with a copied key and CBC IV. |
| `public System.Byte[] Update(System.ReadOnlySpan<System.Byte> source)` | Transforms complete blocks into a copied result. |
| `public System.Int32 Update(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination)` | Transforms complete blocks into caller storage. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-6f8cb7330208"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-805bf3240721"></a>
### Finish

`public System.Void Finish()`

Releases the stream and clears IV/block/key storage; safe while idle.

<a id="member-28170378e1c8"></a>
### GetIVState

`public System.Byte[] GetIVState()`

Gets a copied current CBC chaining IV.

Returns: 16 bytes; encryption tracks last ciphertext output and decryption last ciphertext input.

<a id="member-3f5d3bc57d2c"></a>
### GetIVState

`public System.Int32 GetIVState(System.Span<System.Byte> destination)`

Copies the current CBC chaining IV.

destination: At least 16 bytes.

Returns: 16.

System.InvalidOperationException: No CBC context is active.

System.ArgumentException: Output storage is too small.

<a id="member-b130c4111d5d"></a>
### Start

`public System.Void Start(Electron2D.AESMode mode, System.ReadOnlySpan<System.Byte> key, System.ReadOnlySpan<System.Byte> iv = default)`

Starts a raw AES stream with a copied key and CBC IV.

mode: An ECB/CBC encrypt/decrypt selector.

key: Exactly 16 or 32 bytes.

iv: Exactly 16 bytes for CBC; ignored for ECB.

System.ArgumentException: Key/IV lengths do not match the mode.

System.ArgumentOutOfRangeException: The mode is invalid or Max.

System.InvalidOperationException: Finish has not closed the previous stream.

<a id="member-4ff21deb5bc9"></a>
### Update

`public System.Byte[] Update(System.ReadOnlySpan<System.Byte> source)`

Transforms complete blocks into a copied result.

source: A multiple of 16 bytes; empty is valid.

Returns: Exactly the source length.

<a id="member-9f3e8ae87c56"></a>
### Update

`public System.Int32 Update(System.ReadOnlySpan<System.Byte> source, System.Span<System.Byte> destination)`

Transforms complete blocks into caller storage.

source: Whole blocks, borrowed during the call.

destination: Enough storage; exact in-place overlap is supported.

Returns: Bytes written, equal to source length.

System.ArgumentException: Input is not block aligned, storage is too small or buffers partially overlap; stream state is preserved.

System.InvalidOperationException: No AES stream is active.

<a id="member-e5f7ef01f8b7"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[CryptoTests](../../tests/Electron2D.Tests/CryptoTests.cs) verifies known digest/RFC HMAC/NIST AES vectors, split/in-place/invalid input and lifecycle, RSA/EC interoperability, generated resource save/load and Linux TLS trust using an independent SslStream client. Prepared caller-span intervals measure managed allocations only. Provider-native costs, asymmetric/setup/snapshot work, foreign platforms, routed traffic and human acceptance remain separate. See [the component](../components/crypto.md) for exact supported widths and empty-input contracts.
