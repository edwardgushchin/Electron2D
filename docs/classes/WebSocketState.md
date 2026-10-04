# WebSocketState

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.WebSocketState`. **Source:** [WebSocketPeer.cs](../../src/Core/Networking/WebSocketPeer.cs). **Component:** [WebSocket](../components/websocket.md).

## Description

Describes a WebSocket connection independently of its transport.

## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `Closed` | 3 | No active connection exists. |
| `Closing` | 2 | A close handshake is pending; keep polling. |
| `Connecting` | 0 | Transport or HTTP upgrade establishment is pending. |
| `Open` | 1 | Complete text and binary messages can be exchanged. |

## Enumeration Descriptions

<a id="member-b5bbf0b5006b"></a>
### Closed

`Closed = 3`

No active connection exists.

<a id="member-887fddbb8db5"></a>
### Closing

`Closing = 2`

A close handshake is pending; keep polling.

<a id="member-a05f85b0fdec"></a>
### Connecting

`Connecting = 0`

Transport or HTTP upgrade establishment is pending.

<a id="member-f198431a9798"></a>
### Open

`Open = 1`

Complete text and binary messages can be exchanged.

## Verification

WebSocketTests exercises state transitions, text/binary markers and invalid selectors. The enum preserves a distinct semantic domain under ADR 0051; it does not expose foreign backend enum identity.
