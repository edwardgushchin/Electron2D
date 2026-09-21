#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
coverage_tmp=$(mktemp -d)
trap 'rm -rf "$coverage_tmp"' EXIT

dotnet run --project tools/coverage/Exporter.csproj -c Release -- "$coverage_tmp/electron2d.json"
cmp docs/coverage/data/electron2d.json "$coverage_tmp/electron2d.json"
python3 -B tools/coverage/test_render.py
python3 -B tools/coverage/render.py --check

if [[ $# -gt 0 ]]; then
    python3 -B tools/coverage/godot_xml.py "$1" "$coverage_tmp/godot.json"
    cmp docs/coverage/data/godot-4.7.2.json "$coverage_tmp/godot.json"
fi
