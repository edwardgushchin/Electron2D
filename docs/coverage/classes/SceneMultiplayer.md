# SceneMultiplayer API coverage

Last updated: 2026-09-22

Godot source: [modules/multiplayer/doc_classes/SceneMultiplayer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [MultiplayerAPI](MultiplayerAPI.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SceneMultiplayer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method clear() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method complete_auth(int id) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method disconnect_peer(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_authenticating_peers() -> PackedInt32Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method send_auth(int id, PackedByteArray data) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method send_bytes(PackedByteArray bytes, int id = 0, int mode [MultiplayerPeer.TransferMode] = 2, int channel = 0) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property bool allow_object_decoding = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property Callable auth_callback = Callable()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property float auth_timeout = 3.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property int max_delta_packet_size = 65535`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property int max_sync_packet_size = 1350`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property bool refuse_new_connections = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property NodePath root_path = NodePath("")`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property bool server_relay = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal peer_authenticating(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal peer_authentication_failed(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal peer_packet(int id, PackedByteArray packet) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/SceneMultiplayer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
