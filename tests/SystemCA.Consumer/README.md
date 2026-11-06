# System CA consumer

This public-only consumer shares SystemCAChecks with the root runner. In a Windows Developer PowerShell, build the independent SDK inspector and run:

```powershell
$proof = 'C:/absolute/system-ca-receipts'
New-Item -ItemType Directory -Force -Path $proof
cl /nologo /EHsc /O2 /MT /std:c++17 /utf-8 tools/system-certificates/inspect.cpp /Fe:"$proof/inspect.exe" /Fo:"$proof/inspect.obj" crypt32.lib
dotnet run --project tests/SystemCA.Consumer/SystemCA.Consumer.csproj -c Release -- "$proof/inspect.exe" "$proof/source" example.com
```

Omit the third argument for store/PEM/resource checks without external network access. The live profile performs three native TLS controls against the named host on port 443: exported CA snapshot, default live OS trust and rejected unrelated anchor. An independent local SslStream server provides a deterministic hostname-refusal control without public-server SNI routing. Positive controls require HTTP/1.x response bytes. Generated control keys are disposed after use; Windows Schannel uses a temporary provider-key import and the encoded key buffer is cleared. No certificate is installed and no OS trust-store changes occur.

Repeat against a fresh candidate package and strict full-trim NativeAOT using the matching native bundle. Keep outputs outside the repository. The root profile uses ELECTRON2D_TEST_SYSTEM_CA=INSPECTOR_EXE, with receipt directory and optional live host as application arguments. Native producer failure controls are separate in [SystemCA.NativeChecks](../SystemCA.NativeChecks/README.md); [the component](../../docs/components/system-certificates.md) records actual semantics and qualification limits.
