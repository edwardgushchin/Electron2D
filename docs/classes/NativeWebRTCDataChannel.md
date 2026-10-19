# NativeWebRTCDataChannel

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Visibility:** internal. **Source:** [WebRTCDataChannel.cs](../../src/Core/Networking/WebRTCDataChannel.cs).

## Description

Implements the public data-channel/PacketPeer contract against one retained native cohort. Cached label/protocol strings and immutable negotiated identity accompany live state/reliability/packet queries. Caller spans preserve boundaries and small-buffer retry; native receive overflow remains observable. The wrapper owns its native channel handle, while peer retirement closes transport without disposing the wrapper.

## Dependencies and verification

The [WebRTC component](../components/webrtc.md) owns native callback lifetime, owner-thread dispatch, queue budgets, disposal, package provenance and verification limits. WebRTCTests exercises this implementation through the public peer/channel/multiplayer/SceneTree workflows; NativePackageTests verifies unchanged dependency hashes and the required ABI. Native allocator counts and foreign execution remain separate gates.
