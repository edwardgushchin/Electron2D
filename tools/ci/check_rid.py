"""Check built target artifacts without claiming execution on another platform."""

import json
from pathlib import Path
import struct
import sys
import xml.etree.ElementTree as ET

from rids import matrix
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "native"))
import package as native_package


def check_result(log, token):
    marker = "RESULT " + token + " "
    for line in log.splitlines():
        if marker in line:
            status = line.split(marker, 1)[1]
            if status == "PASS":
                return True
            raise RuntimeError("Contract checks failed: " + status)
    return False


def check_notices(read):
    notices = Path(__file__).resolve().parents[2] / "licence"
    for source in notices.iterdir():
        if source.is_file() and source.name != "ReferenceData-LICENSE.txt":
            try:
                data = read(source.name)
            except (KeyError, FileNotFoundError) as error:
                raise RuntimeError(f"Missing bundle notice: {source.name}") from error
            if data != source.read_bytes():
                raise RuntimeError(f"Changed bundle notice: {source.name}")
    try:
        read("ReferenceData-LICENSE.txt")
    except (KeyError, FileNotFoundError):
        return
    raise RuntimeError("Source-only reference-data notice must not be in an application bundle.")


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
    native_packages = {name for name in packages if name.startswith("Electron2D.Native.")}
    expected_native = {"Electron2D.Native." + platform for platform in ("Linux", "MacOS", "Windows")} if row["platform"] in {"Windows", "Linux", "MacOS"} else {"Electron2D.Native.Android"} if row["platform"] == "Android" else set()
    if native_packages != expected_native:
        raise ValueError(f"Wrong private native dependency selection: {native_packages} != {expected_native}")
    with (output / "Electron2D.dll").open("rb") as assembly:
        if assembly.read(2) != b"MZ":
            raise ValueError("Missing managed PE assembly")
    if ET.parse(output / "Electron2D.xml").findtext("./assembly/name") != "Electron2D":
        raise ValueError("Wrong XML documentation assembly")
    desktop = {platform: set(native_package.LIBRARIES[platform]) for platform in ("Linux", "MacOS", "Windows")}
    private = set().union(*desktop.values())
    found = {path.relative_to(output).as_posix() for path in output.rglob("*") if path.is_file() and path.name in private}
    wanted = {f"runtimes/{rid}/native/{name}" for name in desktop.get(row["platform"], ())}
    if found != wanted:
        raise ValueError(f"Wrong private native payload: {found} != {wanted}")
    for file in found:
        if row["platform"] == "Windows":
            native_package.windows_exports(output / file, rid, Path(file).name)
            continue
        header = (output / file).read_bytes()[:20]
        if row["platform"] == "MacOS":
            machine = 0x01000007 if rid == "osx-x64" else 0x0100000c
            if header[:4] != b"\xcf\xfa\xed\xfe" or struct.unpack_from("<I", header, 4)[0] != machine:
                raise ValueError(f"Wrong native architecture: {file}")
        else:
            machine = 62 if rid == "linux-x64" else 183
            if header[:6] != b"\x7fELF\x02\x01" or struct.unpack_from("<H", header, 18)[0] != machine:
                raise ValueError(f"Wrong native architecture: {file}")
    print(f"{rid}: library, XML, target profile, SDL selection and private native payload passed; execution checked separately ({row['suite']}).")


if __name__ == "__main__":
    check(sys.argv[1], Path(sys.argv[2]))
