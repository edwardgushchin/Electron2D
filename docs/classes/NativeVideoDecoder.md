# NativeVideoDecoder

Last updated: 2026-10-29

`internal sealed partial class NativeVideoDecoder` in [NativeVideoDecoder.cs](../../src/Scene/Resources/NativeVideoDecoder.cs).
Part of [video playback](../components/video-playback.md). Unavailable to engine consumers.

Wraps five private C decoder entry points in the existing Electron2DOGV library. Metadata carries eight Int32 fields (including explicit ABI padding), two Int64 frame/sample totals and Double duration. DecoderHandle owns native destruction through SafeHandle. Calls retain that handle; the owner retains the decoder across copying borrowed native frame bytes and synchronous image observers.

Construction validates bounded compressed bytes, requested track and native metadata. Frame returns borrowed RGBA bytes until another frame operation or destruction. ReadAudio writes finite interleaved floats into existing storage; SeekAudio rewinds and decodes sequentially to a bounded sample cursor. Native failures become typed managed exceptions. Disposal releases packets, codec working state and frame/PCM storage. No native decoder hook runs on the audio callback.

The native recipe and sanitizers are described in [the bridge guide](../../tools/ogv-native/README.md); this is an internal ownership boundary, not a public backend API.

Internal OGVNativeMemory prepares the immutable pure-C callback record shared with the encoder. Missing allocator ABI fails before construction. Context, RGBA, packet-vector and compressed-packet payload joins the [direct bridge domain](../coverage/performance-producers.md#ogv-bridge-allocation-domain-2026-10-29); nested codec allocators remain open. See [OGVNativeMemory.cs](../../src/Scene/Resources/OGVNativeMemory.cs).
