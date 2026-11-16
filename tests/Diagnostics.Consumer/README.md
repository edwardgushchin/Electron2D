# Diagnostics consumer

The consumer uses public Electron2D API. It links the existing managed and window diagnostics checks; backend-only native checks remain in Electron2D.Tests.

```powershell
dotnet build tests/Diagnostics.Consumer/Diagnostics.Consumer.csproj -c Release
dotnet run --project tests/Diagnostics.Consumer/Diagnostics.Consumer.csproj -c Release --no-build -- text
dotnet run --project tests/Diagnostics.Consumer/Diagnostics.Consumer.csproj -c Release --no-build -- managed
dotnet run --project tests/Diagnostics.Consumer/Diagnostics.Consumer.csproj -c Release --no-build -- output
dotnet run --project tests/Diagnostics.Consumer/Diagnostics.Consumer.csproj -c Release --no-build -- gpu
dotnet run --project tests/Diagnostics.Consumer/Diagnostics.Consumer.csproj -c Release --no-build -- compatibility
```

After building for win-x64, compare actual process stream bytes:

```powershell
python -B tests/Diagnostics.Consumer/check_wire.py dotnet tests/Diagnostics.Consumer/bin/Release/net10.0/win-x64/Diagnostics.Consumer.dll
```

Use the matching output RID on other hosts. `portable` exercises the managed-only workflow; to prove absence of native loading, build without native assets. `verbose disabled`, `verbose enabled --verbose`, `verbose enabled -v`, `verbose disabled -- --verbose` and `verbose disabled ++ --verbose` check process switches independently.
