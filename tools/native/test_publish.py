"""Check runtime-pack selection before native payload inspection."""

import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_native_publish import check


class PublishTests(unittest.TestCase):
    def test_runtime_pack_matches_the_self_contained_framework(self):
        for version, framework, expected in (
                ("10.0.1", "10.0.1", "Missing native render/text files"),
                ("10.0.8", "10.0.8", "Missing native render/text files"),
                ("10.0.1", "10.0.8", "Missing self-contained runtime pack"),
                ("10.0.1", None, "Missing self-contained runtime pack")):
            with self.subTest(version=version, framework=framework), tempfile.TemporaryDirectory() as directory:
                publish = Path(directory)
                (publish / "Consumer.exe").touch()
                (publish / "Electron2D.dll").touch()
                libraries = dict.fromkeys(("SDL3-CS.Windows/3.4.18", "SDL3-CS.Windows.Image/3.4.6.12",
                    "SDL3-CS.Windows.Shadercross/3.0.0.13", "runtimepack.Microsoft.NETCore.App.Runtime.win-x64/" + version), {})
                (publish / "Consumer.deps.json").write_text(json.dumps(dict(libraries=libraries)))
                runtime = dict(tfm="net10.0")
                if framework:
                    runtime["includedFrameworks"] = [dict(name="Microsoft.NETCore.App", version=framework)]
                else:
                    runtime["framework"] = dict(name="Microsoft.NETCore.App", version=version)
                (publish / "Consumer.runtimeconfig.json").write_text(json.dumps(dict(runtimeOptions=runtime)))
                with self.assertRaisesRegex(AssertionError, expected):
                    check("win-x64", publish, "Consumer")


if __name__ == "__main__":
    unittest.main()
