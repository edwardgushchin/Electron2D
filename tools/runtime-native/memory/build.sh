#!/usr/bin/env bash
set -euo pipefail

if [[ $# != 4 || $(uname -sm) != 'Linux x86_64' ]]; then
    echo "Usage on Linux x64: bash $0 aot|clr RUNTIME_ARCHIVE SDK_ROOT OUTPUT" >&2
    echo "For clr, set E2D_MEMORY_NATIVE to the absolute path to the packaged libElectron2D.so." >&2
    exit 2
fi

mode=$1
archive=$(realpath "$2")
sdk=$(realpath "$3")
output=$(realpath -m "$4")
repo=$(cd "$(dirname "$0")/../../.." && pwd)
probe="$repo/tools/physics"
slot="$repo/tools/native/wsl_build.py"
version=10.0.14
sdk_runtime="$sdk/shared/Microsoft.NETCore.App/$version"

if [[ ! -x "$sdk/dotnet" || ! -d "$sdk_runtime" ]]; then
    echo "Select an SDK root containing Microsoft.NETCore.App $version." >&2
    exit 2
fi

case "$mode" in
    aot) ;;
    clr)
        engine=$(realpath -e "${E2D_MEMORY_NATIVE:?Select the packaged Linux x64 engine module}")
        if [[ $(patchelf --print-soname "$engine") != libElectron2D.so ]]; then
            echo 'The packaged module must have SONAME libElectron2D.so.' >&2
            exit 2
        fi
        ;;
    *) exit 2 ;;
esac

if [[ -e "$output/$mode" ]]; then
    echo "Choose a fresh output for $mode." >&2
    exit 2
fi

printf '%s  %s\n' ce5e2500cd691a2077cff9c58efcc341c8c0c15f31a0272341628c482a85bdb1 "$archive" | sha256sum --check
mkdir -p "$output"
work=$(mktemp -d "${TMPDIR:-/var/tmp}/e2d-managed-memory-XXXXXX")
trap 'rm -rf -- "$work"' EXIT
python3 -c 'import sys, tarfile; tarfile.open(sys.argv[1]).extractall(sys.argv[2], filter="data")' "$archive" "$work"
runtime="$work/runtime-$version"
native="$runtime/artifacts/obj/coreclr/linux.x64.Release"
flags='-DFEATURE_EVENTSOURCE_XPLAT=0 -DFEATURE_PERFTRACING=1'
stage="$work/stage"
mkdir "$stage"
cp "$runtime/LICENSE.TXT" "$runtime/THIRD-PARTY-NOTICES.TXT" "$stage"

if [[ "$mode" == aot ]]; then
    python3 -B "$probe/aot-memory-probe/patch_runtime.py" "$runtime" "$stage/runtime.patch" "$version"
    python3 -B "$slot" -- bash "$runtime/src/coreclr/build-runtime.sh" -release -arch x64 -component nativeaot \
        -numproc 1 -skipgenerateversion -ninja -cmakeargs "$flags"
    cp "$native/nativeaot/Runtime/Full/libRuntime.WorkstationGC.a" "$stage"
    python3 -B "$slot" -- c++ -std=c++17 -O2 -Wall -Wextra -Werror -fPIC -pthread -c \
        "$probe/aot-memory-probe/producer.cpp" -o "$stage/producer.o"
else
    producer="$stage/libElectron2DAOTMemory.so"
    python3 -B "$slot" -- c++ -std=c++17 -O2 -Wall -Wextra -Werror -fPIC -shared -pthread \
        "$probe/managed-live-probe/profiler.cpp" "$probe/clr-memory-probe/producer.cpp" "$engine" \
        -Wl,-soname,libElectron2DAOTMemory.so -o "$producer"
    python3 -B "$probe/clr-memory-probe/patch_runtime.py" "$runtime" "$stage/runtime.patch" "$version"
    export LD_LIBRARY_PATH="$stage:$(dirname "$engine")${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
    python3 -B "$slot" -- bash "$runtime/src/coreclr/build-runtime.sh" -release -arch x64 -component coreclr \
        -numproc 1 -skipgenerateversion -ninja -cmakeargs "$flags -DE2D_MEMORY_PRODUCER=$producer"
    python3 -B "$slot" -- cmake --build "$native" --target clrjit --parallel 1
    # The pinned upstream build tasks resolve an older transitive PKCS package.
    python3 - "$runtime" <<'PY'
from pathlib import Path
import sys

for name in ("Crossgen2Tasks", "installer.tasks"):
    project = Path(sys.argv[1]) / "src/tasks" / name / f"{name}.csproj"
    original = project.read_text()
    project.write_text(original.replace("</Project>", """
  <ItemGroup>
    <PackageReference Include="System.Security.Cryptography.Pkcs" Version="[9.0.21]" PrivateAssets="all" />
  </ItemGroup>
</Project>"""))
PY
    use_installed_dotnet_cli=false NUGET_PACKAGES="$work/nuget" \
        python3 -B "$slot" -- bash "$runtime/build.sh" -subset clr.corelib -c Release -rc Release -m:1 \
        /p:FeatureXplatEventSource=false /p:UseSharedCompilation=false /nr:false

    cp "$sdk/dotnet" "$stage"
    cp -a "$sdk/host" "$stage"
    shared="$stage/shared/Microsoft.NETCore.App/$version"
    mkdir -p "$shared"
    cp -a "$sdk_runtime/." "$shared"
    cp "$native/dlls/mscoree/coreclr/libcoreclr.so" "$native/jit/libclrjit.so" "$shared"
    patchelf --remove-rpath "$shared/libcoreclr.so"
    cp "$runtime/artifacts/bin/coreclr/linux.x64.Release/IL/System.Private.CoreLib.dll" "$shared"
fi

mv -T -- "$stage" "$output/$mode"
