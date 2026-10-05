"""Run the published browser test app in Chromium and require its PASS result."""

import argparse
from contextlib import suppress
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import tempfile
from threading import Event, Thread
import time
import uuid


def validate_result(value, token):
    if not isinstance(value, dict) or value.get("run") != token or value.get("status") not in ("passed", "failed"):
        raise ValueError("Invalid or stale browser test result.")
    if not isinstance(value.get("error", ""), str):
        raise ValueError("Invalid browser error detail.")
    return value


def stop(process):
    # Chromium children can outlive the launcher and keep writing its temporary profile.
    try:
        with suppress(ProcessLookupError):
            os.killpg(process.pid, signal.SIGTERM)
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        pass
    finally:
        with suppress(ProcessLookupError):
            os.killpg(process.pid, signal.SIGKILL)
        process.wait(timeout=10)


class Handler(SimpleHTTPRequestHandler):
    def log_message(self, *_):
        pass

    def do_POST(self):
        if self.path != "/__electron2d_result":
            self.send_error(404)
            return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if not 0 < length <= 16384:
                raise ValueError("Invalid result length.")
            value = validate_result(json.loads(self.rfile.read(length)), self.server.token)
        except (ValueError, UnicodeError):
            self.send_error(400)
            return
        self.server.result = value
        self.send_response(204)
        self.end_headers()
        self.server.completed.set()


def run(directory, browser):
    if not (directory / "index.html").exists():
        raise ValueError("The published browser test index is missing.")
    with ThreadingHTTPServer(("127.0.0.1", 0), partial(Handler, directory=str(directory))) as server:
        server.token = uuid.uuid4().hex
        server.result = None
        server.completed = Event()
        thread = Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            with tempfile.TemporaryDirectory(prefix="e2d-chromium-") as profile, tempfile.TemporaryFile(mode="w+t") as log:
                process = subprocess.Popen([
                    browser, "--headless=new", "--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage",
                    "--no-first-run", "--user-data-dir=" + profile, "--remote-debugging-port=0", "--enable-logging=stderr",
                    f"http://127.0.0.1:{server.server_port}/index.html?run={server.token}",
                ], stdout=log, stderr=log, text=True, start_new_session=True)
                try:
                    deadline = time.monotonic() + 120
                    while not server.completed.wait(1):
                        if process.poll() is not None or time.monotonic() >= deadline:
                            log.seek(0)
                            raise RuntimeError("Browser exited or timed out without a test result:\n" + log.read()[-4000:])
                    if server.result["status"] != "passed":
                        raise RuntimeError("Browser contract checks failed: " + server.result.get("error", ""))
                    print("Browser WebAssembly contract checks passed in Chromium.")
                finally:
                    stop(process)
        finally:
            server.shutdown()
            thread.join()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--browser", default=shutil.which("google-chrome") or shutil.which("chromium"))
    args = parser.parse_args()
    if not args.browser:
        parser.error("A Chromium browser is required.")
    run(args.directory.resolve(), args.browser)
