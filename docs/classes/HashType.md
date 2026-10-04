# HashType

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.HashType`. **Source:** [HashingContext.cs](../../src/Core/Networking/HashingContext.cs). **Component:** [Crypto](../components/crypto.md).

## Description

Selects the digest contract shared by hashing, signatures and HMAC operations.

## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `MD5` | 0 | MD5 produces 16 bytes; provided for existing digest contracts. |
| `SHA1` | 1 | SHA-1 produces 20 bytes. |
| `SHA256` | 2 | SHA-256 produces 32 bytes. |

## Enumeration Descriptions

<a id="member-e4fd175b89fe"></a>
### MD5

`MD5 = 0`

MD5 produces 16 bytes; provided for existing digest contracts.

<a id="member-a672475dc6af"></a>
### SHA1

`SHA1 = 1`

SHA-1 produces 20 bytes.

<a id="member-055c4f63bb93"></a>
### SHA256

`SHA256 = 2`

SHA-256 produces 32 bytes.

## Validation

CryptoTests checks algorithm selection and invalid selectors. HashType is shared by digest/signature/HMAC consumers; HMAC rejects MD5. AESMode.Max bounds its domain and cannot select an operation.
