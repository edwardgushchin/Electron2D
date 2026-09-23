# JSONRPC API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/JSONRPC.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class JSONRPC`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum ErrorCode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value INTERNAL_ERROR [ErrorCode] = -32603`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value INVALID_PARAMS [ErrorCode] = -32602`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value INVALID_REQUEST [ErrorCode] = -32600`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value METHOD_NOT_FOUND [ErrorCode] = -32601`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`enum_value PARSE_ERROR [ErrorCode] = -32700`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method make_notification(String method, Variant params) -> Dictionary`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method make_request(String method, Variant params, Variant id) -> Dictionary`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method make_response(Variant result, Variant id) -> Dictionary`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method make_response_error(int code, String message, Variant id = null) -> Dictionary`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method process_action(Variant action, bool recurse = false) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method process_string(String action) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
| [`method set_method(String name, Callable callback) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/JSONRPC.xml) | — | Blocked | Trigger: first typed networking, address-resolution and RPC slice. |
