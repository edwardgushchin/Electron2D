# MultiplayerPeer API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/MultiplayerPeer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [PacketPeer](PacketPeer.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class MultiplayerPeer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`constant TARGET_PEER_BROADCAST = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`constant TARGET_PEER_SERVER = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum ConnectionStatus`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum TransferMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value CONNECTION_CONNECTED [ConnectionStatus] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value CONNECTION_CONNECTING [ConnectionStatus] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value CONNECTION_DISCONNECTED [ConnectionStatus] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value TRANSFER_MODE_RELIABLE [TransferMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value TRANSFER_MODE_UNRELIABLE [TransferMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value TRANSFER_MODE_UNRELIABLE_ORDERED [TransferMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method close() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method disconnect_peer(int peer, bool force = false) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method generate_unique_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_connection_status() -> int [MultiplayerPeer.ConnectionStatus]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_packet_channel() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_packet_mode() -> int [MultiplayerPeer.TransferMode]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_packet_peer() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_unique_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method is_server_relay_supported() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method poll() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method set_target_peer(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property bool refuse_new_connections = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property int transfer_channel = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property int transfer_mode [MultiplayerPeer.TransferMode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal peer_connected(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal peer_disconnected(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MultiplayerPeer.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
