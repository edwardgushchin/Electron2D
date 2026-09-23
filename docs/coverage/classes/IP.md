# IP API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/IP.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class IP`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`constant RESOLVER_INVALID_ID = -1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`constant RESOLVER_MAX_QUERIES = 256`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum ResolverStatus`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum Type`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value RESOLVER_STATUS_DONE [ResolverStatus] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value RESOLVER_STATUS_ERROR [ResolverStatus] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value RESOLVER_STATUS_NONE [ResolverStatus] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value RESOLVER_STATUS_WAITING [ResolverStatus] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value TYPE_ANY [Type] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value TYPE_IPV4 [Type] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value TYPE_IPV6 [Type] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value TYPE_NONE [Type] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method clear_cache(String hostname = "") -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method erase_resolve_item(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method get_local_addresses() -> PackedStringArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method get_local_interfaces() -> Dictionary[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method get_resolve_item_address(int id) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method get_resolve_item_addresses(int id) -> Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method get_resolve_item_status(int id) -> int [IP.ResolverStatus]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method resolve_hostname(String host, int ip_type [IP.Type] = 3) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method resolve_hostname_addresses(String host, int ip_type [IP.Type] = 3) -> PackedStringArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method resolve_hostname_queue_item(String host, int ip_type [IP.Type] = 3) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/IP.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
