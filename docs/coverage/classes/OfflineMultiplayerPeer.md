# OfflineMultiplayerPeer API coverage

Last updated: 2026-10-04

Godot source: [modules/multiplayer/doc_classes/OfflineMultiplayerPeer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/OfflineMultiplayerPeer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [MultiplayerPeer](MultiplayerPeer.md). Electron2D type: [`public class Electron2D.OfflineMultiplayerPeer`](../../classes/OfflineMultiplayerPeer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OfflineMultiplayerPeer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/multiplayer/doc_classes/OfflineMultiplayerPeer.xml) | [`public class Electron2D.OfflineMultiplayerPeer`](../../classes/OfflineMultiplayerPeer.md) | Implemented | ADR 0094 multiplayer transport delivery: typed owner-thread peer identity/lifecycle/routing/next-packet metadata and all managed extension hooks, Offline local authority and executable owned WS/WSS server/client cohorts. Four-byte little-endian binary identity precedes connection events; broadcast/positive/exclusion targeting, Reliable/channel zero actual metadata, admission/deadlines, bounded aggregate inbox/backpressure and deterministic cleanup execute. Callback fanout/errors/reentry are defined. MultiplayerTests verifies native Linux two-client public scene flows, independent ID/raw-message wire, managed overrides/offline behavior, routing/pressure/refusal/invalid IDs/timeouts/cleanup and 64 warmed custom event/packet plus WS/WSS active/idle zero-managed-allocation cycles. Typed exceptions/spans replace Error/native/script-array ABI. Scene RPC/replication, ENet/WebRTC/browser/foreign/routed/external-native/owner acceptance remain separate. |
