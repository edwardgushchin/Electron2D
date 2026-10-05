"""Run the published browser test app in Chromium and require its PASS result."""

import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import shutil
import subprocess
import tempfile
from threading import Thread


class Handler(SimpleHTTPRequestHandler):
    def log_message(self, *_):
        pass


def run(directory, browser):
    if not (directory / "index.html").exists():
        raise ValueError("The published browser test index is missing.")
    with ThreadingHTTPServer(("127.0.0.1", 0), partial(Handler, directory=str(directory))) as server:
        thread = Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            with tempfile.TemporaryDirectory(prefix="e2d-chromium-") as profile:
                result = subprocess.run([
                    browser, "--headless=new", "--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage",
                    "--no-first-run", "--user-data-dir=" + profile, "--dump-dom", "--virtual-time-budget=60000",
                    f"http://127.0.0.1:{server.server_port}/index.html",
                ], capture_output=True, text=True, timeout=120, check=True)
                if "<body>PASS</body>" not in result.stdout:
                    raise RuntimeError("Browser test did not pass:\n" + result.stdout + "\n" + result.stderr[-4000:])
                print("Browser WebAssembly contract checks passed in Chromium.")
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
