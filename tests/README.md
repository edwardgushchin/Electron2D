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

Обычная проверка:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1
```

Baseline-проверка с намеренно падающим тестом:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Run-Tests.ps1 -IncludeBaseline
```
