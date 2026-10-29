# Private OGV codecs

This C ABI encodes the existing recording host's RGB8/Int32 PCM into synchronized Theora/Vorbis Ogg output. It does not open a window, mix audio or spawn an encoder process. Four bridge functions own one context and a synchronous borrowed write callback. Callback failure is sticky; destroy never writes. The bridge reorders speakers, pads YCbCr420 planes, preserves sample granules and contains short-stream priming/EOS rules.

CMake pins official libogg 1.3.6, libvorbis 1.3.7 and libtheora 1.2.0 archives by SHA-256. Portable source lists follow upstream build files. Original notices live under licence/. vorbis-residue.cmake checks the original encoder hash and compiles a narrowly adapted copy that reuses shared residue setup allocations. Upstream submission status is tracked in docs/components/movie-writing.md.

Desktop/Android delivery uses one shared library. Static Apple/Web delivery merges the complete codec closure into one archive. Neither path requires global codec libraries. Native producer receipts cover this directory and the build recipe.

Build Electron2DOGVCheck and Electron2DVorbisResidueCheck through CMake. The former takes an existing output directory; it exercises bounds, all channel layouts, short/empty output, terminal calls and write callback failures. The latter initializes/clears 24 channel/quality profiles. Run sanitizer builds with leak detection; the unadapted dependency leaks during residue setup. test_decode.py uses independent FFmpeg and the generated expectation sidecars to check CRC/timestamps, exact decoded clocks/pixels, keyframe limits and native speaker tones. Runtime GPU/Compatibility checks use OGVMovieWriterTests; their native output may be checked with the same decoder.

Video playback adds five functions (ten exports total, including allocator preparation): e2d_video_create, e2d_video_frame, e2d_video_audio, e2d_video_seek_audio and e2d_video_destroy. Create copies/indexes bounded Ogg input, validates all selected Theora/Vorbis packets and warms decoder storage; it retains compressed packets and working frame/PCM buffers. The returned metadata has explicit 32-bit ABI padding before Int64 totals. Frame borrows opaque RGBA8 storage; Audio copies interleaved Vorbis-order floats. SeekAudio rewinds and discards to the requested sample; destroy releases every partial/full initialization stage. Input, dimensions, stream/packet counts and channels have explicit bounds. CRC holes, incomplete packets, unsupported headers and truncated pages/packets fail before publication.

GNU/Clang codec targets use -fwrapv for the releases' signed integer bit representations. This leaves bridge code under ordinary undefined-behavior instrumentation. No fetched decoder source is modified. Test with AddressSanitizer, UndefinedBehaviorSanitizer and LeakSanitizer; use Electron2DVideoAllocationCheck for wrapped malloc/calloc/realloc/free replay measurement after cold preparation.

Example checks from the repository root after configuring/building the native directory:

```bash
python3 -B tools/ogv-native/test_video.py obj/video-native/libElectron2DOGV.so bin/video-playback-validation/independent
obj/video-native/Electron2DVideoAllocationCheck bin/video-playback-validation/independent/*.ogv
```

The independent FFmpeg oracle covers 27 chroma/channel profiles plus multiple audio tracks, cropped rational-FPS images, sample counts/content, nonmonotonic frames, audio seeks and malformed input. It explicitly removes the minimum Vorbis half-window priming from skip_manual output; default decoder trimming omits endpoint samples in these fixtures. Preserve the encoder/lifecycle/residue tests above because both roles share one private codec closure. Source receipts and package/publish validators require the exact ten exports for every producer; Linux checks do not establish foreign-host execution.


The bridge's e2d_ogv_memory entry point accepts one immutable E2DMemoryFunctions
record before either creation function. Incomplete or replacement callbacks fail;
identical concurrent registration is permitted. Engine callers use pure-C
process-lifetime callbacks from the common ledger, never managed thunks. The same
record covers every original Ogg/Vorbis/Theora allocation in the selected portable
encoder/decoder closure, direct frame/packet/context storage and owner metadata.
Unprepared creation returns -5. No separate codec version or allocation library
is loaded.

