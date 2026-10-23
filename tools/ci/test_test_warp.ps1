$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'install_test_warp.ps1'
$errors = $null; $tokens = $null
$null = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw ($errors | Out-String) }
$temporary = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString())
New-Item -ItemType Directory $temporary | Out-Null
$fixture = Join-Path $temporary 'fixture.zip'
$zip = [IO.Compression.ZipFile]::Open($fixture, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($folder in 'x64', 'win32', 'arm64') {
        $entry = $zip.CreateEntry("build/native/bin/$folder/d3d10warp.dll")
        $writer = [IO.StreamWriter]::new($entry.Open())
        try { $writer.Write($folder) } finally { $writer.Dispose() }
    }
}
finally { $zip.Dispose() }
function Invoke-WebRequest { param($Uri, $OutFile) Copy-Item $fixture $OutFile }
function dotnet { $global:LASTEXITCODE = $warpVerifyCode }
function Get-FileHash {
    param($Path, $Algorithm)
    if ($Algorithm -ne 'SHA256') { throw 'Unexpected digest algorithm.' }
    $hash = if ($Path.EndsWith('.nupkg')) {
        if ($warpBadPackage) { 'WRONG' } else { 'EA44B77EB30EEC14427193E20F40CDB2E4C31ED11AB39CB880538FDF4BAC2681' }
    } else {
        if ($warpBadDLL) { 'WRONG' } else { @{'x64'='E79C10550449365ADF0A9393D97A0DF69941E671AB6E952A78D92DA066517CA3';'win32'='FD659631C310DFD00F10CFA1CAFD2434BEDD4647BB0819E857959737B7D97BA3';'arm64'='43C12A63B7708D83B29A09118CA63D49F20293B4EC3ED67182209CBCF4E095F7'}[[IO.File]::ReadAllText($Path)] }
    }
    @{Hash=$hash}
}
function Get-AuthenticodeSignature {
    param($Path)
    @{Status=$warpSignatureStatus;SignerCertificate=@{Subject=$warpSigner}}
}
try {
    foreach ($case in 'x64', 'x86', 'arm64', 'bad-package', 'bad-DLL', 'verify-failure', 'invalid-signature', 'foreign-signer', 'signer-suffix', 'existing') {
        $runner = Join-Path $temporary $case; New-Item -ItemType Directory $runner | Out-Null
        $output = Join-Path $temporary ($case + '-output')
        $warpBadPackage = $case -eq 'bad-package'; $warpBadDLL = $case -eq 'bad-DLL'
        $warpVerifyCode = if ($case -eq 'verify-failure') { 7 } else { 0 }
        $warpSignatureStatus = if ($case -eq 'invalid-signature') { 'NotSigned' } else { 'Valid' }
        $warpSigner = if ($case -eq 'foreign-signer') { 'O=Foreign' } elseif ($case -eq 'signer-suffix') { 'O=Microsoft CorporationSuffix' } else { 'CN=Publisher, O=Microsoft Corporation, C=US' }
        $architecture = if ($case -in 'x64', 'x86', 'arm64') { $case } else { 'x64' }
        if ($case -eq 'existing') { [IO.File]::WriteAllText((Join-Path $runner 'd3d10warp.dll'), 'preserved') }
        $rejected = $false
        try { & $scriptPath $runner $output $architecture } catch { $rejected = $true; $reason = $_.Exception.Message }
        $success = $case -in 'x64', 'x86', 'arm64'
        if ($rejected -eq $success) { throw "Unexpected installation outcome for ${case}: $reason" }
        $receipt = Join-Path $output 'warp-source.json'
        if ((Test-Path $receipt) -ne $success) { throw "Incorrect receipt for $case." }
        if ($success -and (Get-Content $receipt -Raw | ConvertFrom-Json).architecture -ne $architecture) { throw 'Wrong receipt architecture.' }
        if ($case -eq 'existing' -and [IO.File]::ReadAllText((Join-Path $runner 'd3d10warp.dll')) -ne 'preserved') { throw 'Existing DLL was overwritten.' }
    }
    Write-Output 'Test WARP: architecture extraction, package/DLL pins, verification/signature rejection and existing-file preservation passed.'
}
finally { Remove-Item -Recurse -Force $temporary }
