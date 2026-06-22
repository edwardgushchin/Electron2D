# CI-матрица `0.1.0 Preview`

Статус: целевая спецификация.
Задача: `T-0003`.
Обновлено: 2026-06-22.

## Цель

CI должен проверять новый clean runtime baseline на Tier 1 desktop-платформах и явно показывать, что mobile/export проверки пока являются gap до соответствующих задач экспорта.

## Обязательная матрица

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

Каждая desktop-платформа должна:

- использовать `actions/checkout`;
- устанавливать .NET SDK `10.0.x`;
- восстанавливать `src/Electron2D.sln`;
- запускать `tools/Run-Tests.ps1` без `-IncludeBaseline`.
- запускать `tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot`.
- запускать `tools/Verify-ProjectTemplate.ps1`.
- запускать `tools/Verify-UserDocumentation.ps1`.
- запускать `tools/Verify-LocalDocumentation.ps1`.
- запускать `tools/Verify-CanonicalGoalAlignment.ps1`.
- запускать `tools/Verify-ExportDocumentation.ps1`.
- запускать `tools/Verify-PublicApiXmlDocs.ps1 -FailOnIssues`.
- клонировать `Electron2D.wiki.git` в `.github/wiki`.
- запускать `tools/Update-ApiManifest.ps1 -WikiPath .github/wiki -Check`.
- запускать `tools/Update-ApiWiki.ps1 -OutputPath .github/wiki -Check`.
- запускать `tools/Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki`.
- запускать `tools/Verify-PerformanceBudgets.ps1`.

## Baseline и gaps

CI не запускает `Category=Baseline` по умолчанию, потому что этот red-test фиксирует отсутствие `Electron2D.Node` до задач объектной модели. Отдельный job должен явно сообщать, что Android/iOS/export smoke checks ещё не являются активным gate в текущем baseline.

## Верификация

Репозиторий должен содержать локальный verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Verifier проверяет наличие workflow, desktop-матрицу, .NET SDK `10.0.x`, запуск `tools/Run-Tests.ps1`, запуск `tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot`, API manifest gate, local documentation gate, canonical goal alignment audit, документационные проверки и явное упоминание mobile/export status gap.
