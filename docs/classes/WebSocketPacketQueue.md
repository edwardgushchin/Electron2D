# WebSocketPacketQueue

Last updated: 2026-10-04

**Visibility:** internal. **Source:** [WebSocketPeer.cs](../../src/Core/Networking/WebSocketPeer.cs). **Component:** [WebSocket](../components/websocket.md).

## Responsibilities

Prepared byte ring and value records retain complete input messages or partially drained output frames. Separate application count/payload totals exclude control/frame overhead; first-message size/text metadata stays stable until consumption. Store/Peek/Consume/Take wrap without allocating. Prepare runs only at connection setup. Clear discards logical queue state without borrowing or disposing the stream. WebSocketPeer checks capacity before Store and supplies exact read sizes; four output records and the documented byte reserve allow control traffic under application pressure.

## Verification

WebSocketTests exercises input/output/control backpressure, record/payload accounting, wrap/reuse through prepared repeated messages, empty messages, partial sends and zero warmed managed allocation. Foreign hosts, native externals and routed performance remain separate gates.
