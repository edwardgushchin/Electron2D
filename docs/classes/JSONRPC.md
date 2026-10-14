# JSONRPC

Last updated: 2026-10-14

## Overview

`JSONRPC : ElectronObject` builds JSON-RPC 2.0 documents and synchronously dispatches explicitly registered C# callbacks. It is caller-owned and opens no transport. The existing `JSON` resource owns input/output trees; concrete generic models and optional compiled `JsonTypeInfo` metadata keep application values typed. See [JSON-RPC documents](../components/jsonrpc.md) and [ADR 0094](../decisions/networking.md#adr-0094).

## Syntax

```csharp
using var rpc = new JSONRPC();
rpc.SetMethod<int[], int>("sum", values => values.Sum());
using var request = rpc.MakeRequest("sum", new[] { 20, 22 }, 1L);
using var response = rpc.ProcessAction(request);
string wire = response!.Data!.ToJsonString();
```

## Member reference

| Member | Behavior |
| --- | --- |
| `JSONRPC()` | Empty independent dispatcher. |
| `ProcessAction(JSON action, bool recurse = false)` | Snapshots the input; returns a new caller-owned response or null for notifications only. Root arrays require `recurse`; nested batches are invalid. |
| `ProcessString(string action)` | Strict JSON wire parsing and compact output; automatically permits root batches. Empty input and notification-only output are empty strings. IDs retain their wire numeric token precision. |
| `SetMethod<TParameters, TResult>(name, Func<TParameters,TResult>, parametersTypeInfo = null, resultTypeInfo = null)` | Registers/replaces one case-sensitive name. The parameter model receives the complete array/object. Compiled metadata is required for custom models when reflection is disabled. |
| `SetMethod<TResult>(name, Func<TResult>, resultTypeInfo = null)` | Requires absent parameters or an empty object/array. |
| `SetMethod<TParameters>(name, Action<TParameters>, parametersTypeInfo = null)` / `SetMethod(name, Action)` | Actions return JSON null on successful requests; notifications have no response. |
| `SetMethod(name, Func<JSON,JSON?>)` | Receives an independent temporary document, disposed after invocation. A returned borrowed document is copied first and is not disposed. |
| `MakeRequest(method, JSON? parameters, JSON? id)` | Copies an array/object parameter document and string/number/null ID. A null parameter document omits params; a null ID emits explicit null. |
| `MakeRequest<TParameters,TID>(method, parameters, id, parametersTypeInfo = null, idTypeInfo = null)` | Typed independent request snapshot with the same shape validation. |
| `MakeNotification(method, JSON? parameters)` / `MakeNotification<TParameters>(method, parameters, parametersTypeInfo = null)` | Copies parameters and omits ID. |
| `MakeResponse(JSON? result, JSON? id)` / `MakeResponse<TResult,TID>(result, id, resultTypeInfo = null, idTypeInfo = null)` | Copies any result value and a valid ID into a success envelope. |
| `MakeResponseError(int code, string message, JSON? id = null)` / `MakeResponseError(JSONRPCErrorCode code, string message, JSON? id = null)` / `MakeResponseError<TID>(code, message, id, idTypeInfo = null)` | Standard or application-defined integer errors; absent/unknown IDs become JSON null. |
| `Dispose()` | Rejects later operations and clears registrations. Caller documents and delegate targets remain caller-owned. |

## Behavior and limits

Names are ordinal and case-sensitive; the `rpc.` prefix is reserved at registration/construction. There is no reflected method lookup or serialized engine-object reconstruction. Array arity belongs to the concrete handler; `ArgumentException` parameter rejection produces InvalidParams. Typed-schema failures also produce InvalidParams; other callback/result failures produce InternalError without exception text. Malformed envelopes, invalid IDs/versions, empty and nested batches produce InvalidRequest. Notifications suppress method, parameter and callback errors after a valid envelope is recognized. Ordered batches omit notifications and emit nothing when all entries are notifications.

One callback is captured under a private gate and invoked outside it. Reentry and replacement affect later calls, including later batch items. Calls execute synchronously on their calling thread; application callbacks retain scene/native ownership requirements. JSON documents are mutable: callers synchronize access while a snapshot is taken. Parsing, copying and conversion allocate and belong outside real-time callbacks. Wire JSON is strict with a 1024-level parser limit; the ordinary JSON utility retains its separate permissive parsing contract. Transport framing, timeouts, authentication and remote exposure belong to the owning application/transport.
