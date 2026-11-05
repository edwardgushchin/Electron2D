"""Exercise original C allocation ownership and refusal through real image owners."""

import argparse
import base64
import ctypes as C
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import resource
import struct
import subprocess
import sys


class Snapshot(C.Structure):
    _fields_ = [(name, C.c_uint64) for name in (
        "managedCurrent", "managedPeak", "nativeCurrent", "nativePeak", "current", "peak",
        "allocated", "retired", "allocations", "resizes", "frees", "failures")]


class Info(C.Structure):
    _fields_ = [(name, C.c_uint32) for name in (
        "width", "height", "levels", "version", "format", "flags", "swizzle", "reserved")]


def encoders(codec, rdo=False):
    encode = codec.e2d_image_encode
    encode.argtypes = [C.c_uint32] * 7 + [C.c_float, C.c_void_p, C.c_uint64, C.POINTER(C.c_void_p), C.POINTER(C.c_uint64)]
    codec.e2d_image_encoded_free.argtypes = [C.c_void_p]
    pixels = bytes([224, 64, 32, 255] * (16 + 4 + 1))
    if rdo:
        pixels = bytes(channel for edge in (8, 4, 2, 1) for y in range(edge) for x in range(edge)
                       for channel in ((x * 31 + y * 17) % 256, (x * 7 + y * 43) % 256,
                                       (x * 11 + y * 13) % 256, 255 - (x + y) % 4 * 31))
    source = C.create_string_buffer(pixels)

    def current():
        state = Snapshot()
        codec.e2d_memory_snapshot(C.byref(state))
        return state.nativeCurrent

    def call(kind, failure):
        codec.e2d_test_arm(failure)
        output, size = C.c_void_p(1), C.c_uint64(123)
        status = encode(kind, 8 if rdo else 4, 8 if rdo else 4, 4 if rdo else 3, 0, 0, 1,
                        1.25 if rdo else 0, source, len(pixels), C.byref(output), C.byref(size))
        attempts = codec.e2d_test_attempts()
        try:
            if status:
                assert not output.value and not size.value, ("encoder failed publication", kind, failure, status)
                encoded = None
            else:
                assert output.value and size.value
                encoded = C.string_at(output, size.value)
            assert source.raw[:-1] == pixels
        finally:
            codec.e2d_image_encoded_free(output)
        return status, attempts, encoded

    requests, digests = [], []
    for kind in ([0] if rdo else range(7)):
        assert call(kind, 0)[0] == 0
        baseline = current()
        healthy = call(kind, 0)
        assert healthy[0] == 0 and current() == baseline
        for failure in range(1, healthy[1] + 1):
            refused = call(kind, failure)
            assert refused[0] != 0 and current() == baseline, ("encoder refusal/drain", kind, failure, refused[:2], current(), baseline)
            retry = call(kind, 0)
            assert retry[0] == 0 and retry[2] == healthy[2] and current() == baseline, ("encoder retry", kind, failure)
        requests.append(healthy[1])
        digests.append(hashlib.sha256(healthy[2]).hexdigest())
    return {"name": "rdo-encoder" if rdo else "encoders", "requests": requests, "refusals": sum(requests), "sha256": digests}


