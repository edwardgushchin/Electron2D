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
$tasksPath = Join-Path $repoRoot 'TASKS.md'

if (-not (Test-Path -LiteralPath $tasksPath)) {
    throw "TASKS.md was not found: $tasksPath"
}

$tasksText = [System.IO.File]::ReadAllText($tasksPath, [System.Text.Encoding]::UTF8)
$errors = New-Object System.Collections.Generic.List[string]

if ($tasksText -match '(?m)^## T-\d{4} \[!\]') {
    $errors.Add('Task headers must not use [!]. Use [ ] plus a blocked state instead.')
}

$taskMatches = [regex]::Matches($tasksText, '(?m)^## (T-\d{4}) \[[ x]\] [^\r\n]+')
$sections = @()
for ($index = 0; $index -lt $taskMatches.Count; $index++) {
    $match = $taskMatches[$index]
    $nextStart = if ($index + 1 -lt $taskMatches.Count) { $taskMatches[$index + 1].Index } else { $tasksText.Length }
    $sections += [pscustomobject]@{
        Id = $match.Groups[1].Value
        Heading = $match.Value
        Text = $tasksText.Substring($match.Index, $nextStart - $match.Index)
    }
}

$knownTaskIds = New-Object System.Collections.Generic.HashSet[string]
foreach ($section in $sections) {
    [void]$knownTaskIds.Add($section.Id)
}

$completedRoot = Join-Path $repoRoot 'completed-tasks'
if (Test-Path -LiteralPath $completedRoot) {
    foreach ($file in Get-ChildItem -LiteralPath $completedRoot -Recurse -File -Filter '*.md') {
        $completedText = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        foreach ($idMatch in [regex]::Matches($completedText, 'T-\d{4}')) {
            [void]$knownTaskIds.Add($idMatch.Value)
        }
    }
}

$allowedStates = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($state in @('open', 'in progress', 'blocked', 'ready for acceptance', 'tracking', 'accepted', 'closed')) {
    [void]$allowedStates.Add($state)
}

$noneDependency = -join ([char]0x043d, [char]0x0435, [char]0x0442)

foreach ($section in $sections) {
    $statusMatch = [regex]::Match($section.Text, '(?m)^- \u0421\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435:\s*(.+?)\s*$')
    if (-not $statusMatch.Success) {
        $errors.Add("$($section.Id) is missing a state line.")
    }
    else {
        $state = $statusMatch.Groups[1].Value.Trim()
        if (-not $allowedStates.Contains($state)) {
            $errors.Add("$($section.Id) has unsupported state ""$state"".")
        }
    }

    $dependencyMatch = [regex]::Match($section.Text, '(?m)^- \u0417\u0430\u0432\u0438\u0441\u0438\u043c\u043e\u0441\u0442\u0438:\s*(.+?)\s*$')
    if ($dependencyMatch.Success) {
        $dependencyText = $dependencyMatch.Groups[1].Value.Trim()
        if ($dependencyText -ne $noneDependency -and $dependencyText -notmatch '^(T-\d{4})(,\s*T-\d{4})*$') {
            $errors.Add("$($section.Id) has a non-machine-readable dependency line: ""$dependencyText"". Use only task ids separated by commas, or ""нет"".")
        }

        foreach ($dependency in [regex]::Matches($dependencyText, 'T-\d{4}')) {
            if (-not $knownTaskIds.Contains($dependency.Value)) {
                $errors.Add("$($section.Id) depends on unknown task $($dependency.Value).")
            }
        }
    }

    if ($statusMatch.Success) {
        $state = $statusMatch.Groups[1].Value.Trim()
        if ($state -eq 'open' -or $state -eq 'in progress') {
            $criteriaMatch = [regex]::Match($section.Text, '(?s)### \u041a\u0440\u0438\u0442\u0435\u0440\u0438\u0438 \u043f\u0440\u0438\u0451\u043c\u043a\u0438\s*(.*?)(?:\r?\n### |\z)')
            if ($criteriaMatch.Success) {
                $criteriaText = $criteriaMatch.Groups[1].Value
                $criteriaBoxes = [regex]::Matches($criteriaText, '(?m)^- \[( |x)\]')
                if ($criteriaBoxes.Count -gt 0) {
                    $openCriteria = [regex]::Matches($criteriaText, '(?m)^- \[ \]')
                    if ($openCriteria.Count -eq 0) {
                        $errors.Add("$($section.Id) has all acceptance criteria checked but state is ""$state"". Use ""ready for acceptance"" until the user accepts it.")
                    }
                }
            }
        }
    }
}

if ($errors.Count -gt 0) {
    foreach ($errorItem in $errors) {
        Write-Error $errorItem
    }

    exit 1
}

Write-Host "TASKS.md verification passed: $($sections.Count) active tasks checked."
