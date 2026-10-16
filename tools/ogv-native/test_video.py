"""Compare the private decoder with independently encoded and decoded FFmpeg files."""
import argparse
import ctypes as c
import json
from pathlib import Path
import struct
import re
import subprocess


class Info(c.Structure):
    _fields_ = [(name, c.c_int) for name in
                ("width", "height", "fps_numerator", "fps_denominator", "channels", "rate", "tracks", "reserved")]
    _fields_ += [("frames", c.c_int64), ("samples", c.c_int64), ("duration", c.c_double)]


def run(*args):
    return subprocess.check_output(["ffmpeg", "-v", "error", "-nostdin", "-threads", "1", *map(str, args)])


def check(library, output):
    output.mkdir(parents=True, exist_ok=True)
    lib = c.CDLL(str(library.resolve()))
    lib.e2d_video_create.argtypes = [c.c_void_p, c.c_size_t, c.c_int, c.POINTER(Info), c.POINTER(c.c_void_p)]
    lib.e2d_video_destroy.argtypes = [c.c_void_p]
    lib.e2d_video_frame.argtypes = [c.c_void_p, c.c_int64, c.POINTER(c.c_void_p)]
    lib.e2d_video_audio.argtypes = [c.c_void_p, c.POINTER(c.c_float), c.c_int]
    lib.e2d_video_seek_audio.argtypes = [c.c_void_p, c.c_int64]
    results = []
    for chroma in ("yuv420p", "yuv422p", "yuv444p"):
        for channels in range(9):
            path = output / f"{chroma}-{channels}.ogv"
            args = ["-y", "-f", "lavfi", "-i", "testsrc2=size=34x26:rate=30000/1001:duration=0.3"]
            if channels:
                expression = "|".join(f"0.1*sin(2*PI*{300 + i * 200}*t)" for i in range(channels))
                args += ["-f", "lavfi", "-i", f"aevalsrc={expression}:s=48000:d=0.3", "-c:a", "libvorbis"]
            run(*args, "-c:v", "libtheora", "-pix_fmt", chroma, "-threads", "1", path)
            data = path.read_bytes()
            info, reader = Info(), c.c_void_p()
            assert lib.e2d_video_create(data, len(data), 0, c.byref(info), c.byref(reader)) == 0, path
            try:
                assert (info.width, info.height, info.frames, info.channels) == (34, 26, 9, channels), (path, {name: getattr(info, name) for name, _ in info._fields_})
                expected = run("-i", path, "-map", "0:v:0", "-vf", "fps=30000/1001", "-f", "rawvideo", "-pix_fmt", "rgba", "-")
                extent = info.width * info.height * 4
                assert len(expected) == extent * info.frames
                worst_pixel = 0
                # Nonmonotonic requests also test prepared rewind and duplicate reads.
                for frame in [*range(info.frames), 0, 7, 2, 2]:
                    rgba = c.c_void_p()
                    assert lib.e2d_video_frame(reader, frame, c.byref(rgba)) >= 0
                    actual = c.string_at(rgba, extent)
                    error = max(abs(a - b) for a, b in zip(actual, expected[frame * extent:(frame + 1) * extent]))
                    worst_pixel = max(worst_pixel, error)
                    assert error <= 3, (path, frame, error)
                max_pcm = 0
                if channels:
                    expected_pcm = run("-flags2", "+skip_manual", "-i", path, "-map", "0:a:0", "-f", "f32le", "-")
                    # Decode priming/padding explicitly: the minimum block's half-window precedes PCM.
                    header = data.index(b"\x01vorbis")
                    priming = 1 << ((data[header + 28] & 15) - 1)
                    expected_pcm = expected_pcm[priming * channels * 4:(priming + 14400) * channels * 4]
                    # Reference output uses speaker order; the native decoder retains Vorbis order.
                    order = {1: [0], 2: [0, 1], 3: [0, 2, 1], 4: [0, 1, 2, 3], 5: [0, 2, 1, 3, 4], 6: [0, 2, 1, 5, 3, 4], 7: [0, 2, 1, 6, 5, 3, 4], 8: [0, 2, 1, 7, 5, 6, 3, 4]}[channels]
                    samples = []
                    buffer = (c.c_float * (257 * channels))()
                    while True:
                        count = lib.e2d_video_audio(reader, buffer, 257)
                        assert count >= 0
                        if not count:
                            break
                        for frame in range(count):
                            samples.extend(buffer[frame * channels + i] for i in order)
                    oracle = struct.unpack(f"<{len(expected_pcm) // 4}f", expected_pcm)
                    assert len(samples) == len(oracle), (path, len(samples), len(oracle))
                    max_pcm = max(abs(a - b) for a, b in zip(samples, oracle))
                    assert max_pcm < 1e-5, (path, max_pcm)
                    assert info.samples == len(samples) // channels
                    for seek in (0, 7000, 127, info.samples, info.samples + 10):
                        assert lib.e2d_video_seek_audio(reader, seek) == 0
                        count = lib.e2d_video_audio(reader, buffer, 257)
                        want = min(257, max(0, info.samples - seek))
                        assert count == want, (path, seek, count, want)
                        for frame in range(count):
                            for channel, source in enumerate(order):
                                assert abs(buffer[frame * channels + source] - oracle[(seek + frame) * channels + channel]) < 1e-5
                results.append({"file": path.name, "max_pixel_error": worst_pixel, "max_pcm_error": max_pcm,
                                "frames": info.frames, "samples": info.samples, "duration": info.duration})
            finally:
                lib.e2d_video_destroy(reader)
            for invalid in (b"", data[:20], data[:len(data) // 2], data[:-1], b"bad" + data, data + b"bad"):
                rejected = c.c_void_p()
                assert lib.e2d_video_create(invalid, len(invalid), 0, c.byref(info), c.byref(rejected)) < 0
                assert not rejected.value
    multi = output / "multi-track.ogv"
    run("-y", "-f", "lavfi", "-i", "testsrc2=size=34x26:rate=30000/1001:duration=0.3",
        "-f", "lavfi", "-i", "aevalsrc=0.1*sin(2*PI*300*t)|0.1*sin(2*PI*500*t):s=48000:d=0.3",
        "-f", "lavfi", "-i", "sine=frequency=900:sample_rate=32000:duration=0.3",
        "-map", "0:v:0", "-map", "1:a:0", "-map", "2:a:0", "-c:v", "libtheora", "-c:a", "libvorbis", "-threads", "1", multi)
    data = multi.read_bytes()
    for track in (0, 1, -1, 99):
        info, reader = Info(), c.c_void_p()
        assert lib.e2d_video_create(data, len(data), track, c.byref(info), c.byref(reader)) == 0
        try:
            assert info.tracks == 2 and info.channels == {0: 2, 1: 1}.get(track, 0)
            if track >= 0 and track < 2:
                assert info.rate == (48000 if track == 0 else 32000)
                pcm = (c.c_float * (int(info.rate * .3 + 1024) * info.channels))()
                count = lib.e2d_video_audio(reader, pcm, int(info.rate * .3 + 1024))
                assert count == int(info.rate * .3)
                decoded = run("-flags2", "+skip_manual", "-i", multi, "-map", f"0:a:{track}", "-f", "f32le", "-")
                header = [m.start() for m in re.finditer(b"\x01vorbis", data)][track]
                skip = (1 << ((data[header + 28] & 15) - 1)) * info.channels * 4
                oracle = struct.unpack(f"<{count * info.channels}f", decoded[skip:skip + count * info.channels * 4])
                assert max(abs(pcm[i] - value) for i, value in enumerate(oracle)) < 1e-5, (track, info.rate)
        finally:
            lib.e2d_video_destroy(reader)
    (output / "decode-results.json").write_text(json.dumps(results, indent=2) + "\n")
    print(f"Independent video decode checks passed: {len(results)} profiles")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("library", type=Path)
    parser.add_argument("output", type=Path)
    options = parser.parse_args()
    check(options.library, options.output)
