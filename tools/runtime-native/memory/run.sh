#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 3 || $(uname -sm) != 'Linux x86_64' ]]; then
    echo "Usage on Linux x64: bash $0 aot|clr PROFILE_ROOT APP [ARG ...]" >&2
    exit 2
fi

mode=$1
profile=$(realpath "$2")
app=$(realpath -e "$3")
shift 3
native="$(dirname "$app")/runtimes/linux-x64/native"
if [[ ! -f "$native/libElectron2D.so" ]]; then
    echo "The published application is missing its packaged libElectron2D.so." >&2
    exit 2
fi

export DOTNET_gcConcurrent=0 DOTNET_gcServer=0
case "$mode" in
    aot)
        export LD_LIBRARY_PATH="$native"
        exec "$app" "$@"
        ;;
    clr)
        clr="$profile/clr"
        producer="$clr/libElectron2DAOTMemory.so"
        if [[ ! -x "$clr/dotnet" || ! -f "$producer" ]]; then
            echo "The selected profile is missing its instrumented CoreCLR or allocation producer." >&2
            exit 2
        fi
        export LD_LIBRARY_PATH="$native:$clr"
        export CORECLR_ENABLE_PROFILING=1 CORECLR_PROFILER='{4CB0917C-5E03-4787-83BC-98C86DC7BE4C}'
        export CORECLR_PROFILER_PATH="$producer"
        exec "$clr/dotnet" exec --fx-version 10.0.14 "$app" "$@"
        ;;
    *) exit 2 ;;
esac