def child(library, fixture, failure):
    ledger = codec = C.CDLL(str(library))
    ledger.e2d_memory_snapshot.argtypes = [C.POINTER(Snapshot)]
    ledger.e2d_test_arm.argtypes = [C.c_size_t]
    ledger.e2d_test_attempts.restype = C.c_size_t
    if fixture.get("container") == "encode":
        return encoders(codec)
    if fixture.get("container") == "encode-rdo":
        return encoders(codec, rdo=True)
    if fixture.get("container") == "block":
        return blocks(codec, fixture)
    create = codec.e2d_basis_create if fixture.get("container") == "basis" else codec.e2d_ktx_create
    create.argtypes = [C.c_void_p, C.c_uint64, C.POINTER(Info), C.POINTER(C.c_void_p)]
    codec.e2d_ktx_destroy.argtypes = [C.c_void_p]
    codec.e2d_ktx_level.argtypes = [C.c_void_p, C.c_uint32, C.c_void_p, C.c_uint64, C.c_uint32, C.c_uint32]
    source = base64.b64decode(fixture["encoded"])
    data = C.create_string_buffer(source)
    expected = base64.b64decode(fixture["pixels"])

    def snapshot():
        result = Snapshot()
        ledger.e2d_memory_snapshot(C.byref(result))
        return result

    def call(refusal):
        ledger.e2d_test_arm(refusal)
        info, context = Info(), C.c_void_p()
        status = create(data, len(source), C.byref(info), C.byref(context))
        attempts, live = ledger.e2d_test_attempts(), snapshot().nativeCurrent
        try:
            if status:
                assert not context.value, ("failed owner publication", refusal, status)
                assert status in (-3, 13), ("allocation failure status", refusal, status)
            else:
                assert context.value
                dimensions = [(max(1, info.width >> level), max(1, info.height >> level)) for level in range(info.levels)]
                pixels = sum(w * h for w, h in dimensions)
                stride, remainder = divmod(len(expected), pixels)
                assert not remainder and stride in (1, 2, 3, 4, 6, 8, 12, 16)
                actual = bytearray()
                for level, (w, h) in enumerate(dimensions):
                    output = C.create_string_buffer(w * h * stride)
                    assert codec.e2d_ktx_level(context, level, output, len(output), w * stride, h) == 0
                    actual.extend(output.raw)
                assert actual == expected, ("independent pixels", refusal)
            assert data.raw[:-1] == source
        finally:
            codec.e2d_ktx_destroy(context)
        return status, attempts, live, snapshot().nativeCurrent

    if failure:
        result = call(failure)
        assert result[0] != 0, ("cold refusal", failure, result)
        retry = call(0)
        assert retry[0] == 0, ("cold retry", failure, retry)
        return {"baseline": retry[3]}
    cold = call(0)
    assert cold[0] == 0
    baseline = snapshot().nativeCurrent
    healthy = call(0)
    assert healthy[0] == 0 and healthy[3] == baseline and healthy[2] > baseline
    for index in range(1, healthy[1] + 1):
        result = call(index)
        assert result[0] != 0 and result[3] == baseline, ("warm refusal/drain", index, result, baseline)
        retry = call(0)
        assert retry[0] == 0 and retry[3] == baseline, ("warm retry", index, retry, baseline)
    return {"name": fixture["name"], "coldRequests": cold[1], "requests": healthy[1],
            "baseline": baseline, "live": healthy[2], "peak": snapshot().nativePeak}


