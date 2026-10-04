# AESMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.AESMode`. **Source:** [AESContext.cs](../../src/Core/Networking/AESContext.cs). **Component:** [Crypto](../components/crypto.md).

## Description

Selects a raw AES encryption/decryption and chaining contract.

## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `CBCDecrypt` | 3 | Decrypts chained 16-byte blocks with CBC and a caller IV. |
| `CBCEncrypt` | 2 | Encrypts chained 16-byte blocks with CBC and a caller IV. |
| `ECBDecrypt` | 1 | Decrypts independent 16-byte blocks with ECB. |
| `ECBEncrypt` | 0 | Encrypts independent 16-byte blocks with ECB. |
| `Max` | 4 | Bounds the domain; cannot select an operation. |

## Enumeration Descriptions

<a id="member-76cba58efdd2"></a>
### CBCDecrypt

`CBCDecrypt = 3`

Decrypts chained 16-byte blocks with CBC and a caller IV.

<a id="member-907f5bfa00a8"></a>
### CBCEncrypt

`CBCEncrypt = 2`

Encrypts chained 16-byte blocks with CBC and a caller IV.

<a id="member-a0011e5882c3"></a>
### ECBDecrypt

`ECBDecrypt = 1`

Decrypts independent 16-byte blocks with ECB.

<a id="member-9ea475457007"></a>
### ECBEncrypt

`ECBEncrypt = 0`

Encrypts independent 16-byte blocks with ECB.

<a id="member-e442022ced96"></a>
### Max

`Max = 4`

Bounds the domain; cannot select an operation.

## Validation

CryptoTests checks algorithm selection and invalid selectors. HashType is shared by digest/signature/HMAC consumers; HMAC rejects MD5. AESMode.Max bounds its domain and cannot select an operation.
