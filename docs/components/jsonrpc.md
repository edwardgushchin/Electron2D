# JSON-RPC documents and typed dispatch

Last updated: 2026-10-14

## Responsibility and structure

[JSONRPC](../classes/JSONRPC.md) builds and dispatches JSON-RPC 2.0 requests, notifications, success/error responses and ordered batches. `src/Core/Networking/JSONRPC.cs` owns parsing, registrations and dispatch; `JSONRPC.API.cs` owns makers; [JSONRPCErrorCode](../classes/JSONRPCErrorCode.md) owns the five standard error identities. The component uses core object lifetime, the existing JSON document resource/native schemas and .NET JSON parsing. It adds no package, listener or assembly. [ADR 0094](../decisions/networking.md#adr-0094) and [ADR 0048](../decisions/core-data-io.md#adr-0048) define its protocol/document boundaries.

## Runtime behavior

Makers copy caller parameters/results/IDs. Raw documents permit every JSON value within a dedicated document; typed overloads validate concrete model metadata and reject untyped or engine-object schemas. Structured parameters are omitted when null and otherwise must be an object/array. IDs are strings, valid JSON numbers or explicit null. The wire parser preserves large/fractional numeric ID tokens without passing them through a floating-point conversion; formatting uses standard JSON text. The ordinary JSON utility parser remains permissive and separately documented.

Dispatch recognizes a valid envelope before treating an absent ID as a notification. Invalid envelopes produce an unknown-ID error. Valid notifications execute but produce no response, including parameter/handler failure. Root-only batching preserves input order, reports invalid entries individually, excludes notifications and returns nothing for notification-only batches. A typed callback receives the whole positional array or named object rather than dynamic CLR argument dispatch. Required fields, types and array arity follow the selected metadata and handler checks. No exception text crosses the InternalError boundary.

The registry gate protects replacement, snapshots and cleanup; callbacks run outside it. Existing calls retain their captured callback while replacement affects later entries. Disposal retires registrations and rejects subsequent calls; borrowed caller documents/targets are not disposed. Explicit document callbacks receive a temporary independent parameter resource; their result is copied before the temporary is disposed. All message work allocates and stays outside real-time callbacks.

## Integration and verification

`JSONRPCTests` exercises positional and source-generated named schemas, every maker, large/fractional/string/null IDs, malformed JSON/envelopes/parameter shapes, all five errors, mixed/empty/nested/notification-only batches, input/result ownership, temporary-document retirement, handler reentry, parallel pure dispatch and disposal. Its public `Node`/`SceneTree` host resolves localhost and exchanges a mixed request/notification batch and correlated response over `TCPServer`, `StreamPeerTCP` and `PacketPeerStream`. It uses only public engine operations.

Transport-independent JSON text can be carried by existing HTTP/WebSocket or framed packet consumers; those transports own their connection/proxy/TLS/timeout lifecycle and application authorization. This component does not imply rendering, public-network/remote endpoint, hardware, editor or other-platform acceptance.

## Linux package checkpoint (2026-10-14)

The source-generated public-only JSONRPCConsumer runs with JSON reflection disabled. The independent managed package `0.1.0-jsonrpc.20261014.1` uses a private package cache and the existing Linux native package; a self-contained publish runs from `/tmp` with `LD_LIBRARY_PATH` unset. Named-schema conversion, an exact 9007199254740993 numeric ID and ordinary SceneTree/framed TCP exchange pass. Runtime packaging checks cover 70 ELF files/78 engine notices and the existing 9/15 private exports. Evidence is retained in `bin/jsonrpc-validation/2026-10-14`; this confirms Linux x64/headless transport and compiled metadata, with NativeAOT, other hosts, rendering and routed/owner acceptance still separate.
