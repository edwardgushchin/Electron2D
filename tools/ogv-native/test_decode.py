"""Decode produced OGV contracts with independent FFmpeg; no runtime codec helpers."""
import argparse
import array
import hashlib
import json
import math
from pathlib import Path
import struct
import subprocess


def pages(data):
    offset = 0
    while offset < len(data):
        assert data[offset:offset + 4] == b'OggS'
        count = data[offset + 26]
        size = 27 + count + sum(data[offset + 27:offset + 27 + count])
        page = bytearray(data[offset:offset + size])
        assert len(page) == size
        expected = struct.unpack_from('<I', page, 22)[0]
        page[22:26] = bytes(4)
        crc = 0
        for value in page:
            crc ^= value << 24
            for _ in range(8):
                crc = ((crc << 1) ^ (0x04c11db7 if crc & 0x80000000 else 0)) & 0xffffffff
        assert crc == expected
        yield page
        offset += size
    assert offset == len(data)


def inspect(path, expected):
    data = path.read_bytes()
    video_id = audio_id = None
    last = {}; timeline = []; video_shift = None; audio_eos = video_eos = False
    for page in pages(data):
        serial, number = struct.unpack_from('<II', page, 14)
        granule = struct.unpack_from('<q', page, 6)[0]
        assert number == last.get(serial, -1) + 1; last[serial] = number
        body = page[27 + page[26]:]
        if page[5] & 2:
            if body[1:7] == b'theora':
                video_id = serial; video_shift = (struct.unpack_from('>H', body, 40)[0] >> 5) & 31
            elif body[1:7] == b'vorbis': audio_id = serial
            else: raise AssertionError('Unexpected stream')
        elif granule >= 0:
            if serial == video_id:
                count = (granule >> video_shift) + (granule & ((1 << video_shift) - 1))
                timeline.append(count / expected['fps'])
            else: timeline.append(granule / expected['rate'])
        if serial == video_id: video_eos |= bool(page[5] & 4)
        if serial == audio_id: audio_eos |= bool(page[5] & 4)
    assert video_id is not None and audio_id is not None and video_eos and audio_eos
    assert all(a <= b + 1e-10 for a, b in zip(timeline, timeline[1:])), timeline
    probe = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-show_streams', '-of', 'json', str(path)]))
    v = next(s for s in probe['streams'] if s['codec_type'] == 'video')
    a = next(s for s in probe['streams'] if s['codec_type'] == 'audio')
    assert v['codec_name'] == 'theora' and a['codec_name'] == 'vorbis'
    assert (v['width'], v['height']) == (expected['width'], expected['height'])
    assert (a['channels'], int(a['sample_rate'])) == (expected['channels'], expected['rate'])
    def decode(*args):
        result = subprocess.run(['ffmpeg', '-nostdin', '-v', 'error', '-threads', '1', '-i', str(path), *args, '-'], capture_output=True)
        assert result.returncode == 0, result.stderr.decode(errors='replace')
        return result.stdout
    frames = expected['frames']; samples = frames * expected['rate'] // expected['fps']
    pixels = decode('-map', '0:v:0', '-fps_mode', 'passthrough', '-pix_fmt', 'rgb24', '-f', 'rawvideo') if frames else b''
    pcm = decode('-map', '0:a:0', '-c:a', 'pcm_f32le', '-f', 'f32le')
    assert len(pixels) == frames * expected['width'] * expected['height'] * 3, (path, len(pixels), frames)
    assert len(pcm) == samples * expected['channels'] * 4, (path, len(pcm), samples, expected['channels'])
    if frames: assert pixels[(5 * expected['width'] + 5) * 3] > 200
    if frames:
        info = json.loads(subprocess.check_output(['ffprobe', '-v', 'error', '-select_streams', 'v', '-show_frames', '-show_entries', 'frame=key_frame', '-of', 'json', str(path)]))['frames']
        keys = [i for i, f in enumerate(info) if f['key_frame']]
        assert keys and keys[0] == 0 and all(b - a <= expected['keyframes'] for a, b in zip(keys, keys[1:])), keys
    if frames == 17 and 'input_channels' in expected:
        values = array.array('f'); values.frombytes(pcm)
        frequencies = [220, 330, 440, 55, 550, 660, 770, 880]
        mapping = [0, 1, 2, 3, -1, -1] if expected['input_channels'] == 4 else list(range(expected['input_channels']))
        for channel, source in enumerate(mapping):
            start, end = 1024, samples - 1024
            actual = [values[i * expected['channels'] + channel] for i in range(start, end)]
            rms = math.sqrt(sum(x * x for x in actual) / len(actual))
            if source < 0: assert rms < .002, (path, channel, rms)
            else:
                signal = sum(value * math.sin(2 * math.pi * frequencies[source] * i / expected['rate']) for i, value in zip(range(start, end), actual)) * 2 / len(actual)
                assert .07 < signal < .13, (path, channel, source, signal, rms)
    return {'file': str(path), 'frames': frames, 'samples': samples, 'channels': expected['channels'],
            'encoded_sha256': hashlib.sha256(data).hexdigest(), 'pixels_sha256': hashlib.sha256(pixels).hexdigest(),
            'pcm_sha256': hashlib.sha256(pcm).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('folders', type=Path, nargs='+'); parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args(); rows = []
    for folder in args.folders:
        contracts = sorted(folder.glob('*.ogv.expect.json')) + sorted(folder.glob('*.OGV.expect.json'))
        assert contracts, folder
        for contract in contracts:
            rows.append(inspect(Path(str(contract)[:-len('.expect.json')]), json.loads(contract.read_text())))
    args.report.write_text(json.dumps(rows, indent=2) + '\n')
    print(f'Independent OGV decoding: {len(rows)} files, exact samples/pixels, mux timestamps, keyframe limits and native channel tones passed')


if __name__ == '__main__': main()
