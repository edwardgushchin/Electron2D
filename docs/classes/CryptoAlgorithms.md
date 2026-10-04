# CryptoAlgorithms

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [HashingContext.cs](../../src/Core/Networking/HashingContext.cs). **Component:** [Crypto](../components/crypto.md).

Internal static typed selector and digest-width validator shared by hashing, HMAC and asymmetric signatures. MD5/SHA1/SHA256 map to BCL identities; invalid enum values reject. Precomputed signatures require exactly 16/20/32 bytes. It owns no mutable state or handles.

CryptoTests exercises public callers, lifecycle and prepared allocation boundaries. Provider-native allocations and foreign host policy remain separate.
