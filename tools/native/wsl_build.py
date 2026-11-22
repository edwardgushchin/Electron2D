"""Run a heavy build command in the shared Electron2D WSL build slot."""
import os
from pathlib import Path
import platform
import subprocess
import sys


def is_wsl():
    return sys.platform == "linux" and "microsoft" in platform.release().lower()


def jobs(normal):
    return "1" if is_wsl() else str(normal)


def run_build(command, **options):
    if not is_wsl():
        subprocess.run(command, check=True, **options)
        return

    import pwd

    # A build can set HOME to a scratch directory for its own tool caches.
    lock = Path(pwd.getpwuid(os.geteuid()).pw_dir) / ".cache" / "electron2d-wsl-build.lock"
    lock.parent.mkdir(parents=True, exist_ok=True)
    result = subprocess.run([
        "flock", "--no-fork", "--nonblock", "--conflict-exit-code", "75", str(lock),
        "timeout", "--kill-after=15s", "45m", *command,
    ], **options)
    if result.returncode == 75:
        raise SystemExit("The Electron2D WSL build slot is busy. Wait for the current build to finish.")
    if result.returncode in (124, 137):
        raise SystemExit(f"The Electron2D WSL build was interrupted (exit {result.returncode}). Check the build log before retrying.")
    result.check_returncode()


if __name__ == "__main__":
    args = sys.argv[1:]
    if len(args) < 2 or args[0] != "--":
        raise SystemExit("Usage: python3 -B tools/native/wsl_build.py -- COMMAND [ARG ...]")
    run_build(args[1:])
