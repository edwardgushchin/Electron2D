"""Exercise the public installation ID query with isolated Linux files."""

import hmac
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile


def query(command, sandbox=()):
    environment = os.environ.copy()
    for name in ("DISPLAY", "WAYLAND_DISPLAY", "SDL_VIDEODRIVER"):
        environment.pop(name, None)

    result = subprocess.run(
        [*sandbox, *command, "unique-id"],
        env=environment,
        capture_output=True,
        timeout=30,
        check=False,
    )
    if result.returncode or result.stderr:
        raise RuntimeError(f"Installation ID consumer failed (exit {result.returncode}).")
    return result.stdout


def derive(machine_id):
    return hmac.digest(b"Electron2D.OS.GetUniqueID", bytes.fromhex(machine_id), "sha256").hex().encode()


def require(condition, scenario):
    if not condition:
        raise AssertionError(scenario)


def main(command):
    system_path = Path("/etc/machine-id")
    original = system_path.read_bytes()
    host_id = next((line.strip() for line in original.split(b"\n") if line.strip()), b"")
    require(re.fullmatch(b"[0-9a-f]{32}", host_id) and host_id != b"0" * 32, "A valid host machine ID is required")
    require(query(command) == query(command) == derive(host_id.decode("ascii")), "Independent processes on the same installation")

    base = ["bwrap", "--ro-bind", "/", "/", "--proc", "/proc", "--cap-drop", "ALL"]
    first = "0123456789abcdef" * 2
    second = "fedcba9876543210" * 2
    fixtures = [
        ("Trimmed first nonempty line", f"\n \t\r\n {first} \t\r\n{second}\n", derive(first)),
        ("Different machine ID without a final newline", second, derive(second)),
        ("Trailing carriage return at end of file", f"{first}\r", derive(first)),
        ("Empty file", "", b""),
        ("Blank lines", "\r\n \t\n", b""),
        ("First boot marker before a valid ID", f"uninitialized\n{first}\n", b""),
        ("Damaged first line before a valid ID", f"g{first[1:]}\n{first}\n", b""),
        ("Carriage return before extra text", f"{first}\rextra\n{second}\n", b""),
        ("Carriage return between two IDs", f"{first}\r{second}", b""),
        ("Uppercase hexadecimal", first.upper(), b""),
        ("Zero ID", "0" * 32, b""),
        ("Wrong length", first + "0", b""),
        ("UUID punctuation", "01234567-89ab-cdef-0123-456789abcdef", b""),
        ("Corrected content", first, derive(first)),
    ]
    with tempfile.TemporaryDirectory(prefix="electron2d-machine-id-") as work:
        source = Path(work, "machine-id")
        bound = [*base, "--ro-bind", str(source), str(system_path)]
        for scenario, contents, expected in fixtures:
            source.write_bytes(contents.encode())
            require(query(command, bound) == expected, scenario)
            require(source.read_bytes() == contents.encode(), "The fixture was modified")

        source.chmod(0)
        require(query(command, bound) == b"", "Permission denied")
        source.chmod(0o600)
        require(query(command, bound) == derive(first), "Read permission restored")
        require(query(command, [*base, "--tmpfs", "/etc"]) == b"", "Missing file")

    require(system_path.read_bytes() == original, "The host machine ID was modified")
    print(f"Linux installation ID: independent processes and {len(fixtures) + 3} isolated scenarios passed.")


if __name__ == "__main__":
    if not sys.platform.startswith("linux") or len(sys.argv) < 2:
        raise SystemExit("Usage on Linux: test_machine_id.py COMMAND [ARG ...]")
    main(sys.argv[1:])
