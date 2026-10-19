# WebRTCChannelHandle

Last updated: 2026-10-19

**Namespace:** `Electron2D`. **Visibility:** internal. **Source:** [WebRTCNative.cs](../../src/Core/Networking/WebRTCNative.cs).

## Description

Owns one opaque native shared channel-state handle. SafeHandle pins each native call and releases it through e2d_rtc_channel_destroy. Callback closures retain weak state, preventing a handle/callback ownership cycle.

## Dependencies and verification

The [WebRTC component](../components/webrtc.md) owns native callback lifetime, owner-thread dispatch, queue budgets, disposal, package provenance and verification limits. WebRTCTests exercises this implementation through the public peer/channel/multiplayer/SceneTree workflows; NativePackageTests verifies unchanged dependency hashes and the required ABI. Native allocator counts and foreign execution remain separate gates.
