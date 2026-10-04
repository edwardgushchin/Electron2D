# WebSocketWriteMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.WebSocketWriteMode`. **Source:** [WebSocketPeer.cs](../../src/Core/Networking/WebSocketPeer.cs). **Component:** [WebSocket](../components/websocket.md).

## Description

Selects the WebSocket message payload contract.

## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `Binary` | 1 | Payload bytes are arbitrary binary data. |
| `Text` | 0 | Payload bytes must be valid UTF-8 text. |

## Enumeration Descriptions

<a id="member-d66b896a65e9"></a>
### Binary

`Binary = 1`

Payload bytes are arbitrary binary data.

<a id="member-eadb98fb0c84"></a>
### Text

`Text = 0`

Payload bytes must be valid UTF-8 text.

## Verification

WebSocketTests exercises state transitions, text/binary markers and invalid selectors. The enum preserves a distinct semantic domain under ADR 0051; it does not expose foreign backend enum identity.
