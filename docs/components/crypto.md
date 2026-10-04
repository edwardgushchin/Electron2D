# Cryptographic operations and prepared contexts

Last updated: 2026-10-04

## Surface and integration

[Crypto](../classes/Crypto.md) supplies CSPRNG bytes, RSA key generation, self-signed certificates, precomputed-digest signing/verification, RSA encryption/decryption, HMAC and fixed-time content comparison. [HashingContext](../classes/HashingContext.md), [HMACContext](../classes/HMACContext.md) and [AESContext](../classes/AESContext.md) provide incremental processing. These ElectronObject owners require their constructing thread for calls/disposal. [HashType](../classes/HashType.md) is shared by digest/signature/HMAC APIs; [AESMode](../classes/AESMode.md) is a separate domain, following ADR 0051. Implementation is under `src/Core/Networking/`, with the platform BCL cryptography provider kept private.

Generated [CryptoKey](../classes/CryptoKey.md) and [X509Certificate](../classes/X509Certificate.md) resources use their existing `.key`/`.crt` load/save, copied duplication and typed ResourceLoader paths. TLSOptions.Server borrows these resources; StreamPeerTLS retains them during active sessions. Each asymmetric operation imports an independent algorithm under the key resource lock, then releases it after use. This permits signing during TLS retention without weakening replacement/disposal guards. General ResourceSaver/scene-file/editor workflows remain separate.

## Digest, HMAC and lifecycle

HashType selects MD5 (16 bytes), SHA1 (20) or SHA256 (32). Hashing Start rejects an active context; Update requires a nonempty chunk. Finish without Update produces the empty-message digest and closes the computation. Caller-span Finish validates capacity before changing state; an undersized destination preserves all accumulated input. Finish on idle fails; a completed context can Start again. Dispose releases an unfinished computation.

HMAC supports SHA1/SHA256, requires a nonempty copied key and rejects empty Update chunks. MD5 is explicitly unsupported. Start followed immediately by Finish computes HMAC for an empty message, but Crypto.HMACDigest requires a nonempty message because it performs one Update. Source Error codes/empty failure arrays become typed C# exceptions. Invalid hash selectors and precomputed signature widths reject before backend operations.

## Raw AES blocks

AES accepts exactly 16-byte or 32-byte keys and ECB/CBC encryption/decryption. CBC requires a 16-byte IV; ECB ignores the IV argument. Max is a domain bound and cannot select an operation. Start prepares/copies the native-backed transform, key schedule and IV. Update requires a multiple of 16 bytes, with empty input valid; it adds no padding or authentication. Exact in-place operation works for all modes, including CBC decryption. Partial overlap, unaligned input and undersized output reject before changing chaining state. Native transformation failure closes the stream; successful updates write exactly the input length.

GetIVState is valid only during active CBC. Encryption tracks the last ciphertext output; decryption tracks the last ciphertext input. Span queries borrow caller storage; arrays are owned snapshots. Finish is safe while idle and releases provider state while clearing engine key preparation, block and IV buffers. A fresh Start must precede reuse. Applications own framing, padding, IV persistence and authentication. FileAccess/ConfigFile authenticated encryption remains its existing independent AES-GCM contract.

## Asymmetric operations and identity generation

GenerateRSA takes the size in bits supported by the current BCL provider and returns canonical private key material; temporary exported DER bytes are cleared. Encrypt accepts public/private RSA keys and PKCS#1 v1.5 plaintext up to modulus bytes minus eleven; Decrypt requires a private RSA key and exactly one modulus-sized ciphertext. Empty plaintext is valid. EC encryption/decryption is unsupported. Native crypto errors and invalid padding use CryptographicException.

Sign/Verify operate on an already computed exact-width MD5/SHA1/SHA256 digest. RSA uses PKCS#1 v1.5; EC signatures use ASN.1 DER, not fixed-width concatenation. Sign requires a private key; Verify accepts public/private keys and returns false for malformed/mismatched signatures. A missing/disposed key or wrong digest remains a typed argument/lifetime error.

GenerateSelfSignedCertificate uses an RSA/EC private key, SHA256 and X509 v3. Subject equals issuer. The X.500 name requires nonempty CN and O plus two ASCII letters for C. Defaults are `CN=myserver,O=myorganisation,C=IT`, UTC `20140101000000` and `20340101000000`; dates must have exactly fourteen digits and end later than start. The certificate is a CA with path length zero, and uses a positive serial generated from twenty random bytes. It contains no private key or SAN extension. Existing TLS hostname verification can use its CN; applications requiring additional extensions supply imported certificates. Invalid names/dates reject rather than partially constructing identity resources.

GenerateRandomBytes uses the platform CSPRNG and permits zero length; negative array sizes reject. ConstantTimeCompare has no content-dependent early exit for equal-length spans; length differences reject immediately and two empty spans compare equal. Lengths remain visible.

## Preparation and verification boundaries

Constructors/Start prepare algorithm state and AES block storage. Prepared hash/HMAC Update and span Finish, AES span Update/GetIVState, random-span fill and content comparison do not allocate engine buffers. Snapshot arrays, HMACDigest, resource import/export, key/certificate generation and asymmetric operations are explicitly allocating setup/loading work. Native provider allocation totals and policy remain external; no new package or vendored source is introduced.

[CryptoTests](../../tests/Electron2D.Tests/CryptoTests.cs) checks known empty/abc digests, [RFC HMAC test vectors](https://www.rfc-editor.org/rfc/rfc4231), [NIST SP 800-38A AES vectors](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38a.pdf), split chunks, in-place CBC, capacity preservation, invalid keys/selectors/empty chunks, lifecycle/owner failure, random sizes and 64 warmed prepared operation intervals with zero managed bytes. Independent BCL RSA/ECDSA verifies signature/encryption wire in both directions, including RSA maximum plaintext and malformed signatures. Generated resources save/reload and create a real Linux native StreamPeerTLS server; an independent SslStream client validates its generated certificate through custom trust and hostname checks. Signing while TLS retains the key preserves resource lifetime guards.

These tests establish the exercised Linux provider and native TLS integration. Foreign hosts/provider policy, external native allocation, routed throughput, rendered/editor/agent workflow and human acceptance require their own gates. See [platform verification](../platform-verification.md) and [ADR 0094](../decisions/networking.md#adr-0094).
