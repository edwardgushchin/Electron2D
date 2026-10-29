"""Exercise every OGV/Ogg allocation refusal against built native checkers."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
from pathlib import Path
import subprocess


def sweep(command, attempts, label):
    def refused(position):
        result = subprocess.run([*command, str(position)], capture_output=True, text=True)
        if result.returncode:
            raise RuntimeError(f"{label}/{position}: {result.returncode}\n{result.stdout}{result.stderr}")
        row = json.loads(result.stdout)
        assert row["failure"] == position and row["attempts"] >= position, row
    with ThreadPoolExecutor(max_workers=2) as workers:
        for completed, _ in enumerate(workers.map(refused, range(1, attempts + 1)), 1):
            if completed % 100 == 0 or completed == attempts:
                print(f"{label}: {completed}/{attempts}", flush=True)


def check(checker, fixtures, output):
    container = checker.with_name("Electron2DOGGContainerCheck")
    container_baseline = json.loads(subprocess.check_output([str(container.resolve()), "0"], text=True))
    sweep([str(container.resolve())], container_baseline["attempts"], "Container")
    bits = checker.with_name("Electron2DOGGBitwiseCheck")
    bit_results = []
    for role in ("lsb", "msb"):
        command = [str(bits.resolve()), role]
        baseline = json.loads(subprocess.check_output([*command, "0"], text=True))
        sweep(command, baseline["attempts"], "Bitwise/" + role)
        bit_results.append(baseline)
    packets = checker.with_name("Electron2DVorbisPacketCheck")
    packet_baseline = json.loads(subprocess.check_output([str(packets.resolve()), "0"], text=True))
    sweep([str(packets.resolve())], packet_baseline["attempts"], "Comment packet")
    headers = checker.with_name("Electron2DVorbisHeadersCheck")
    header_baseline = json.loads(subprocess.check_output([str(headers.resolve()), "0"], text=True))
    sweep([str(headers.resolve())], header_baseline["attempts"], "DSP headers")
    oracles = []
    for name in ("Electron2DOGGBitwiseReference", "Electron2DOGGBitwiseOracle"):
        oracle = checker.with_name(name)
        result = subprocess.run([str(oracle.resolve())], capture_output=True, text=True)
        if result.returncode:
            raise RuntimeError(f"{name}: {result.returncode}\n{result.stdout}{result.stderr}")
        oracles.append({"name": name, "sha256": hashlib.sha256(oracle.read_bytes()).hexdigest(), "stdout": result.stdout, "stderr": result.stderr})
    results = []
    for fixture in fixtures:
        for role in ("encode", "encode-noise", "decode"):
            command = [str(checker.resolve()), str(fixture.resolve()), role]
            baseline = json.loads(subprocess.check_output([*command, "0"], text=True))
            sweep(command, baseline["attempts"], fixture.name + "/" + role)
            results.append({"fixture": str(fixture), "sha256": hashlib.sha256(fixture.read_bytes()).hexdigest(), **baseline})
    output.write_text(json.dumps({"checker_sha256": hashlib.sha256(checker.read_bytes()).hexdigest(),
                                 "container_checker_sha256": hashlib.sha256(container.read_bytes()).hexdigest(),
                                 "container": container_baseline, "bitwise_checker_sha256": hashlib.sha256(bits.read_bytes()).hexdigest(),
                                 "bitwise": bit_results, "packet_checker_sha256": hashlib.sha256(packets.read_bytes()).hexdigest(),
                                 "packet": packet_baseline, "headers_checker_sha256": hashlib.sha256(headers.read_bytes()).hexdigest(),
                                 "headers": header_baseline, "oracles": oracles, "results": results}, indent=2) + "\n")
    print(f"OGV allocations: {sum(row['attempts'] for row in results)} bridge, {container_baseline['attempts']} container, {sum(row['attempts'] for row in bit_results)} bit-buffer, {packet_baseline['attempts']} comment-header and {header_baseline['attempts']} DSP-header refusal positions; preparation/growth/retirement/retry passed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("checker", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("fixtures", type=Path, nargs="+")
    args = parser.parse_args()
    check(args.checker, args.fixtures, args.output)
