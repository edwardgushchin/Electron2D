#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 v<release>" >&2
    exit 2
fi

root=$(cd "$(dirname "$0")/.." && pwd)
scratch=$(mktemp -d)
trap 'rm -rf "$scratch"' EXIT
git clone --quiet --depth 1 --branch "$1" https://github.com/edwardgushchin/SDL3-CS.git "$scratch/upstream"
revision=$(git -C "$scratch/upstream" rev-parse HEAD)
git -C "$scratch/upstream" archive HEAD SDL3-CS/SDL SDL3-CS/ShaderCross SDL3-CS/Image LICENSE | tar -x -C "$scratch"
mv "$scratch/SDL3-CS/SDL" "$scratch/SDL"
mv "$scratch/SDL3-CS/ShaderCross" "$scratch/ShaderCross"
mv "$scratch/SDL3-CS/Image" "$scratch/Image"
find "$scratch/SDL" "$scratch/ShaderCross" "$scratch/Image" -name '*.cs' -print0 | xargs -0 perl -pi -e 's/^public /internal /'
sed -i 's/private const string SDLLibrary = "SDL3";/private const string SDLLibrary = global::Electron2D.NativeLibraries.SDLLibrary;/' "$scratch/SDL/SDL.cs"
sed -i 's/private const string ImageLibrary = "SDL3_image";/private const string ImageLibrary = global::Electron2D.NativeLibraries.SDLImageLibrary;/' "$scratch/Image/Image.cs"
sed -i 's/private const string ShaderCrossLibrary = "SDL3_shadercross";/private const string ShaderCrossLibrary = global::Electron2D.NativeLibraries.SDLShaderCrossLibrary;/' "$scratch/ShaderCross/Mixer.cs"
sed -i -E 's/(private static (unsafe )?(AppInitNative|AppIterateNative|AppEventNative|AppQuitNative|MainNative) [A-Za-z]+Function) = /\1 => /' "$scratch/SDL/Basics/main/PInvoke.cs"
sed -i -E 's/(private static partial|private delegate) ulong (SDL_(Read|Write)IO|ReadIONative|WriteIONative)/\1 UIntPtr \2/' "$scratch/SDL/File and IO Abstractions/iostream/PInvoke.cs"
sed -i '1s/^\xEF\xBB\xBF/#pragma warning disable CS0649 \/\/ Native SDL initializes this callback table.\n/' \
    "$scratch/SDL/File and IO Abstractions/storage/StorageInterface.cs"
mv "$scratch/LICENSE" "$scratch/SDL3-CS-LICENSE.txt"
cat > "$scratch/UPSTREAM.md" <<EOF
# SDL3-CS core, shadercross and image binding source

Source: https://github.com/edwardgushchin/SDL3-CS
Release: $1
Commit: $revision

The complete upstream SDL3-CS/SDL, SDL3-CS/ShaderCross and SDL3-CS/Image trees are compiled into Electron2D.dll.
Local adaptation makes top-level binding types internal and suppresses CS0649 on
the native-initialized storage callback table. Core/Image/ShaderCross library
constants use the engine resolver's names, static executable symbols on iOS/tvOS
or generated archive module tables on Web.
Application-owned entry-point delegates are lazy so unrelated SDL calls do not
retain nonexistent host exports during static application linking.
Native SDL IO read/write results use pointer-sized size_t on both 32-bit and
64-bit hosts; the managed binding convenience result remains ulong.
The upstream license is retained.
Refresh with tools/update-sdl3-cs.sh and a release tag, then inspect the diff and
run the engine, test, coverage, and native example checks.
EOF
rm -rf "$root/src/Vendor/SDL3-CS"
mkdir -p "$root/src/Vendor/SDL3-CS"
mv "$scratch/SDL" "$scratch/ShaderCross" "$scratch/Image" "$scratch/UPSTREAM.md" "$root/src/Vendor/SDL3-CS/"
mv "$scratch/SDL3-CS-LICENSE.txt" "$root/licence/SDL3-CS-LICENSE.txt"
echo "Imported $1 ($revision)"
