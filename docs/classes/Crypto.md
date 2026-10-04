# Crypto

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.Crypto`. **Inherits:** [ElectronObject](ElectronObject.md). **Source:** [Crypto.cs](../../src/Core/Networking/Crypto.cs).

## Description

Generates keys/certificates and performs RSA, digest-signature, HMAC and secure-random operations.

Operations/disposal require the constructing thread. Key operations import an independent BCL key snapshot under the resource lock and release it after the call. Generated resources are caller-owned. Key generation, import, asymmetric operations and copied results are cold work; prepared contexts handle streams.

[Cryptographic operations](../components/crypto.md) and [ADR 0094](../decisions/networking.md#adr-0094) define algorithms, typed validation, owner/lifetime and preparation boundaries. Shared [HashType](HashType.md) and separate [AESMode](AESMode.md) preserve public selector domains. No backend handles are exposed.

## Example

Public API excerpt. CryptoTests exercises generated identities in an actual native TLS exchange; constructing TLSOptions alone does not establish a connection. AES is raw block processing without padding or authentication.

```csharp
using var crypto = new Crypto();
using var key = crypto.GenerateRSA(2048);
using var identity = crypto.GenerateSelfSignedCertificate(key, "CN=localhost,O=Electron2D,C=RU");
using var hash = new HashingContext();
hash.Start(HashType.SHA256);
hash.Update("game payload"u8);
byte[] digest = hash.Finish();
byte[] signature = crypto.Sign(HashType.SHA256, digest, key);
bool valid = crypto.Verify(HashType.SHA256, digest, signature, key);
using var options = TLSOptions.Server(key, identity); // Supply to an accepted StreamPeerTLS.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public Crypto()` | Creates a cryptographic operation provider. |

## Constructor Descriptions

<a id="member-f76a152fb747"></a>
### .ctor

`public Crypto()`

