# CryptoKey

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.CryptoKey`. **Inherits:** [Resource](Resource.md). **Source:** [CryptoKey.cs](../../src/Core/Networking/CryptoKey.cs).

## Description

Stores a copied RSA or elliptic-curve public or private key.

PEM/DER loading is transactional. Active TLS sessions prevent replacement and disposal. Private encoded buffers are cleared on replacement/disposal. Exported strings belong to the caller and cannot be cleared.

The [TLS component](../components/tls.md) and [ADR 0094](../decisions/networking.md#adr-0094) specify trust, owner/lifetime, preparation, failure and platform boundaries.

## Example

Public API excerpt. Context identifiers belong to the caller; the component records full workflow and readiness requirements.

```csharp
using var key = ResourceLoader.Load<CryptoKey>("res://network/server.key");
string publicPEM = key.SaveToString(publicOnly: true);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public CryptoKey()` | Creates an empty key resource. |

## Constructor Descriptions

<a id="member-75a97492bedb"></a>
### .ctor

`public CryptoKey()`

Creates an empty key resource.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected override System.Void Finalize()` | Clears abandoned private encoded key storage. |
| `public System.Boolean IsPublicOnly()` | Reports whether the loaded key contains only public material. |
| `public System.Void Load(System.String path, System.Boolean publicOnly = false)` | Loads a PEM or DER key from an engine filesystem path. |
| `public System.Void LoadFromString(System.String key, System.Boolean publicOnly = false)` | Loads one unencrypted PEM key. |
| `protected override System.Void OnResetState()` | Clears non-stored state when Electron2D.Resource.ResetState or Electron2D.Resource.CopyFromResource(Electron2D.Resource) requests it. |
| `public System.Void Save(System.String path, System.Boolean publicOnly = false)` | Saves PEM key text to an engine filesystem path. |
| `public System.String SaveToString(System.Boolean publicOnly = false)` | Exports copied PEM key text. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-8e04c202dab8"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-2ef53c0a255d"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-118d2486c39a"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-0fdadcd18ee6"></a>
### Finalize

`protected override System.Void Finalize()`

Clears abandoned private encoded key storage.

<a id="member-bb895883df65"></a>
### IsPublicOnly

`public System.Boolean IsPublicOnly()`

Reports whether the loaded key contains only public material.

Returns: False initially.

<a id="member-b593003a445f"></a>
### Load

`public System.Void Load(System.String path, System.Boolean publicOnly = false)`

Loads a PEM or DER key from an engine filesystem path.

path: OS, res:// or user:// path.

publicOnly: Whether only a public key is accepted.

System.Security.Cryptography.CryptographicException: The key is malformed or unsupported.

<a id="member-76dee7c8cb63"></a>
### LoadFromString

`public System.Void LoadFromString(System.String key, System.Boolean publicOnly = false)`

Loads one unencrypted PEM key.

key: PKCS#1, PKCS#8, EC private or subject-public-key PEM text.

publicOnly: Whether private-key PEM must be rejected.

System.InvalidOperationException: A TLS session is using this key.

<a id="member-5018536288a8"></a>
### OnResetState

`protected override System.Void OnResetState()`

Clears non-stored state when Electron2D.Resource.ResetState or Electron2D.Resource.CopyFromResource(Electron2D.Resource) requests it.

<a id="member-55de5a473bff"></a>
### Save

`public System.Void Save(System.String path, System.Boolean publicOnly = false)`

Saves PEM key text to an engine filesystem path.

path: Destination path.

publicOnly: Whether to export only public material.

<a id="member-f5ea7ad6211d"></a>
### SaveToString

`public System.String SaveToString(System.Boolean publicOnly = false)`

Exports copied PEM key text.

publicOnly: Whether to export only public material.

Returns: Subject-public-key or PKCS#8 PEM.

System.Security.Cryptography.CryptographicException: The key is empty or a public key was requested as private.

<a id="member-78a4a1f3be65"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) exercises the TLS/security resource contract, native Linux OpenSSL 3 streams, independent .NET SslStream interoperability and warmed managed-allocation boundaries. Other-platform TLS backend/packaging, system-wide trust variation, native OpenSSL allocation totals, routed throughput and owner acceptance remain separate. See [coverage](../coverage/classes/CryptoKey.md).

## Cryptographic operation integration

[Crypto](Crypto.md) generates private RSA resources and imports independent RSA/EC snapshots under this resource lock for signing/verification/encryption/certificate generation. Snapshot ownership ends after each operation; it can coexist with TLS retention without permitting resource replacement/disposal. CryptoTests verifies generated key save/load, independent asymmetric wires and signing while an active TLS session retains the key. See [the component](../components/crypto.md).
