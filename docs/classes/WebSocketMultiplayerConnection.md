# WebSocketMultiplayerConnection

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [WebSocketMultiplayerPeer.cs](../../src/Core/Networking/WebSocketMultiplayerPeer.cs). **Component:** [Multiplayer](../components/multiplayer.md).

## Responsibilities

One peer identity, monotonic establishment start, ready/retired/event-suppression flags and its WebSocket/TCP/TLS lifetime. Client WebSocket owns its native chain; accepted server streams remain borrowed by WebSocket and owned by this record. Disposal releases WebSocket then TLS then TCP in nested finally blocks, so subscriber failure cannot strand the native transport. Retired records compact outside event iteration; host generation prevents reuse after callback reset. Native/managed connection preparation is cold lifecycle work.

## Verification

MultiplayerTests exercises normal/forced/host cleanup, peer disposal failure, listener restart, reentrant connection callback Close, client identity timeouts and prepared packet flow. Other hosts and external native allocation remain separate gates.
