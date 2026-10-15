# Private OGV encoder

This C ABI encodes the existing recording host's RGB8/Int32 PCM into synchronized Theora/Vorbis Ogg output. It does not open a window, mix audio or spawn an encoder process. Four bridge functions own one context and a synchronous borrowed write callback. Callback failure is sticky; destroy never writes. The bridge reorders speakers, pads YCbCr420 planes, preserves sample granules and contains short-stream priming/EOS rules.

CMake pins official libogg 1.3.6, libvorbis 1.3.7 and libtheora 1.2.0 archives by SHA-256. Portable source lists follow upstream build files. Original notices live under licence/. vorbis-residue.cmake checks the original encoder hash and compiles a narrowly adapted copy that reuses shared residue setup allocations. Upstream submission status is tracked in docs/components/movie-writing.md.

Desktop/Android delivery uses one shared library. Static Apple/Web delivery merges the complete codec closure into one archive. Neither path requires global codec libraries. Native producer receipts cover this directory and the build recipe.

Build Electron2DOGVCheck and Electron2DVorbisResidueCheck through CMake. The former takes an existing output directory; it exercises bounds, all channel layouts, short/empty output, terminal calls and write callback failures. The latter initializes/clears 24 channel/quality profiles. Run sanitizer builds with leak detection; the unadapted dependency leaks during residue setup. test_decode.py uses independent FFmpeg and the generated expectation sidecars to check CRC/timestamps, exact decoded clocks/pixels, keyframe limits and native speaker tones. Runtime GPU/Compatibility checks use OGVMovieWriterTests; their native output may be checked with the same decoder.
