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
            raise RuntimeError("Contract checks failed:\n" + log[log.index(marker):][:12000])
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
    if packages:
        raise ValueError(f"The managed engine must not restore native packages: {packages}")
    with (output / "Electron2D.dll").open("rb") as assembly:
        if assembly.read(2) != b"MZ":
            raise ValueError("Missing managed PE assembly")
    if ET.parse(output / "Electron2D.xml").findtext("./assembly/name") != "Electron2D":
        raise ValueError("Wrong XML documentation assembly")
    found = {path.relative_to(output).as_posix() for path in output.rglob("*") if path.is_file() and
             (".so" in path.name or path.suffix in {".dylib", ".a"} or
              path.suffix == ".dll" and path.name != "Electron2D.dll") }
    if found:
        raise ValueError(f"The managed engine must not include native payloads: {found}")
    print(f"{rid}: library, XML, target profile, managed-only dependency/payload selection passed; execution checked separately ({row['suite']}).")


if __name__ == "__main__":
    check(sys.argv[1], Path(sys.argv[2]))
