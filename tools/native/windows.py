"""Inspect native PE libraries and link against the existing SDL Windows DLL."""

import argparse
from pathlib import Path
import platform
import re
import subprocess

import pefile

MACHINES = {"win-x86": 0x014c, "win-x64": 0x8664, "win-arm64": 0xaa64}


def inspect(path, rid, name):
    try:
        with pefile.PE(str(path), fast_load=True) as binary:
            if binary.FILE_HEADER.Machine != MACHINES[rid] or not binary.FILE_HEADER.Characteristics & 0x2000:
                raise ValueError(f"Wrong native PE architecture or DLL profile: {path}")
            if binary.OPTIONAL_HEADER.DATA_DIRECTORY[14].VirtualAddress:
                raise ValueError(f"A private native package must not contain a managed assembly: {path}")
            binary.parse_data_directories(directories=[0, 1])
            exports = binary.DIRECTORY_ENTRY_EXPORT
            if exports.name.decode("ascii").lower() != name.lower():
                raise ValueError(f"Wrong PE export identity: {path}")
            return {symbol.name.decode("ascii") for symbol in exports.symbols if symbol.name}, {
                entry.dll.decode("ascii").lower() for entry in getattr(binary, "DIRECTORY_ENTRY_IMPORT", [])}
    except (pefile.PEFormatError, AttributeError, UnicodeError, IndexError) as error:
        raise ValueError(f"Invalid native PE library: {path}") from error


def sdl_import_library(path, rid, output):
    exports, _ = inspect(path, rid, "SDL3.dll")
    if not {"SDL_Init", "SDL_OpenAudioDeviceStream", "SDL_PutAudioStreamData"} <= exports or any(
            not re.fullmatch(r"[A-Za-z_][A-Za-z_0-9]*", name) for name in exports):
        raise ValueError("The pinned SDL library has an invalid native export table")
    output.mkdir(parents=True, exist_ok=True)
    definition = output / "SDL3.def"
    definition.write_text("LIBRARY SDL3.dll\nEXPORTS\n" + "\n".join(sorted(exports)) + "\n", encoding="ascii")
    library = output / "SDL3.lib"
    if platform.system() == "Windows":
        subprocess.run(["lib", "/nologo", "/def:" + str(definition), "/out:" + str(library),
                        "/machine:" + rid.removeprefix("win-")], check=True)
    else:
        machine = {"win-x86": "i386", "win-x64": "i386:x86-64", "win-arm64": "arm64"}[rid]
        subprocess.run(["llvm-dlltool", "-m", machine, "-d", str(definition), "-l", str(library)], check=True)
    print(f"{rid}: checked SDL3.dll exports and generated matching import library without rebuilding SDL")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("rid", choices=MACHINES)
    parser.add_argument("sdl", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    sdl_import_library(args.sdl.resolve(), args.rid, args.output.resolve())
