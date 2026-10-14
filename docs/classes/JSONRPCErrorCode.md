# JSONRPCErrorCode

Last updated: 2026-10-14

## Overview

The shared public enum identifies JSON-RPC 2.0 protocol errors used by [JSONRPC](JSONRPC.md). Application errors use the integer code accepted by `MakeResponseError`; they do not add enum values.

## Member reference

| Value | Code | Meaning |
| --- | ---: | --- |
| `ParseError` | -32700 | Invalid JSON wire text. |
| `InvalidRequest` | -32600 | Invalid envelope, ID, version or root/nested batch. |
| `MethodNotFound` | -32601 | No registered case-sensitive method. |
| `InvalidParams` | -32602 | Scalar/incorrect parameters, schema failure or handler argument rejection. |
| `InternalError` | -32603 | Other handler or result-conversion failure. |
