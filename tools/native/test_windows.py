"""Exercise PE rejection and import-library production against restored SDL DLLs."""

from pathlib import Path
import os
import tempfile
import unittest

import windows
import package


class WindowsNativeTests(unittest.TestCase):
    def test_restored_sdl_architectures_exports_and_import_libraries(self):
        packages = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget/packages")) / "sdl3-cs.windows/3.4.16/runtimes"
        with tempfile.TemporaryDirectory() as directory:
            for rid in windows.MACHINES:
                path = packages / rid / "native/SDL3.dll"
                exports, imports = windows.inspect(path, rid, "SDL3.dll")
                self.assertIn("SDL_OpenAudioDeviceStream", exports)
                self.assertIn("kernel32.dll", imports)
                with self.assertRaises(ValueError):
                    windows.inspect(path, next(target for target in windows.MACHINES if target != rid), "SDL3.dll")
                with self.assertRaises(ValueError):
                    windows.inspect(path, rid, "foreign.dll")
                output = Path(directory) / rid
                windows.sdl_import_library(path, rid, output)
                self.assertGreater((output / "SDL3.lib").stat().st_size, 0)
            broken = Path(directory) / "broken.dll"
            broken.write_bytes(b"MZ")
            with self.assertRaises(ValueError):
                windows.inspect(broken, "win-x64", "broken.dll")

    def test_private_dependency_closure(self):
        from unittest.mock import patch
        with patch.object(windows, "inspect", return_value=(set(), {"kernel32.dll", "sdl3.dll"})):
            package.windows_exports(Path("FAudio.dll"), "win-x64", "FAudio.dll")
        for imports in ({"kernel32.dll"}, {"sdl3.dll", "vcruntime140.dll"}, {"sdl3.dll", "libicu.dll"}):
            with patch.object(windows, "inspect", return_value=(set(), imports)):
                with self.assertRaises(ValueError):
                    package.windows_exports(Path("FAudio.dll"), "win-x64", "FAudio.dll")
        with patch.object(windows, "inspect", return_value=(set(), {"libcrypto-3.dll"})):
            with self.assertRaises(ValueError):
                package.windows_exports(Path("libssl-3-Electron2D.dll"), "win-x64", "libssl-3-Electron2D.dll")


if __name__ == "__main__":
    unittest.main()
