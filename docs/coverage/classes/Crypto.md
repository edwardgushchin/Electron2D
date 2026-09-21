# Crypto API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/Crypto.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Crypto`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method constant_time_compare(PackedByteArray trusted, PackedByteArray received) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method decrypt(CryptoKey key, PackedByteArray ciphertext) -> PackedByteArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method encrypt(CryptoKey key, PackedByteArray plaintext) -> PackedByteArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method generate_random_bytes(int size) -> PackedByteArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method generate_rsa(int size) -> CryptoKey`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method generate_self_signed_certificate(CryptoKey key, String issuer_name = "CN=myserver,O=myorganisation,C=IT", String not_before = "20140101000000", String not_after = "20340101000000") -> X509Certificate`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method hmac_digest(int hash_type [HashingContext.HashType], PackedByteArray key, PackedByteArray msg) -> PackedByteArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method sign(int hash_type [HashingContext.HashType], PackedByteArray hash, CryptoKey key) -> PackedByteArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
| [`method verify(int hash_type [HashingContext.HashType], PackedByteArray hash, PackedByteArray signature, CryptoKey key) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Crypto.xml) | — | Blocked | Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). |
