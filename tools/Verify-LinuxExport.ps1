<#
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
#>
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot 'tools/Verify-LinuxExport.sh'

if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw 'Linux export bash verifier tools/Verify-LinuxExport.sh was not found.'
}

$isLinuxHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Linux)
$isWindowsHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows)

if ($isLinuxHost) {
    bash $scriptPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    exit 0
}

if ($isWindowsHost) {
    $wslCommand = Get-Command wsl -ErrorAction SilentlyContinue
    if ($null -eq $wslCommand) {
        throw 'Linux export verification requires Linux host or WSL on Windows.'
    }

    $fullRepoRoot = [System.IO.Path]::GetFullPath($repoRoot)
    if ($fullRepoRoot.Length -lt 3 -or $fullRepoRoot[1] -ne ':') {
        throw "Failed to map repository path into WSL: $fullRepoRoot"
    }

    $drive = [char]::ToLowerInvariant($fullRepoRoot[0])
    $relativeRoot = $fullRepoRoot.Substring(3).Replace('\', '/')
    $repoRootLinux = "/mnt/$drive/$relativeRoot"

    $scriptPathLinux = "$repoRootLinux/tools/Verify-LinuxExport.sh"
    & wsl bash $scriptPathLinux
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    exit 0
}

throw 'Linux export verification requires Linux host or WSL on Windows.'
