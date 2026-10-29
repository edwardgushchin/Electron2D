# NativeVideoDecoder

Last updated: 2026-10-29

`internal sealed partial class NativeVideoDecoder` in [NativeVideoDecoder.cs](../../src/Scene/Resources/NativeVideoDecoder.cs).
Part of [video playback](../components/video-playback.md). Unavailable to engine consumers.

Wraps five private C decoder entry points in the existing Electron2DOGV library. Metadata carries eight Int32 fields (including explicit ABI padding), two Int64 frame/sample totals and Double duration. DecoderHandle owns native destruction through SafeHandle. Calls retain that handle; the owner retains the decoder across copying borrowed native frame bytes and synchronous image observers.

Construction validates bounded compressed bytes, requested track and native metadata. Frame returns borrowed RGBA bytes until another frame operation or destruction. ReadAudio writes finite interleaved floats into existing storage; SeekAudio rewinds and decodes sequentially to a bounded sample cursor. Native failures become typed managed exceptions. Disposal releases packets, codec working state and frame/PCM storage. No native decoder hook runs on the audio callback.

The native recipe and sanitizers are described in [the bridge guide](../../tools/ogv-native/README.md); this is an internal ownership boundary, not a public backend API.

Internal [OGVNativeMemory](../../src/Scene/Resources/OGVNativeMemory.cs) prepares the immutable pure-C record shared with recording. Missing allocator ABI fails before construction. The [bridge and Ogg allocation domains](../coverage/performance-producers.md#vorbis-header-allocation-domain-2026-10-29) count context/frame/copied-packet, stream/lacing/granule, synchronization, encoder bit-buffer and header storage, retaining transient peaks. Failed page growth reports OutOfMemoryException before publication. Decoder bit readers borrow packet bytes. Other Vorbis/Theora working storage remains open.
