"""Exercise explicit WebP selection against newer packaged build/publish assets."""

import unittest
from pathlib import Path
import subprocess
import tempfile
from xml.sax.saxutils import escape
import test_audio_delivery


class WebPDeliveryTests(test_audio_delivery.AudioDeliveryTests):
    property_name = "Electron2DWebPNativeLibrary"
    linux_name = "libElectron2DWebP.so"
    macos_name = "libElectron2DWebP.dylib"

    def test_publish_disambiguates_only_the_selected_destination(self):
        for rid in ("linux-x64", "linux-arm64", "osx-x64", "osx-arm64"):
            for separator in ("/", "\\"):
                with self.subTest(rid=rid, separator=separator), tempfile.TemporaryDirectory(prefix="webp selection ") as directory:
                    root = Path(directory)
                    source = root / "selected library"
                    source.write_bytes(b"selected")
                    destination = separator.join(("runtimes", rid, "native", self.macos_name if rid.startswith("osx-") else self.linux_name))
                    project = root / "Selection.proj"
                    project.write_text('<Project><PropertyGroup>'
                        f'<RuntimeIdentifier>{rid}</RuntimeIdentifier><{self.property_name}>{escape(str(source))}</{self.property_name}>'
                        '</PropertyGroup><ItemGroup>'
                        f'<ResolvedFileToPublish Include="{escape(str(source))}" RelativePath="{destination}" />'
                        f'<ResolvedFileToPublish Include="packaged-library" RelativePath="{destination}" />'
                        f'<ResolvedFileToPublish Include="unrelated-library" RelativePath="assets/{self.linux_name}" />'
                        '</ItemGroup>'
                        f'<Import Project="{escape(str(test_audio_delivery.ROOT / "tools/native-consumer.props"))}" />'
                        '<Target Name="_HandleFileConflictsForPublish">'
                        '<WriteLinesToFile File="selected.txt" Lines="@(ResolvedFileToPublish)" Overwrite="true" />'
                        '</Target></Project>')
                    result = subprocess.run(["dotnet", "msbuild", str(project), "-nologo", "-target:_HandleFileConflictsForPublish"], capture_output=True, text=True, cwd=root)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertEqual((root / "selected.txt").read_text().splitlines(), [str(source), "unrelated-library"])


if __name__ == "__main__":
    unittest.main()
