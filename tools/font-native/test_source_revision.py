"""Reject source overrides that do not match the producer's pinned revision."""

from pathlib import Path
import subprocess
import tempfile
import unittest


class SourceRevisionTests(unittest.TestCase):
    def test_clean_changed_wrong_and_unversioned_sources(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "source"
            empty_hooks = root / "hooks"
            empty_hooks.mkdir()
            subprocess.run(["git", "-c", f"init.templateDir={empty_hooks}", "init", "-q", str(source)], check=True)
            tracked = source / "input.txt"
            tracked.write_text("original\n")
            subprocess.run(["git", "-C", str(source), "add", "input.txt"], check=True)
            subprocess.run(["git", "-C", str(source), "-c", "user.name=Fixture", "-c",
                            "user.email=fixture@example.invalid", "-c", "commit.gpgsign=false",
                            "-c", f"core.hooksPath={empty_hooks}", "commit", "-qm", "fixture"], check=True)
            revision = subprocess.check_output(["git", "-C", str(source), "rev-parse", "HEAD"], text=True).strip()
            helper = Path(__file__).with_name("verify-source.cmake").as_posix()

            def verify(path, expected):
                script = root / "check.cmake"
                script.write_text(f'set(sample_SOURCE_DIR "{path.as_posix()}")\n'
                                  f'include("{helper}")\nverify_source(Sample "{expected}")\n')
                return subprocess.run(["cmake", "-P", str(script)], cwd=root, capture_output=True, text=True)

            self.assertEqual(verify(source, revision).returncode, 0)
            tracked.write_text("changed\n")
            result = verify(source, revision)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("source has tracked changes", result.stderr)
            tracked.write_text("original\n")
            result = verify(source, "0" * 40)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("differs from the required", " ".join(result.stderr.split()))
            result = verify(root, revision)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("differs from the required", " ".join(result.stderr.split()))


if __name__ == "__main__":
    unittest.main()
