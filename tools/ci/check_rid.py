"""Check built target artifacts without claiming execution on another platform."""

import json
from pathlib import Path
import struct
import sys
import xml.etree.ElementTree as ET

from rids import matrix


def check(rid, output):
    row = next(item for item in matrix() if item["rid"] == rid)
    profile = json.loads((output / "profile.json").read_text())
    expected = {"RuntimeIdentifier": rid, "TargetFramework": row["framework"], "Electron2DNativePlatform": row["platform"]}
    if profile["Properties"] != expected:
        raise ValueError(f"Wrong evaluated target profile: {profile['Properties']}")
    packages = {item["Identity"] for item in profile["Items"]["PackageReference"]}
    sdl = {name for name in packages if name.startswith("SDL3-CS.")}
    platforms = {"Windows", "Linux", "MacOS"} if row["platform"] in {"Windows", "Linux", "MacOS"} else {row["platform"]}
    wanted = {f"SDL3-CS.{platform}{suffix}" for platform in platforms for suffix in ("", ".Image", ".Shadercross")} if row["platform"] != "Web" else set()
    if sdl != wanted:
        raise ValueError(f"Wrong SDL dependency selection: {sdl} != {wanted}")
    with (output / "Electron2D.dll").open("rb") as assembly:
        if assembly.read(2) != b"MZ":
            raise ValueError("Missing managed PE assembly")
    if ET.parse(output / "Electron2D.xml").findtext("./assembly/name") != "Electron2D":
        raise ValueError("Wrong XML documentation assembly")
    private = {"libElectron2DTextBreak.so", "libFAudio.so.0", "libElectron2DENet.so"}
    found = {path.relative_to(output).as_posix() for path in output.rglob("*") if path.is_file() and path.name in private}
    wanted = {f"runtimes/{rid}/native/{name}" for name in private} if row["platform"] == "Linux" else set()
    if found != wanted:
        raise ValueError(f"Wrong private native payload: {found} != {wanted}")
    for file in found:
        header = (output / file).read_bytes()[:20]
        machine = 62 if rid == "linux-x64" else 183
        if header[:6] != b"\x7fELF\x02\x01" or struct.unpack_from("<H", header, 18)[0] != machine:
            raise ValueError(f"Wrong native architecture: {file}")
    print(f"{rid}: library, XML, target profile, SDL selection and private native payload passed; execution checked separately ({row['suite']}).")


if __name__ == "__main__":
    check(sys.argv[1], Path(sys.argv[2]))
