# TLSOptions API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/TLSOptions.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TLSOptions`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method client(X509Certificate trusted_chain = null, String common_name_override = "") -> TLSOptions`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method client_unsafe(X509Certificate trusted_chain = null) -> TLSOptions`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method get_common_name_override() -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method get_own_certificate() -> X509Certificate`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method get_private_key() -> CryptoKey`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method get_trusted_ca_chain() -> X509Certificate`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method is_server() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method is_unsafe_client() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
| [`method server(CryptoKey key, X509Certificate certificate) -> TLSOptions`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TLSOptions.xml) | — | Blocked | ADR 0094: Requires typed certificate/key/trust resources and a shared TLS configuration contract consumed by StreamPeerTLS and DTLS. |
