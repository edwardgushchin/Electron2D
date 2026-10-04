# HTTPResponseCode

Last updated: 2026-10-04

**Source:** [HTTPResponseCode.cs](../../src/Core/Networking/HTTPResponseCode.cs). **Component:** [HTTP](../components/http.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `Accepted` | 202 | HTTP 202 Accepted. |
| `AlreadyReported` | 208 | HTTP 208 Already Reported. |
| `BadGateway` | 502 | HTTP 502 Bad Gateway. |
| `BadRequest` | 400 | HTTP 400 Bad Request. |
| `Conflict` | 409 | HTTP 409 Conflict. |
| `Continue` | 100 | HTTP 100 Continue. |
| `Created` | 201 | HTTP 201 Created. |
| `ExpectationFailed` | 417 | HTTP 417 Expectation Failed. |
| `FailedDependency` | 424 | HTTP 424 Failed Dependency. |
| `Forbidden` | 403 | HTTP 403 Forbidden. |
| `Found` | 302 | HTTP 302 Found. |
| `GatewayTimeout` | 504 | HTTP 504 Gateway Timeout. |
| `Gone` | 410 | HTTP 410 Gone. |
| `HTTPVersionNotSupported` | 505 | HTTP 505 HTTPVersion Not Supported. |
| `ImATeapot` | 418 | HTTP 418 Im ATeapot. |
| `ImUsed` | 226 | HTTP 226 Im Used. |
| `InsufficientStorage` | 507 | HTTP 507 Insufficient Storage. |
| `InternalServerError` | 500 | HTTP 500 Internal Server Error. |
| `LengthRequired` | 411 | HTTP 411 Length Required. |
| `Locked` | 423 | HTTP 423 Locked. |
| `LoopDetected` | 508 | HTTP 508 Loop Detected. |
| `MethodNotAllowed` | 405 | HTTP 405 Method Not Allowed. |
| `MisdirectedRequest` | 421 | HTTP 421 Misdirected Request. |
| `MovedPermanently` | 301 | HTTP 301 Moved Permanently. |
| `MultiStatus` | 207 | HTTP 207 Multi Status. |
| `MultipleChoices` | 300 | HTTP 300 Multiple Choices. |
| `NetworkAuthRequired` | 511 | HTTP 511 Network Auth Required. |
| `NoContent` | 204 | HTTP 204 No Content. |
| `NonAuthoritativeInformation` | 203 | HTTP 203 Non Authoritative Information. |
| `NotAcceptable` | 406 | HTTP 406 Not Acceptable. |
| `NotExtended` | 510 | HTTP 510 Not Extended. |
| `NotFound` | 404 | HTTP 404 Not Found. |
| `NotImplemented` | 501 | HTTP 501 Not Implemented. |
| `NotModified` | 304 | HTTP 304 Not Modified. |
| `OK` | 200 | HTTP 200 OK. |
| `PartialContent` | 206 | HTTP 206 Partial Content. |
| `PaymentRequired` | 402 | HTTP 402 Payment Required. |
| `PermanentRedirect` | 308 | HTTP 308 Permanent Redirect. |
| `PreconditionFailed` | 412 | HTTP 412 Precondition Failed. |
| `PreconditionRequired` | 428 | HTTP 428 Precondition Required. |
| `Processing` | 102 | HTTP 102 Processing. |
| `ProxyAuthenticationRequired` | 407 | HTTP 407 Proxy Authentication Required. |
| `RequestEntityTooLarge` | 413 | HTTP 413 Request Entity Too Large. |
| `RequestHeaderFieldsTooLarge` | 431 | HTTP 431 Request Header Fields Too Large. |
| `RequestTimeout` | 408 | HTTP 408 Request Timeout. |
| `RequestURITooLong` | 414 | HTTP 414 Request URIToo Long. |
| `RequestedRangeNotSatisfiable` | 416 | HTTP 416 Requested Range Not Satisfiable. |
| `ResetContent` | 205 | HTTP 205 Reset Content. |
| `SeeOther` | 303 | HTTP 303 See Other. |
| `ServiceUnavailable` | 503 | HTTP 503 Service Unavailable. |
| `SwitchProxy` | 306 | HTTP 306 Switch Proxy. |
| `SwitchingProtocols` | 101 | HTTP 101 Switching Protocols. |
| `TemporaryRedirect` | 307 | HTTP 307 Temporary Redirect. |
| `TooManyRequests` | 429 | HTTP 429 Too Many Requests. |
| `Unauthorized` | 401 | HTTP 401 Unauthorized. |
| `UnavailableForLegalReasons` | 451 | HTTP 451 Unavailable For Legal Reasons. |
| `UnprocessableEntity` | 422 | HTTP 422 Unprocessable Entity. |
| `UnsupportedMediaType` | 415 | HTTP 415 Unsupported Media Type. |
| `UpgradeRequired` | 426 | HTTP 426 Upgrade Required. |
| `UseProxy` | 305 | HTTP 305 Use Proxy. |
| `VariantAlsoNegotiates` | 506 | HTTP 506 Variant Also Negotiates. |

HTTPMethod.Max is a sentinel and rejects dispatch. HTTPResponseCode preserves an unknown valid response number through its underlying integer; zero means no response. Status and completion-result domains are distinct and follow ADR 0051.