codec-memory.h is force-included before each Vorbis/Theora source. Its original
allocator aliases route to memory.c. Each opaque runtime context retains a native
pointer table; table capacity is real owned storage and is counted. Allocation
refusal jumps only to a live C entry frame and makes that context terminal with
-2. Its partially initialized vendor objects are never destructed; the owner
retires all retained blocks without dereferencing their contents. The floating
point environment is restored on refusal. Normal teardown still uses the codec
cleanup paths and releases the owner table. This mechanism is restricted to the
memory-only portable C closure; it is not a C++ unwinding or general OS-resource
cleanup policy.

Encoder write callbacks run with the jump frame suspended. Managed exceptions
remain contained by the existing callback thunk. Reentrant calls on the same
active context are rejected. A decoder failure leaves construction metadata and
context unpublished; a failed encoder constructor can return a partial context
which its caller must destroy. Existing staged output cancellation and atomic
track replacement remain in the managed owners. The four-function allocator and
ten-export ABI are unchanged.

Build Electron2DOGVMemoryCheck on Linux and run
`python3 -B tools/ogv-native/check_memory.py CHECKER RECEIPT FIXTURE...`.
The script rejects unwrapped heap imports in both codec archives and the final
native payload, then sweeps every observed refusal in a fresh process. Include
more than 128 packets and the large-noise encoder profile. Dependent checks cover
container streaming/CRC, both bit orders, full upstream byte vectors, comment
packets and DSP setup/header/decoder lifetimes. Electron2DOGVOwnerCheck exercises
hash growth/removal/resize, zero-byte ownership, nested callbacks, reentrancy,
FPU restoration, rollback and retry. An abandoned context requires a new owner;
partially initialized codec state is never resumed.

Repeat shared/static and address/undefined/leak-sanitizer profiles, independent
FFmpeg decoding, exact encoder-byte comparison, residue initialization and
prepared whole-codec replay. CLR/full-trim NativeAOT public profiles remain in
tests/NativeMemory.ICUHost. Electron2DOGVFaultCore is a test-only common allocator;
never distribute it. The complete domain evidence is linked from
[the allocation inventory](../../docs/coverage/native-memory-inventory.md).

vorbis-psy.cmake compiles a hash-checked copy of psy.c that clamps the second
noise-band interpolation index at the last band. The pinned code otherwise reads
one element past a row despite a zero interpolation weight, detected by UBSan.
The downloaded source remains unchanged. Qualification compares all generated
encoder files byte-for-byte with the preceding interpolation and repeats the
independent decoding and full sanitizer profiles.


ogg-memory.cmake compiles hash-checked framing.c and bitwise.c copies from the
same libogg pin. Both source/target checks complete before either copy is written.
Stream bodies, lacing/granule arrays, sync and bit buffers use the shared pure-C
allocator. A private native-thread epoch classifies packet/page insertion
failure before any managed/write callback can run. OOM becomes -2; invalid data
retains its existing error. Unscoped Ogg checks retain the library's invalidation path. Runtime allocation
refusal aborts the native call and retires the complete context owner. The container
checker also verifies thread isolation of refusal epochs and full healthy retry.

The bitwise copy also defines the result of a zero-bit MSB write when unsigned
long is 32 bits. The original shift traps under 32-bit shift instrumentation;
the adapted full upstream oracle passes with the same instrumentation. This
integer-width check does not establish execution on another platform.

Run `python3 -B tools/ogv-native/test_ogg_source.py SOURCE/src/framing.c` for
pristine-copy, changed-source and changed-target controls. Before changing any
upstream pin, audit the complete selected source list for allocation/global-state
and non-memory resources, and repeat compiled heap-import/refusal qualification.
The former info.c/block.c allocator copies are superseded by whole-codec routing.
