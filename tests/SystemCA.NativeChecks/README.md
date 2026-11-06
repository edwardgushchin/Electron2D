# Native certificate-source controls

This private test project compiles the exact OS.Certificates.cs production file with a permanent-owner shell. It adds no engine reference, shipped API or runtime test hook. Its isolated assembly can register a private resolver without replacing the production engine's native resolver. The checks establish native producer behavior; actual engine/package tests remain in [SystemCA.Consumer](../SystemCA.Consumer/README.md).

Build the fixture DLL with the Windows SDK, then run each mode in a fresh process:

```powershell
cl /nologo /EHsc /O2 /MT /LD /std:c++17 /utf-8 tools/system-certificates/fixture.cpp /Fe:"$proof/fixture.dll" /Fo:"$proof/fixture.obj" /link /DEF:tools/system-certificates/fixture.def
dotnet build tests/SystemCA.NativeChecks/SystemCA.NativeChecks.csproj -c Release -o "$proof/checks"
foreach ($mode in 'filter','zero-time','empty','open-error','enumerate-error','property-error','bad-time') {
    & "$proof/checks/SystemCA.NativeChecks.exe" "$proof/fixture.dll" $mode "$proof/receipts/$mode"
}
```

Use an absolute private proof directory. The native fixture verifies readonly/existing-only effective ROOT opening, then creates only a private CryptoAPI memory store from three generated DER certificates. It sets past, future and empty native disallow properties without changing any OS store. Other modes refuse open/enumeration/property reads or report an invalid metadata size. Every exported context and store must drain, including early failures. Successful modes change the source before a second query to reject stale caching. Public DER, PEM and drain receipts stay in the private proof directory. Repeat this exact-source test as NativeAOT separately from the public engine consumer. This private project's AOT target makes only the five tested CryptoAPI imports dynamic so its resolver can supply controlled inputs, preserving the SDK's remaining direct Windows imports. The actual engine consumer retains ordinary direct AOT binding and queries the real OS store.
