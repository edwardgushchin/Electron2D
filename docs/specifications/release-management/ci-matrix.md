# CI-матрица `0.1.0 Preview`

Статус: целевая спецификация.
Задача: `T-0003`.
Обновлено: 2026-06-20.

## Цель

CI должен проверять новый clean runtime baseline на Tier 1 desktop-платформах и явно показывать, что mobile/export проверки пока являются gap до соответствующих задач экспорта.

## Обязательная матрица

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

Каждая desktop-платформа должна:

- использовать `actions/checkout`;
- устанавливать .NET SDK `10.0.x`;
- восстанавливать `Electron2D.sln`;
- запускать `tools/Run-Tests.ps1` без `-IncludeBaseline`.

## Baseline и gaps

CI не запускает `Category=Baseline` по умолчанию, потому что этот red-test фиксирует отсутствие `Electron2D.Node` до задач объектной модели. Отдельный job должен явно сообщать, что Android/iOS/export smoke checks ещё не являются активным gate в текущем baseline.

## Верификация

Репозиторий должен содержать локальный verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Verifier проверяет наличие workflow, desktop-матрицу, .NET SDK `10.0.x`, запуск `tools/Run-Tests.ps1` и явное упоминание mobile/export status gap.
