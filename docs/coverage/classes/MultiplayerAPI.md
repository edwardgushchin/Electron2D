# MultiplayerAPI API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/MultiplayerAPI.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class MultiplayerAPI`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum RPCMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Trigger: first typed networking and multiplayer slice. |
| [`enum_value RPC_MODE_ANY_PEER [RPCMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Trigger: first typed networking and multiplayer slice. |
| [`enum_value RPC_MODE_AUTHORITY [RPCMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Trigger: first typed networking and multiplayer slice. |
| [`enum_value RPC_MODE_DISABLED [RPCMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Trigger: first typed networking and multiplayer slice. |
| [`method create_default_interface() -> MultiplayerAPI`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_default_interface() -> StringName`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_peers() -> PackedInt32Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_remote_sender_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_unique_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method has_multiplayer_peer() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Trigger: first typed networking and multiplayer slice. |
| [`method is_server() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method object_configuration_add(Object object, Variant configuration) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method object_configuration_remove(Object object, Variant configuration) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method poll() -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method rpc(int peer, Object object, StringName method, Array arguments = []) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Trigger: first typed networking and multiplayer slice. |
| [`method set_default_interface(StringName interface_name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property MultiplayerPeer multiplayer_peer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Trigger: first typed networking and multiplayer slice. |
| [`signal connected_to_server() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal connection_failed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal peer_connected(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal peer_disconnected(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal server_disconnected() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerAPI.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
