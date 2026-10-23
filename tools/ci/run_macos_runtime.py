#!/usr/bin/env python3
"""Retain an owned test process's output and sample prolonged silent intervals."""
from pathlib import Path
import subprocess
import sys
import threading
import time

IDLE_SECONDS = 300
POLL_SECONDS = 1


def run(executable, output):
    output = Path(output)
    output.parent.mkdir(parents=True, exist_ok=True)
    stopped = threading.Event()
    last_output = time.monotonic()
    process = subprocess.Popen([str(executable)], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)

    def sample_idle():
        next_sample = 0
        sequence = 0
        while not stopped.wait(POLL_SECONDS):
            now = time.monotonic()
            if process.poll() is not None:
                return
            if now - last_output < IDLE_SECONDS or now < next_sample:
                continue
            sequence += 1
            report = output.with_name(f'{output.stem}-stall-{sequence}.log')
            try:
                result = subprocess.run(['/usr/bin/sample', str(process.pid), '1', '-file', str(report)],
                                        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=15)
                if result.returncode or not report.exists():
                    report.write_bytes(f'Sample exit: {result.returncode}\n'.encode() + result.stdout)
            except (OSError, subprocess.TimeoutExpired) as error:
                report.write_text(f'Sample unavailable: {error}\n')
            next_sample = time.monotonic() + IDLE_SECONDS

    sampler = threading.Thread(target=sample_idle, name='macos-runtime-sampler')
    sampler.start()
    try:
        with output.open('wb') as log:
            for line in process.stdout:
                log.write(line)
                log.flush()
                sys.stdout.buffer.write(line)
                sys.stdout.buffer.flush()
                last_output = time.monotonic()
        code = process.wait()
    finally:
        stopped.set()
        sampler.join()
        process.stdout.close()
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
    sys.stdout.buffer.write(f'Runtime exit: {code}\n'.encode())
    sys.stdout.buffer.flush()
    return code if code else int(b'Electron2D checks passed.' not in output.read_bytes().splitlines())


if __name__ == '__main__':
    raise SystemExit(run(Path(sys.argv[1]).resolve(), sys.argv[2]))
