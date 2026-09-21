# HTTPRequest API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/HTTPRequest.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node](Node.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class HTTPRequest`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum Result`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_BODY_DECOMPRESS_FAILED [Result] = 8`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_BODY_SIZE_LIMIT_EXCEEDED [Result] = 7`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_CANT_CONNECT [Result] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_CANT_RESOLVE [Result] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_CHUNKED_BODY_SIZE_MISMATCH [Result] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_CONNECTION_ERROR [Result] = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_DOWNLOAD_FILE_CANT_OPEN [Result] = 10`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_DOWNLOAD_FILE_WRITE_ERROR [Result] = 11`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_NO_RESPONSE [Result] = 6`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_REDIRECT_LIMIT_REACHED [Result] = 12`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_REQUEST_FAILED [Result] = 9`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_SUCCESS [Result] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_TIMEOUT [Result] = 13`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`enum_value RESULT_TLS_HANDSHAKE_ERROR [Result] = 5`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method cancel_request() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_body_size() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_downloaded_bytes() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method get_http_client_status() -> int [HTTPClient.Status]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method request(String url, PackedStringArray custom_headers = PackedStringArray(), int method [HTTPClient.Method] = 0, String request_data = "") -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method request_raw(String url, PackedStringArray custom_headers = PackedStringArray(), int method [HTTPClient.Method] = 0, PackedByteArray request_data_raw = PackedByteArray()) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method set_http_proxy(String host, int port) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method set_https_proxy(String host, int port) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`method set_tls_options(TLSOptions client_options) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property bool accept_gzip = true`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property int body_size_limit = -1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property int download_chunk_size = 65536`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property String download_file = ""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property int max_redirects = 8`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property float timeout = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`property bool use_threads = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
| [`signal request_completed(int result, int response_code, PackedStringArray headers, PackedByteArray body) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HTTPRequest.xml) | — | Blocked | Networking: trigger is the first networking and multiplayer slice. |
