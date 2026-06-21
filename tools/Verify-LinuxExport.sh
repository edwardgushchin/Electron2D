#!/usr/bin/env bash
#
# Electron2D
# MIT License
# Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
# SPDX-License-Identifier: MIT
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
template_root="$repo_root/templates/electron2d-empty"
package_output="$repo_root/.temp/linux-export-package"
work_root="$repo_root/.temp/linux-export-check"
created_project="$work_root/Electron2D.Empty"
project_path="$created_project/Electron2D.Empty.csproj"
packages_root="$work_root/.nuget-packages"
nuget_config="$work_root/NuGet.Config"

if [[ ! -d "$template_root" ]]; then
    echo "Template directory templates/electron2d-empty was not found." >&2
    exit 1
fi

rm -rf "$package_output" "$work_root"
mkdir -p "$package_output" "$created_project"

dotnet restore "$repo_root/src/Electron2D/Electron2D.csproj"
dotnet pack "$repo_root/src/Electron2D/Electron2D.csproj" --no-restore -o "$package_output"

cp -R "$template_root/." "$created_project/"
rm -rf "$created_project/.template.config"

cat > "$nuget_config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="electron2d-local" value="$package_output" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

dotnet restore "$project_path" --configfile "$nuget_config" --packages "$packages_root" --runtime linux-x64

expected_scene_output="Electron2D empty scene loaded: scenes/main.scene.json"
expected_lifecycle_output="Electron2D C# script lifecycle: _EnterTree,_Ready"
expected_service_output="Electron2D C# script services: tree=True,text=True"

for configuration in Debug Release; do
    publish_output="$work_root/publish-$configuration"
    dotnet publish "$project_path" \
        --no-restore \
        --configuration "$configuration" \
        --runtime linux-x64 \
        --self-contained true \
        --output "$publish_output"

    executable_path="$publish_output/Electron2D.Empty"
    if [[ ! -f "$executable_path" ]]; then
        echo "Linux export executable was not found: $executable_path" >&2
        exit 1
    fi

    chmod +x "$executable_path"

    project_settings_path="$publish_output/project.e2d.json"
    if [[ ! -f "$project_settings_path" ]]; then
        echo "Linux export project settings were not found: $project_settings_path" >&2
        exit 1
    fi

    scene_path="$publish_output/scenes/main.scene.json"
    if [[ ! -f "$scene_path" ]]; then
        echo "Linux export reference scene was not found: $scene_path" >&2
        exit 1
    fi

    run_output="$("$executable_path")"
    for expected_output in "$expected_scene_output" "$expected_lifecycle_output" "$expected_service_output"; do
        if [[ "$run_output" != *"$expected_output"* ]]; then
            echo "$run_output"
            echo "Linux export run output does not contain expected line: $expected_output" >&2
            exit 1
        fi
    done
done

echo "Linux export verification passed."
