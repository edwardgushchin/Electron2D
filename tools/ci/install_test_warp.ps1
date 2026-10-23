param([Parameter(Mandatory)][string]$Runner, [Parameter(Mandatory)][string]$Output,
      [ValidateSet('arm64', 'x64', 'x86')][string]$Architecture = 'x64')
$ErrorActionPreference = 'Stop'
$Runner = (Resolve-Path $Runner).Path
$destination = Join-Path $Runner 'd3d10warp.dll'
if (Test-Path $destination) { throw 'The test runner already contains a WARP DLL.' }
New-Item -ItemType Directory -Force $Output | Out-Null
$Output = (Resolve-Path $Output).Path
$package = Join-Path $Output 'microsoft.direct3d.warp.1.0.21.nupkg'
Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/microsoft.direct3d.warp/1.0.21/microsoft.direct3d.warp.1.0.21.nupkg' -OutFile $package
if ((Get-FileHash $package -Algorithm SHA256).Hash -ne 'EA44B77EB30EEC14427193E20F40CDB2E4C31ED11AB39CB880538FDF4BAC2681') { throw 'The pinned WARP package hash differs.' }
dotnet nuget verify $package --all
if ($LASTEXITCODE -ne 0) { throw 'WARP NuGet signature verification failed.' }
$folder = @{ x64 = 'x64'; x86 = 'win32'; arm64 = 'arm64' }[$Architecture]
$expected = @{
    x64 = 'E79C10550449365ADF0A9393D97A0DF69941E671AB6E952A78D92DA066517CA3'
    x86 = 'FD659631C310DFD00F10CFA1CAFD2434BEDD4647BB0819E857959737B7D97BA3'
    arm64 = '43C12A63B7708D83B29A09118CA63D49F20293B4EC3ED67182209CBCF4E095F7'
}[$Architecture]
$archive = [IO.Compression.ZipFile]::OpenRead($package)
try {
    $entry = $archive.GetEntry("build/native/bin/$folder/d3d10warp.dll")
    if (!$entry) { throw 'The WARP package lacks the requested architecture.' }
    [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $false)
}
finally { $archive.Dispose() }
$hash = (Get-FileHash $destination -Algorithm SHA256).Hash
if ($hash -ne $expected) { throw 'The extracted WARP DLL hash differs.' }
$signature = Get-AuthenticodeSignature $destination
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch '(^|,\s*)O=Microsoft Corporation(,|$)') { throw 'The test WARP DLL must have a valid Microsoft signature.' }
@{ version = '1.0.21'; architecture = $Architecture; path = $destination; SHA256 = $hash;
   fileVersion = (Get-Item $destination).VersionInfo.FileVersion; scope = 'Test runner only; not a runtime or distributable dependency.' } |
    ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $Output 'warp-source.json')
