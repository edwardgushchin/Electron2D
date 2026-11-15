"""Verify immutable native pins through a Windows-style Git checkout."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import package


class WindowsCheckoutTests(unittest.TestCase):
    def test_checkout_preserves_pinned_bytes_and_rejects_content_changes(self):
        pins = {}
        for vendor in ("PCRE2", "libdatachannel", "MSDFGen", "ICU", "ENet", "FastLZ", "meshoptimizer"):
            root = Path("src/Vendor") / vendor
            files = json.loads((package.ROOT / root / "manifest.json").read_text())["files"]
            if isinstance(files, list):
                files = {item["path"]: item["sha256"] for item in files}
            pins.update({(root / name).as_posix(): digest for name, digest in files.items()})
        paths = set(pins) | {"src/Vendor/PCRE2/manifest.json", "tools/update-pcre2.py", "licence/PCRE2-LICENSE.md"}
        with tempfile.TemporaryDirectory(prefix="native checkout ") as temporary:
            output = Path(temporary)
            subprocess.run(["git", "-c", "core.autocrlf=true", "-c", "core.longpaths=true", "checkout-index",
                "--prefix=" + output.as_posix() + "/", "--stdin"], cwd=package.ROOT,
                input="\n".join(sorted(paths)).encode(), capture_output=True, check=True)
            for name, digest in pins.items():
                self.assertEqual(hashlib.sha256((output / name).read_bytes()).hexdigest(), digest, name)
            command = [sys.executable, "-B", str(output / "tools/update-pcre2.py"), "--check"]
            checked = subprocess.run(command, capture_output=True, text=True)
            self.assertEqual(checked.returncode, 0, checked.stdout + checked.stderr)
            source = output / "src/Vendor/PCRE2/AUTHORS.md"
            source.write_bytes(source.read_bytes() + b"\nchanged source\n")
            corrupted = subprocess.run(command, capture_output=True, text=True)
            self.assertNotEqual(corrupted.returncode, 0)
            self.assertIn("AUTHORS.md", corrupted.stderr)


if __name__ == "__main__":
    unittest.main()
