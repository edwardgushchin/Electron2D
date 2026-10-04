# X509Certificate

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.X509Certificate`. **Inherits:** [Resource](Resource.md). **Source:** [X509Certificate.cs](../../src/Core/Networking/X509Certificate.cs).

## Description

Stores a copied ordered X.509 certificate chain.

Loads append certificates, preserving chain order. Malformed input leaves state unchanged. Active TLS sessions prevent loading/disposal. Duplication copies certificate bytes and never shares native handles.

The [TLS component](../components/tls.md) and [ADR 0094](../decisions/networking.md#adr-0094) specify trust, owner/lifetime, preparation, failure and platform boundaries.

## Example

Public API excerpt. Context identifiers belong to the caller; the component records full workflow and readiness requirements.

```csharp
using var chain = new X509Certificate();
chain.Load("res://network/server.crt");
string pem = chain.SaveToString();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public X509Certificate()` | Creates an empty certificate chain. |

## Constructor Descriptions

<a id="member-2fe0ab8edb67"></a>
### .ctor

`public X509Certificate()`

Creates an empty certificate chain.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Copies derived stored state into a duplicate or copy target. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Void Load(System.String path)` | Loads PEM certificates or one DER certificate from an engine filesystem path. |
| `public System.Void LoadFromString(System.String certificates)` | Appends every certificate from PEM text. |
| `protected override System.Void OnResetState()` | Clears non-stored state when Electron2D.Resource.ResetState or Electron2D.Resource.CopyFromResource(Electron2D.Resource) requests it. |
| `public System.Void Save(System.String path)` | Saves the whole ordered chain as PEM text. |
| `public System.String SaveToString()` | Exports every chain certificate as PEM text. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-e8d52724879f"></a>
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

<a id="member-d986431f912e"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-6298653d7cb4"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

Remarks: Unregisters the cache path and clears resource event subscribers before base cleanup.

<a id="member-9999dd0967e8"></a>
### Load

`public System.Void Load(System.String path)`

Loads PEM certificates or one DER certificate from an engine filesystem path.

path: OS, res:// or user:// path.

<a id="member-14dffdf388ca"></a>
### LoadFromString

`public System.Void LoadFromString(System.String certificates)`

Appends every certificate from PEM text.

certificates: One or more certificate blocks in chain order.

System.Security.Cryptography.CryptographicException: No valid certificate chain is encoded.

System.InvalidOperationException: TLS is using this chain.

<a id="member-2a13b9ca75ee"></a>
### OnResetState

`protected override System.Void OnResetState()`

Clears non-stored state when Electron2D.Resource.ResetState or Electron2D.Resource.CopyFromResource(Electron2D.Resource) requests it.

<a id="member-38eb6ea54add"></a>
### Save

`public System.Void Save(System.String path)`

Saves the whole ordered chain as PEM text.

path: Destination engine filesystem path.

<a id="member-fe80dce3b72d"></a>
### SaveToString

`public System.String SaveToString()`

Exports every chain certificate as PEM text.

Returns: Empty for an empty resource.

<a id="member-65115ae14335"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) exercises the TLS/security resource contract, native Linux OpenSSL 3 streams, independent .NET SslStream interoperability and warmed managed-allocation boundaries. Other-platform TLS backend/packaging, system-wide trust variation, native OpenSSL allocation totals, routed throughput and owner acceptance remain separate. See [coverage](../coverage/classes/X509Certificate.md).
