# CI-матрица `0.1.0 Preview`

Статус: реализованная CI-конфигурация.
Задача: `T-0003`.
Обновлено: 2026-06-20.

## Workflow

CI описан в `.github/workflows/ci.yml`.

Основной job `tests` запускается на:

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

На каждой платформе workflow устанавливает .NET SDK `10.0.x`, восстанавливает `src/Electron2D.sln` и запускает:

```powershell
./tools/Run-Tests.ps1
./tools/Verify-ProjectTemplate.ps1
./tools/Verify-PerformanceBudgets.ps1
```

## Mobile/export gap

Job `mobile-export-status` явно фиксирует, что Android/iOS/export smoke checks ещё не входят в активный gate. Это не release-ready статус экспорта, а прозрачная отметка текущего gap до будущих задач.

## Локальная проверка CI

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Эта команда проверяет структуру workflow без обращения к GitHub Actions.
