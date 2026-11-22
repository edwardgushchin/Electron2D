from pathlib import Path
import subprocess
import sys
from tempfile import TemporaryDirectory
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import wsl_build


class WslBuildTests(unittest.TestCase):
    def test_wsl_compiles_in_one_shared_slot_with_a_deadline(self):
        with TemporaryDirectory() as directory:
            account = SimpleNamespace(pw_dir=directory)
            pwd = SimpleNamespace(getpwuid=lambda _: account)
            lock = Path(directory) / ".cache" / "electron2d-wsl-build.lock"
            command = ["make", "-j1", "build_libs"]
            with (patch.object(wsl_build, "is_wsl", return_value=True),
                  patch.object(wsl_build.os, "geteuid", return_value=1000, create=True),
                  patch.dict(sys.modules, {"pwd": pwd}),
                  patch.object(wsl_build.subprocess, "run") as run):
                self.assertEqual(wsl_build.jobs(4), "1")
                run.return_value = subprocess.CompletedProcess(command, 0)
                wsl_build.run_build(command, cwd="first checkout")
                run.assert_called_once_with([
                    "flock", "--no-fork", "--nonblock", "--conflict-exit-code", "75", str(lock),
                    "timeout", "--kill-after=15s", "45m", *command,
                ], cwd="first checkout")

                for status, message in [(75, "slot is busy"), (124, "was interrupted"), (137, "was interrupted")]:
                    run.return_value = subprocess.CompletedProcess(command, status)
                    with self.assertRaisesRegex(SystemExit, message):
                        wsl_build.run_build(command, cwd="second checkout")
                    self.assertIn(str(lock), run.call_args.args[0])

    def test_other_hosts_keep_the_original_command_and_parallelism(self):
        with (patch.object(wsl_build, "is_wsl", return_value=False),
              patch.object(wsl_build.subprocess, "run") as run):
            command = ["make", "-j" + wsl_build.jobs(4), "build_libs"]
            wsl_build.run_build(command, cwd="native build")
            run.assert_called_once_with(["make", "-j4", "build_libs"], cwd="native build", check=True)


if __name__ == "__main__":
    unittest.main()
