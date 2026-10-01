# Audio fixtures

Synthetic tones only; these files are test resources and are not published with the engine. manifest.json retains full SHA-256 digests. Existing stereo tone files are 0.4 s at 48 kHz; tone-mp3.f32 was independently reproduced through FFmpeg and tone-ogg.f32 through native libvorbisfile, byte-identically. Additional fixtures were generated with FFmpeg: mono.mp3 uses sine=frequency=523:sample_rate=22050:duration=0.2 with libmp3lame at 48k; short.ogg uses sine=frequency=440:sample_rate=48000:duration=0.000167; surround.ogg uses aevalsrc=0.3*sin(440*2*PI*t)|0.2*sin(880*2*PI*t)|0|0|0|0:s=48000:d=0.02 with libvorbis. mono-mp3.f32 uses FFmpeg -f f32le decoding.

Build the independent Vorbis oracle with `cc DecodeVorbis.c -lvorbisfile -o /tmp/decode-vorbis`, then `/tmp/decode-vorbis surround.ogg > surround-ogg.f32`. It preserves native Vorbis channel order and final granule length, without FFmpeg demux timestamp trimming. The six-channel source and oracle retain evidence of the pinned managed decoder's unsupported multichannel submap path; production rejects it explicitly. The ordinary executable suite does not require FFmpeg or native Vorbis tools.

multiplex.ogg prepends two valid CRC32 Ogg pages (serial 0xABCDEF01, sequence 0/1, BOS then ordinary flags, granule zero, one four-byte junk packet each) to tone.ogg, checking selection after unrelated logical streams.
