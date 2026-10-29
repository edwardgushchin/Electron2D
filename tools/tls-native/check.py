"""Qualify one built Linux TLS allocation profile, including every observed refusal."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/native"))
import openssl_memory


def run(command, environment=None):
    result = subprocess.run([str(x) for x in command], env=environment, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=90)
    if result.returncode:
        raise RuntimeError(f"{command}: exit {result.returncode}\n{result.stdout}")
    return result.stdout


def qualify(build, output, sanitize=False):
    output.mkdir(parents=True, exist_ok=True)
    # Exercise the real pinned inputs, repeatable generation and each drift guard.
    with tempfile.TemporaryDirectory(dir=output) as temporary:
        source, destination = Path(temporary) / "source", Path(temporary) / "generated"
        for name in openssl_memory.SOURCES:
            (source / name).parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(build / ("openssl-4.1.0/" + name), source / name)
        openssl_memory.prepare(source, destination)
        first = {name: (destination / name).read_bytes() for name in openssl_memory.SOURCES}
        openssl_memory.prepare(source, destination)
        assert first == {name: (destination / name).read_bytes() for name in first}
        for name in first:
            original = (source / name).read_bytes()
            (source / name).write_bytes(original + b"\n")
            try:
                openssl_memory.prepare(source, destination)
                raise AssertionError("Source drift was accepted: " + name)
            except ValueError:
                assert first == {item: (destination / item).read_bytes() for item in first}
            finally:
                (source / name).write_bytes(original)
    normal, fault = output / "normal", output / "fault"
    normal.mkdir(exist_ok=True)
    fault.mkdir(exist_ok=True)
    for name in ("libcrypto.so.4", "libssl.so.4"):
        shutil.copy2(build / name, normal / name)
        symbols = run(["nm", "-D", "--undefined-only", normal / name])
        imports = {line.split()[-1].split("@")[0] for line in symbols.splitlines()}
        assert not imports & {"malloc", "calloc", "realloc", "free", "strdup", "strndup", "aligned_alloc", "posix_memalign", "memalign"}, name
    shutil.copy2(build / "core/libElectron2DENet.so", normal / "libElectron2DENet.so")
    flags = ["-fsanitize=address,undefined", "-fno-omit-frame-pointer", "-g1"] if sanitize else []
    run(["cc", "-std=c11", "-O2", *flags, "-shared", "-fPIC", "-fvisibility=hidden", "-Wl,-soname,libElectron2DENet.so", ROOT / "tools/tls-native/fault.c", "-pthread", "-o", fault / "libElectron2DENet.so"])
    includes = ["-I" + str(build / "build/include"), "-I" + str(build / "openssl-4.1.0/include")]
    executable = output / "check-memory"
    run(["cc", "-std=c11", "-O2", *flags, "-Wall", "-Wextra", "-Werror", ROOT / "tools/tls-native/check_memory.c", *includes,
         "-L" + str(normal), "-l:libssl.so.4", "-l:libcrypto.so.4", "-lElectron2DENet", "-pthread", "-ldl", "-o", executable])
    rows = []
    for profile in ("normal", "fault"):
        environment = dict(os.environ, LD_LIBRARY_PATH=os.pathsep.join([str(fault), str(normal)]) if profile == "fault" else str(normal))
        for mode in ("custom", "early", "exit", "primitives", "map-budget", "threads", "buffer", "aligned", "secure", "context", "digest"):
            stdout = run([executable, mode], environment)
            if profile == "fault":
                assert "TLS native exit: all owned bytes retired." in stdout
            rows.append(dict(profile=profile, mode=mode, fail_at=0, output=stdout))
            if profile == "fault" and mode in ("buffer", "aligned", "secure", "context", "digest"):
                calls = int(re.search(r"attempts=(\d+)", stdout)[1])
                for fail_at in range(1, calls + 1):
                    stdout = run([executable, mode, fail_at], environment)
                    assert "TLS native exit: all owned bytes retired." in stdout
                    rows.append(dict(profile=profile, mode=mode, fail_at=fail_at, output=stdout))
    binaries = {path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in normal.iterdir()}
    (output / "results.json").write_text(json.dumps(dict(binaries=binaries, runs=rows), indent=2))
    print(f"TLS native memory: {len(rows)} fresh processes passed; fault-core destructor verified complete retirement.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("build", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--sanitize", action="store_true")
    args = parser.parse_args()
    qualify(args.build.resolve(), args.output.resolve(), args.sanitize)
