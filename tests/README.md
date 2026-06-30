# Тесты Electron2D

В этом каталоге будут размещаться автоматические проверки движка:

- unit-тесты для чистой доменной логики;
- integration-тесты подсистем runtime;
- smoke-тесты запуска и жизненного цикла;
- golden-data и golden-render проверки, когда появится стабильная тестовая инфраструктура.

С `T-0002` каталог содержит реальные тестовые проекты:

- `Electron2D.Tests.Unit`
- `Electron2D.Tests.Integration`
- `Electron2D.Tests.RuntimeSmoke`
- `Electron2D.Tests.GoldenData`
- `Electron2D.Tests.AotSmoke` - консольный smoke-проект для trimmed/NativeAOT publish через отдельный verifier.

Обычная проверка:

```bash
dotnet run --project eng/Electron2D.Build -- test
```

Baseline-проверка с намеренно падающим тестом:

```bash
dotnet run --project eng/Electron2D.Build -- test --include-baseline
```

Проверка эталонных метрик производительности и переносимый запуск одного сценария:

```bash
dotnet run --project eng/Electron2D.Build -- verify performance
dotnet run --project eng/Electron2D.Build -- verify performance run --scenario <id> [--out <path>] [--timeout-seconds <n>] -- <fileName> [args...]
```

AOT metadata smoke:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-AotMetadataSafety.ps1 -NativeAot
```
