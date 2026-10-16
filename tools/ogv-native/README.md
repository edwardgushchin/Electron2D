# Private OGV codecs

This C ABI encodes the existing recording host's RGB8/Int32 PCM into synchronized Theora/Vorbis Ogg output. It does not open a window, mix audio or spawn an encoder process. Four bridge functions own one context and a synchronous borrowed write callback. Callback failure is sticky; destroy never writes. The bridge reorders speakers, pads YCbCr420 planes, preserves sample granules and contains short-stream priming/EOS rules.

CMake pins official libogg 1.3.6, libvorbis 1.3.7 and libtheora 1.2.0 archives by SHA-256. Portable source lists follow upstream build files. Original notices live under licence/. vorbis-residue.cmake checks the original encoder hash and compiles a narrowly adapted copy that reuses shared residue setup allocations. Upstream submission status is tracked in docs/components/movie-writing.md.

Desktop/Android delivery uses one shared library. Static Apple/Web delivery merges the complete codec closure into one archive. Neither path requires global codec libraries. Native producer receipts cover this directory and the build recipe.

Build Electron2DOGVCheck and Electron2DVorbisResidueCheck through CMake. The former takes an existing output directory; it exercises bounds, all channel layouts, short/empty output, terminal calls and write callback failures. The latter initializes/clears 24 channel/quality profiles. Run sanitizer builds with leak detection; the unadapted dependency leaks during residue setup. test_decode.py uses independent FFmpeg and the generated expectation sidecars to check CRC/timestamps, exact decoded clocks/pixels, keyframe limits and native speaker tones. Runtime GPU/Compatibility checks use OGVMovieWriterTests; their native output may be checked with the same decoder.

Video playback adds five functions (nine exports total): e2d_video_create, e2d_video_frame, e2d_video_audio, e2d_video_seek_audio and e2d_video_destroy. Create copies/indexes bounded Ogg input, validates all selected Theora/Vorbis packets and warms decoder storage; it retains compressed packets and working frame/PCM buffers. The returned metadata has explicit 32-bit ABI padding before Int64 totals. Frame borrows opaque RGBA8 storage; Audio copies interleaved Vorbis-order floats. SeekAudio rewinds and discards to the requested sample; destroy releases every partial/full initialization stage. Input, dimensions, stream/packet counts and channels have explicit bounds. CRC holes, incomplete packets, unsupported headers and truncated pages/packets fail before publication.

GNU/Clang codec targets use -fwrapv for the releases' signed integer bit representations. This leaves bridge code under ordinary undefined-behavior instrumentation. No fetched decoder source is modified. Test with AddressSanitizer, UndefinedBehaviorSanitizer and LeakSanitizer; use Electron2DVideoAllocationCheck for wrapped malloc/calloc/realloc/free replay measurement after cold preparation.

Example checks from the repository root after configuring/building the native directory:

```bash
python3 -B tools/ogv-native/test_video.py obj/video-native/libElectron2DOGV.so bin/video-playback-validation/independent
obj/video-native/Electron2DVideoAllocationCheck bin/video-playback-validation/independent/*.ogv
```

The independent FFmpeg oracle covers 27 chroma/channel profiles plus multiple audio tracks, cropped rational-FPS images, sample counts/content, nonmonotonic frames, audio seeks and malformed input. It explicitly removes the minimum Vorbis half-window priming from skip_manual output; default decoder trimming omits endpoint samples in these fixtures. Preserve the encoder/lifecycle/residue tests above because both roles share one private codec closure. Source receipts and package/publish validators require the exact nine exports for every producer; Linux checks do not establish foreign-host execution.
