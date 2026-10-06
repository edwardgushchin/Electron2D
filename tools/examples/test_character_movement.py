"""Check RID selection and packaged icon references without executing foreign hosts."""
import json
from pathlib import Path
import plistlib
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
GAME = ROOT / "examples/CharacterMovement"
rows = json.loads((ROOT / "tools/ci/rids.json").read_text())
assert len(rows) == 18
for row in rows:
    result = subprocess.run([
        "dotnet", "msbuild", str(GAME / "CharacterMovement.csproj"),
        f"-p:RuntimeIdentifier={row['rid']}", "-p:MSBuildEnableWorkloadResolver=false",
        "-getProperty:TargetFramework,Electron2DConsumerPlatform,AppIcon,GameMacBundle",
    ], cwd=ROOT, check=True, capture_output=True, text=True)
    props = json.loads(result.stdout)["Properties"]
    assert props["TargetFramework"] == row["framework"], (row, props)
    assert props["Electron2DConsumerPlatform"] == row["platform"], (row, props)
    if row["platform"] in ("iOS", "tvOS"):
        assert props["AppIcon"] == "AppIcon", props
    assert (props["GameMacBundle"] == "true") == (row["platform"] == "MacOS"), props

manifest = ET.parse(GAME / "Platforms/Android/AndroidManifest.xml").getroot()
android = "{http://schemas.android.com/apk/res/android}"
app = manifest.find("application")
assert app.attrib[android + "icon"] == "@drawable/app_icon"
assert app.attrib[android + "banner"] == "@drawable/tv_banner"
categories = {item.attrib[android + "name"] for item in app.findall("activity/intent-filter/category")}
assert {"android.intent.category.LAUNCHER", "android.intent.category.LEANBACK_LAUNCHER"} <= categories
for path in (GAME / "Platforms/Apple").rglob("Contents.json"):
    data = json.loads(path.read_text())
    for row in data.get("images", []) + data.get("layers", []) + data.get("assets", []):
        assert (path.parent / row["filename"]).is_file() or (path.parent / row["filename"]).is_dir(), path
with (GAME / "Platforms/MacOS/Info.plist").open("rb") as stream:
    mac = plistlib.load(stream)
assert mac["CFBundleExecutable"] == "CharacterMovement"
assert mac["CFBundleIconFile"] == "Electron2D.icns"
assert not (ROOT / "examples/HostExample").exists()
print("CharacterMovement: all 18 RID/framework/package selectors and platform icon references passed; no foreign app execution.")
