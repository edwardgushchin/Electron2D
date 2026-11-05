"""Check migration from missing MSVC/Ninja header dependencies in a developer shell."""

from pathlib import Path
import subprocess
import sys
import tempfile


def check(build):
    cache = dict(line.split("=", 1) for line in (build / "CMakeCache.txt").read_text().splitlines()
                 if "=" in line and not line.startswith(("//", "#")))
    cmake = cache["CMAKE_COMMAND:INTERNAL"]
    ninja = cache["CMAKE_MAKE_PROGRAM:FILEPATH"]
    compiler = cache["CMAKE_CXX_COMPILER:FILEPATH"]
    helper = Path(__file__).with_name("msvc-dependencies.cmake").resolve().as_posix()
    with tempfile.TemporaryDirectory(prefix="e2d-msvc-deps-") as directory:
        root = Path(directory)
        (root / "main.cpp").write_text('#include "header.h"\nint main() { return HEADER_VALUE; }\n')
        (root / "header.h").write_text("#define HEADER_VALUE 7\n")
        (root / "CMakeLists.txt").write_text('''cmake_minimum_required(VERSION 3.24)
project(IncludeDependencies LANGUAGES CXX)
if(FIX_PREFIX)
  include("''' + helper + '''")
else()
  set(CMAKE_CL_SHOWINCLUDES_PREFIX "Invalid: include prefix: ")
endif()
add_executable(probe main.cpp)
''')

        def configure(fixed):
            subprocess.run([cmake, "-S", str(root), "-B", str(root / "build"), "-G", "Ninja",
                            "-DCMAKE_BUILD_TYPE=Release", "-DCMAKE_MAKE_PROGRAM=" + ninja,
                            "-DCMAKE_CXX_COMPILER=" + compiler, "-DFIX_PREFIX=" + fixed], check=True,
                           stdout=subprocess.PIPE, stderr=subprocess.STDOUT)

        def compile_and_read():
            subprocess.run([cmake, "--build", str(root / "build")], check=True,
                           stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            return subprocess.run([str(root / "build/probe.exe")], check=False).returncode

        configure("OFF")
        assert compile_and_read() == 7
        (root / "header.h").write_text("#define HEADER_VALUE 13\n")
        configure("ON")
        assert compile_and_read() == 13, "Existing empty dependency records retained a stale object"
        dependencies = subprocess.check_output([ninja, "-C", str(root / "build"), "-t", "deps"], text=True)
        assert "header.h" in dependencies
        (root / "header.h").write_text("#define HEADER_VALUE 17\n")
        assert compile_and_read() == 17, "A changed header did not rebuild its consumer"
        object_file = root / "build/CMakeFiles/probe.dir/main.cpp.obj"
        modified = object_file.stat().st_mtime_ns
        configure("ON")
        assert compile_and_read() == 17
        assert object_file.stat().st_mtime_ns == modified, "Reconfiguration needlessly rebuilt the object"
    print("MSVC/Ninja: stale dependency migration, localized includes, header invalidation and reconfiguration passed.")


if __name__ == "__main__":
    check(Path(sys.argv[1]))