def blocks(codec, fixture):
    decode = codec.e2d_block_decode
    decode.argtypes = [C.c_uint32] * 3 + [C.c_void_p, C.c_uint64, C.c_void_p, C.c_uint64]
    source, expected = base64.b64decode(fixture["encoded"]), base64.b64decode(fixture["pixels"])
    mode = fixture["mode"]
    block, stride = (8 if mode >= 10 else 4), (16 if mode in (5, 7) else 8 if mode in (9, 11) else 4)
    block_bytes = 16 if mode >= 8 or mode in (2, 6, 7) else 8
    width, height = fixture["width"], fixture["height"]
    encoded_offset = decoded_offset = refusals = 0

    def current():
        state = Snapshot()
        codec.e2d_memory_snapshot(C.byref(state))
        return state.nativeCurrent

    while True:
        encoded_size = ((width + block - 1) // block) * ((height + block - 1) // block) * block_bytes
        decoded_size = width * height * stride
        borrowed = b"!" + source[encoded_offset:encoded_offset + encoded_size] + b"!"
        data = C.create_string_buffer(borrowed)
        oracle = expected[decoded_offset:decoded_offset + decoded_size]
        baseline = current()

        def call(failure):
            output = C.create_string_buffer(b"?" * (decoded_size + 4))
            codec.e2d_test_arm(failure)
            status = decode(mode, width, height, C.byref(data, 1), encoded_size, C.byref(output, 2), decoded_size)
            attempts = codec.e2d_test_attempts()
            assert current() == baseline and data.raw[:-1] == borrowed
            assert output.raw[:2] == b"??" and output.raw[-3:-1] == b"??"
            if status:
                assert status == -3 and output.raw[:-1] == b"?" * (decoded_size + 4), ("block refusal publication", fixture["name"], failure, status)
            else:
                assert output.raw[2:2 + decoded_size] == oracle, fixture["name"]
            return status, attempts

        healthy = call(0)
        assert healthy[0] == 0
        for refusal in range(1, healthy[1] + 1):
            assert call(refusal)[0] != 0, ("block refusal", fixture["name"], refusal)
            assert call(0)[0] == 0
            refusals += 1
        encoded_offset += encoded_size
        decoded_offset += decoded_size
        if width == 1 and height == 1:
            break
        width, height = max(1, width // 2), max(1, height // 2)
    assert encoded_offset == len(source) and decoded_offset == len(expected)
    return {"name": fixture["name"], "blockRefusals": refusals}


def raw_fixture(metadata):
    entries = bytearray()
    for index in range(metadata):
        item = ("key-" + str(index)).encode() + b"\0value"
        entries.extend(struct.pack("<I", len(item)) + item + b"\0" * (-len(item) % 4))
    pixels = bytes(range(64))
    source = b"\xabKTX 11\xbb\r\n\x1a\n" + struct.pack("<13I", 0x04030201, 0x1401, 1, 0x1908, 0x8058, 0x1908, 4, 4, 0, 0, 1, 1, len(entries))
    return {"name": "ktx1-metadata" if metadata else "ktx1-raw", "encoded": base64.b64encode(source + entries + struct.pack("<I", 64) + pixels).decode(), "pixels": base64.b64encode(pixels).decode()}


def main():
    resource.setrlimit(resource.RLIMIT_CORE, (0, 0))
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("library", type=Path)
    parser.add_argument("oracles", type=Path, nargs="*")
    parser.add_argument("--cold", action="store_true")
    parser.add_argument("--blocks-oracle", type=Path)
    parser.add_argument("--child", type=int)
    args = parser.parse_args()
    if args.child is not None:
        print(json.dumps(child(args.library, json.load(sys.stdin), args.child)), flush=True)
        return

    def run(fixture, refusal):
        command = [sys.executable, "-B", __file__, str(args.library.resolve()), "--child", str(refusal)]
        result = subprocess.run(command, input=json.dumps(fixture), capture_output=True, text=True, timeout=90)
        if result.returncode:
            raise RuntimeError(f"{fixture['name']} refusal {refusal}, exit {result.returncode}:\n{result.stdout}\n{result.stderr}")
        return json.loads(result.stdout.strip().splitlines()[-1])

    print(json.dumps(run({"name": "encoders", "container": "encode"}, 0)), flush=True)
    print(json.dumps(run({"name": "rdo-encoder", "container": "encode-rdo"}, 0)), flush=True)
    if args.blocks_oracle:
        for fixture in json.loads(args.blocks_oracle.read_text())["fixtures"]:
            if fixture["source_format"] != "Etc2RaAsRg":
                print(json.dumps(run(dict(fixture, container="block"), 0)), flush=True)
    fixtures = [raw_fixture(0), raw_fixture(512)]
    for oracle in args.oracles:
        fixtures.extend(json.loads(oracle.read_text())["fixtures"])
    for fixture in fixtures:
        result = run(fixture, 0)
        print(json.dumps(result), flush=True)
        if args.cold and fixture["name"] in ("basis-lz", "etc1s"):
            executor = ThreadPoolExecutor(max_workers=4)
            try:
                for index, retry in enumerate(executor.map(lambda refusal: run(fixture, refusal), range(1, result["coldRequests"] + 1)), 1):
                    assert retry["baseline"] == result["baseline"], ("cold retry ownership", index, retry, result)
                    if index % 128 == 0:
                        print(f"{fixture['name']}: cold {index}/{result['coldRequests']}", flush=True)
            finally:
                executor.shutdown(wait=True, cancel_futures=True)
            print(f"{fixture['name']}: {result['coldRequests']} independent cold refusals/retries passed", flush=True)


if __name__ == "__main__":
    main()
