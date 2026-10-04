namespace Electron2D;

/// <summary>Identifies standard HTTP response numbers; unknown valid numbers remain representable through the underlying integer. Zero means no response.</summary>
public enum HTTPResponseCode
{
    /// <summary>HTTP 202 Accepted.</summary>
    Accepted = 202,
    /// <summary>HTTP 208 Already Reported.</summary>
    AlreadyReported = 208,
    /// <summary>HTTP 502 Bad Gateway.</summary>
    BadGateway = 502,
    /// <summary>HTTP 400 Bad Request.</summary>
    BadRequest = 400,
    /// <summary>HTTP 409 Conflict.</summary>
    Conflict = 409,
    /// <summary>HTTP 100 Continue.</summary>
    Continue = 100,
    /// <summary>HTTP 201 Created.</summary>
    Created = 201,
    /// <summary>HTTP 417 Expectation Failed.</summary>
    ExpectationFailed = 417,
    /// <summary>HTTP 424 Failed Dependency.</summary>
    FailedDependency = 424,
    /// <summary>HTTP 403 Forbidden.</summary>
    Forbidden = 403,
    /// <summary>HTTP 302 Found.</summary>
    Found = 302,
    /// <summary>HTTP 504 Gateway Timeout.</summary>
    GatewayTimeout = 504,
    /// <summary>HTTP 410 Gone.</summary>
    Gone = 410,
    /// <summary>HTTP 505 HTTPVersion Not Supported.</summary>
    HTTPVersionNotSupported = 505,
    /// <summary>HTTP 418 Im ATeapot.</summary>
    ImATeapot = 418,
    /// <summary>HTTP 226 Im Used.</summary>
    ImUsed = 226,
    /// <summary>HTTP 507 Insufficient Storage.</summary>
    InsufficientStorage = 507,
    /// <summary>HTTP 500 Internal Server Error.</summary>
    InternalServerError = 500,
    /// <summary>HTTP 411 Length Required.</summary>
    LengthRequired = 411,
    /// <summary>HTTP 423 Locked.</summary>
    Locked = 423,
    /// <summary>HTTP 508 Loop Detected.</summary>
    LoopDetected = 508,
    /// <summary>HTTP 405 Method Not Allowed.</summary>
    MethodNotAllowed = 405,
    /// <summary>HTTP 421 Misdirected Request.</summary>
    MisdirectedRequest = 421,
    /// <summary>HTTP 301 Moved Permanently.</summary>
    MovedPermanently = 301,
    /// <summary>HTTP 300 Multiple Choices.</summary>
    MultipleChoices = 300,
    /// <summary>HTTP 207 Multi Status.</summary>
    MultiStatus = 207,
    /// <summary>HTTP 511 Network Auth Required.</summary>
    NetworkAuthRequired = 511,
    /// <summary>HTTP 203 Non Authoritative Information.</summary>
    NonAuthoritativeInformation = 203,
    /// <summary>HTTP 406 Not Acceptable.</summary>
    NotAcceptable = 406,
    /// <summary>HTTP 510 Not Extended.</summary>
    NotExtended = 510,
    /// <summary>HTTP 404 Not Found.</summary>
    NotFound = 404,
    /// <summary>HTTP 501 Not Implemented.</summary>
    NotImplemented = 501,
    /// <summary>HTTP 304 Not Modified.</summary>
    NotModified = 304,
    /// <summary>HTTP 204 No Content.</summary>
    NoContent = 204,
    /// <summary>HTTP 200 OK.</summary>
    OK = 200,
    /// <summary>HTTP 206 Partial Content.</summary>
    PartialContent = 206,
    /// <summary>HTTP 402 Payment Required.</summary>
    PaymentRequired = 402,
    /// <summary>HTTP 308 Permanent Redirect.</summary>
    PermanentRedirect = 308,
    /// <summary>HTTP 412 Precondition Failed.</summary>
    PreconditionFailed = 412,
    /// <summary>HTTP 428 Precondition Required.</summary>
    PreconditionRequired = 428,
    /// <summary>HTTP 102 Processing.</summary>
    Processing = 102,
    /// <summary>HTTP 407 Proxy Authentication Required.</summary>
    ProxyAuthenticationRequired = 407,
    /// <summary>HTTP 416 Requested Range Not Satisfiable.</summary>
    RequestedRangeNotSatisfiable = 416,
    /// <summary>HTTP 413 Request Entity Too Large.</summary>
    RequestEntityTooLarge = 413,
    /// <summary>HTTP 431 Request Header Fields Too Large.</summary>
    RequestHeaderFieldsTooLarge = 431,
    /// <summary>HTTP 408 Request Timeout.</summary>
    RequestTimeout = 408,
    /// <summary>HTTP 414 Request URIToo Long.</summary>
    RequestURITooLong = 414,
    /// <summary>HTTP 205 Reset Content.</summary>
    ResetContent = 205,
    /// <summary>HTTP 303 See Other.</summary>
    SeeOther = 303,
    /// <summary>HTTP 503 Service Unavailable.</summary>
    ServiceUnavailable = 503,
    /// <summary>HTTP 101 Switching Protocols.</summary>
    SwitchingProtocols = 101,
    /// <summary>HTTP 306 Switch Proxy.</summary>
    SwitchProxy = 306,
    /// <summary>HTTP 307 Temporary Redirect.</summary>
    TemporaryRedirect = 307,
    /// <summary>HTTP 429 Too Many Requests.</summary>
    TooManyRequests = 429,
    /// <summary>HTTP 401 Unauthorized.</summary>
    Unauthorized = 401,
    /// <summary>HTTP 451 Unavailable For Legal Reasons.</summary>
    UnavailableForLegalReasons = 451,
    /// <summary>HTTP 422 Unprocessable Entity.</summary>
    UnprocessableEntity = 422,
    /// <summary>HTTP 415 Unsupported Media Type.</summary>
    UnsupportedMediaType = 415,
    /// <summary>HTTP 426 Upgrade Required.</summary>
    UpgradeRequired = 426,
    /// <summary>HTTP 305 Use Proxy.</summary>
    UseProxy = 305,
    /// <summary>HTTP 506 Variant Also Negotiates.</summary>
    VariantAlsoNegotiates = 506,
}
