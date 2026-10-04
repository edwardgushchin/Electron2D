# TLSOptions

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public sealed class Electron2D.TLSOptions`. **Inherits:** [ElectronObject](ElectronObject.md). **Source:** [TLSOptions.cs](../../src/Core/Networking/TLSOptions.cs).

## Description

Describes an immutable TLS client or server role with borrowed key/certificate resources.

Factories preserve resource identity. Sessions retain resources during use; disposing options never disposes them. Client uses certificate-chain and hostname validation; ClientUnsafe skips name validation; a supplied trust chain still requires valid certificate-chain verification.

The [TLS component](../components/tls.md) and [ADR 0094](../decisions/networking.md#adr-0094) specify trust, owner/lifetime, preparation, failure and platform boundaries.

## Example

Public API excerpt. Context identifiers belong to the caller; the component records full workflow and readiness requirements.

```csharp
using var options = TLSOptions.Client(trustedChain, "service.example");
// Keep trust resources live until every TLS session disconnects.
```

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public static Electron2D.TLSOptions Client(Electron2D.X509Certificate trustedChain = null, System.String commonNameOverride = "")` | Creates a validating client configuration. |
| `public static Electron2D.TLSOptions ClientUnsafe(Electron2D.X509Certificate trustedChain = null)` | Creates an explicit client configuration without expected-name rejection. |
| `public System.String GetCommonNameOverride()` | Gets the expected-name override. |
| `public Electron2D.X509Certificate GetOwnCertificate()` | Gets the borrowed server certificate chain. |
| `public Electron2D.CryptoKey GetPrivateKey()` | Gets the borrowed server private key. |
| `public Electron2D.X509Certificate GetTrustedCAChain()` | Gets the borrowed custom trust chain. |
| `public System.Boolean IsServer()` | Reports whether this configuration accepts incoming TLS sessions. |
| `public System.Boolean IsUnsafeClient()` | Reports whether expected-name rejection is disabled. |
| `public static Electron2D.TLSOptions Server(Electron2D.CryptoKey key, Electron2D.X509Certificate certificate)` | Creates a server configuration. |

## Method Descriptions

<a id="member-a23143da4bfe"></a>
### Client

`public static Electron2D.TLSOptions Client(Electron2D.X509Certificate trustedChain = null, System.String commonNameOverride = "")`

Creates a validating client configuration.

trustedChain: Custom trust anchors, or null for system trust.

commonNameOverride: Expected certificate name override; empty uses ConnectToStream's commonName.

Returns: A caller-owned configuration.

<a id="member-62b8863026b3"></a>
### ClientUnsafe

`public static Electron2D.TLSOptions ClientUnsafe(Electron2D.X509Certificate trustedChain = null)`

Creates an explicit client configuration without expected-name rejection.

trustedChain: Optional required trust chain; null disables certificate verification.

Returns: A caller-owned unsafe test configuration.

<a id="member-68297767982c"></a>
### GetCommonNameOverride

`public System.String GetCommonNameOverride()`

Gets the expected-name override.

Returns: Empty by default.

<a id="member-43dc393f80b1"></a>
### GetOwnCertificate

`public Electron2D.X509Certificate GetOwnCertificate()`

Gets the borrowed server certificate chain.

Returns: Null for clients.

<a id="member-b19a2f6d4128"></a>
### GetPrivateKey

`public Electron2D.CryptoKey GetPrivateKey()`

Gets the borrowed server private key.

Returns: Null for clients.

<a id="member-744de90823f7"></a>
### GetTrustedCAChain

`public Electron2D.X509Certificate GetTrustedCAChain()`

Gets the borrowed custom trust chain.

Returns: Null when system trust is selected.

<a id="member-4861093fb558"></a>
### IsServer

`public System.Boolean IsServer()`

Reports whether this configuration accepts incoming TLS sessions.

Returns: True for Server.

<a id="member-788a60205a1d"></a>
### IsUnsafeClient

`public System.Boolean IsUnsafeClient()`

Reports whether expected-name rejection is disabled.

Returns: True for ClientUnsafe.

<a id="member-4717cffd475c"></a>
### Server

`public static Electron2D.TLSOptions Server(Electron2D.CryptoKey key, Electron2D.X509Certificate certificate)`

Creates a server configuration.

key: A borrowed private key.

certificate: A borrowed leaf certificate followed by intermediate certificates.

Returns: A caller-owned configuration.

## Verification and limits

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) exercises the TLS/security resource contract, native Linux OpenSSL 3 streams, independent .NET SslStream interoperability and warmed managed-allocation boundaries. Other-platform TLS backend/packaging, system-wide trust variation, native OpenSSL allocation totals, routed throughput and owner acceptance remain separate. See [coverage](../coverage/classes/TLSOptions.md).