Creates a cryptographic operation provider.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean ConstantTimeCompare(System.ReadOnlySpan<System.Byte> trusted, System.ReadOnlySpan<System.Byte> received)` | Compares equal-length byte strings without content-dependent early exit. |
| `public System.Byte[] Decrypt(Electron2D.CryptoKey key, System.ReadOnlySpan<System.Byte> ciphertext)` | Decrypts one modulus-sized RSA ciphertext using a private key and PKCS#1 v1.5 padding. |
| `public System.Byte[] Encrypt(Electron2D.CryptoKey key, System.ReadOnlySpan<System.Byte> plaintext)` | Encrypts one RSA plaintext using PKCS#1 v1.5 padding. |
| `public Electron2D.CryptoKey GenerateRSA(System.Int32 size)` | Generates a private RSA key resource. |
| `public System.Byte[] GenerateRandomBytes(System.Int32 size)` | Generates copied cryptographically secure random bytes. |
| `public System.Void GenerateRandomBytes(System.Span<System.Byte> destination)` | Fills caller storage with cryptographically secure random bytes. |
| `public Electron2D.X509Certificate GenerateSelfSignedCertificate(Electron2D.CryptoKey key, System.String issuerName = "CN=myserver,O=myorganisation,C=IT", System.String notBefore = "20140101000000", System.String notAfter = "20340101000000")` | Generates a SHA-256 signed X.509 v3 self-signed CA certificate with path length zero. |
| `public System.Byte[] HMACDigest(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> key, System.ReadOnlySpan<System.Byte> message)` | Computes a complete HMAC-SHA1/HMAC-SHA256 message digest. |
| `public System.Byte[] Sign(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> hash, Electron2D.CryptoKey key)` | Signs a precomputed MD5/SHA1/SHA256 digest. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| `public System.Boolean Verify(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> hash, System.ReadOnlySpan<System.Byte> signature, Electron2D.CryptoKey key)` | Verifies a precomputed digest and RSA PKCS#1/ECDSA DER signature. |

## Method Descriptions

<a id="member-5914385e2e6b"></a>
### ConstantTimeCompare

`public System.Boolean ConstantTimeCompare(System.ReadOnlySpan<System.Byte> trusted, System.ReadOnlySpan<System.Byte> received)`

Compares equal-length byte strings without content-dependent early exit.

trusted: Trusted bytes.

received: Received bytes.

Returns: True for equal bytes, including two empty spans; false for different lengths or content.

Remarks: Lengths are public to the comparison; different lengths reject before comparing content.

<a id="member-3b320410156b"></a>
### Decrypt

`public System.Byte[] Decrypt(Electron2D.CryptoKey key, System.ReadOnlySpan<System.Byte> ciphertext)`

Decrypts one modulus-sized RSA ciphertext using a private key and PKCS#1 v1.5 padding.

key: Loaded private RSA key.

ciphertext: One RSA ciphertext.

Returns: Caller-owned plaintext bytes.

System.Security.Cryptography.CryptographicException: Key role, ciphertext length or padding is invalid.

<a id="member-45fdef858eaf"></a>
### Encrypt

`public System.Byte[] Encrypt(Electron2D.CryptoKey key, System.ReadOnlySpan<System.Byte> plaintext)`

Encrypts one RSA plaintext using PKCS#1 v1.5 padding.

key: Loaded public or private RSA key.

plaintext: At most modulus byte length minus eleven.

Returns: One modulus-sized ciphertext.

System.NotSupportedException: An EC key is supplied.

System.Security.Cryptography.CryptographicException: The key or plaintext size is invalid.

<a id="member-491c5c2afe9f"></a>
### GenerateRSA

`public Electron2D.CryptoKey GenerateRSA(System.Int32 size)`

Generates a private RSA key resource.

size: Key size in bits, subject to the current BCL provider's supported sizes.

Returns: A caller-owned canonical private key.

System.Security.Cryptography.CryptographicException: Key size or generation is unsupported.

<a id="member-bc6e47a47e01"></a>
### GenerateRandomBytes

`public System.Byte[] GenerateRandomBytes(System.Int32 size)`

Generates copied cryptographically secure random bytes.

size: Nonnegative requested byte count; zero is valid.

Returns: A caller-owned array of the requested length.

System.ArgumentOutOfRangeException: Size is negative.

<a id="member-1c993caf0f85"></a>
### GenerateRandomBytes

`public System.Void GenerateRandomBytes(System.Span<System.Byte> destination)`

Fills caller storage with cryptographically secure random bytes.

destination: Borrowed output storage, including an empty span.

<a id="member-d97675b16ff3"></a>
### GenerateSelfSignedCertificate

`public Electron2D.X509Certificate GenerateSelfSignedCertificate(Electron2D.CryptoKey key, System.String issuerName = "CN=myserver,O=myorganisation,C=IT", System.String notBefore = "20140101000000", System.String notAfter = "20340101000000")`

Generates a SHA-256 signed X.509 v3 self-signed CA certificate with path length zero.

key: Loaded RSA/EC private key, borrowed during the call.

issuerName: Subject/issuer X.500 name containing nonempty CN, O and two-letter C.

notBefore: First valid UTC instant as yyyyMMddHHmmss.

notAfter: Last valid UTC instant in the same format, strictly later.

Returns: A caller-owned certificate resource without private-key material.

System.ArgumentException: Name or dates are invalid.

System.Security.Cryptography.CryptographicException: A usable private key is unavailable.

<a id="member-acd7f7eba879"></a>
### HMACDigest

`public System.Byte[] HMACDigest(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> key, System.ReadOnlySpan<System.Byte> message)`

Computes a complete HMAC-SHA1/HMAC-SHA256 message digest.

hashType: SHA1 or SHA256.

key: Borrowed nonempty authentication key.

message: Borrowed nonempty message bytes.

Returns: Caller-owned authentication bytes.

System.NotSupportedException: MD5 is selected.

System.ArgumentException: Key or message is empty.

<a id="member-633f8fa0ff3c"></a>
### Sign

`public System.Byte[] Sign(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> hash, Electron2D.CryptoKey key)`

Signs a precomputed MD5/SHA1/SHA256 digest.

hashType: Digest identity.

hash: Exactly 16, 20 or 32 bytes.

key: Loaded private RSA/EC key.

Returns: RSA PKCS#1 v1.5 or ECDSA DER signature bytes.

System.ArgumentException: Digest length is invalid.

System.Security.Cryptography.CryptographicException: The key or signing operation is invalid.

<a id="member-c430018ebde1"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

<a id="member-1e655d4a30c7"></a>
### Verify

`public System.Boolean Verify(Electron2D.HashType hashType, System.ReadOnlySpan<System.Byte> hash, System.ReadOnlySpan<System.Byte> signature, Electron2D.CryptoKey key)`

Verifies a precomputed digest and RSA PKCS#1/ECDSA DER signature.

hashType: Digest identity.

hash: Exactly the selected digest length.

signature: Untrusted signature bytes.

key: Loaded public or private RSA/EC key.

Returns: False for a mismatched or malformed signature.

System.ArgumentException: The supplied digest has the wrong length.

## Verification and limits

[CryptoTests](../../tests/Electron2D.Tests/CryptoTests.cs) verifies known digest/RFC HMAC/NIST AES vectors, split/in-place/invalid input and lifecycle, RSA/EC interoperability, generated resource save/load and Linux TLS trust using an independent SslStream client. Prepared caller-span intervals measure managed allocations only. Provider-native costs, asymmetric/setup/snapshot work, foreign platforms, routed traffic and human acceptance remain separate. See [the component](../components/crypto.md) for exact supported widths and empty-input contracts.
