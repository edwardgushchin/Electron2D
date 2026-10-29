# WebRTCNative

Last updated: 2026-10-29

**Namespace:** `Electron2D`. **Visibility:** internal. **Source:** [WebRTCNative.cs](../../src/Core/Networking/WebRTCNative.cs).

## Description

Binds the private 24-function C ABI with LibraryImport/cdecl, typed SafeHandle parameters and caller spans. Initialization verifies the matching TLS/crypto provider before creating a transport; stale exports or versions reject. Thread-local native diagnostics become IOException. No public callback pointer, backend type or raw handle is exposed.

## Dependencies and verification

The [WebRTC component](../components/webrtc.md) owns native callback lifetime, owner-thread dispatch, queue budgets, disposal, package provenance and verification limits. WebRTCTests exercises this implementation through the public peer/channel/multiplayer/SceneTree workflows; NativePackageTests verifies unchanged dependency hashes and the required ABI. Native allocator counts and foreign execution remain separate gates.
