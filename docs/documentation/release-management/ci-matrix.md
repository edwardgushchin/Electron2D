# CI-матрица `0.1.0 Preview`

Статус: реализованная CI-конфигурация.
Задача: `T-0003`.
Обновлено: 2026-06-22.

## Workflow

CI описан в `.github/workflows/ci.yml`.

Основной job `tests` запускается на:

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

На каждой платформе workflow устанавливает .NET SDK `10.0.x`, восстанавливает `src/Electron2D.sln` и запускает:

```powershell
./tools/Run-Tests.ps1
./tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot
./tools/Verify-ProjectTemplate.ps1
./tools/Verify-UserDocumentation.ps1
./tools/Verify-LocalDocumentation.ps1
./tools/Verify-CanonicalGoalAlignment.ps1
./tools/Verify-ExportDocumentation.ps1
./tools/Verify-PublicApiXmlDocs.ps1 -FailOnIssues
./tools/Update-ApiManifest.ps1 -WikiPath .github/wiki -Check
./tools/Update-ApiWiki.ps1 -OutputPath .github/wiki -Check
./tools/Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki
./tools/Verify-PerformanceBudgets.ps1
```

`Verify-PublicApiXmlDocs.ps1 -FailOnIssues` является gate публичной XML documentation: недокументированный или неполный public API ломает CI.
`Verify-LocalDocumentation.ps1` является gate локальной документации: generated local-docs index, `e2d docs search/type/member/example`, examples source и documentation pipeline должны оставаться синхронизированными.
`Verify-CanonicalGoalAlignment.ps1` является gate для исторических goal/architecture материалов: старое component-first или four-platform позиционирование не должно возвращаться как актуальный контракт.
`Update-ApiManifest.ps1 -WikiPath .github/wiki -Check` является gate machine-readable API manifest: tracked JSON должен совпадать с compiled public API, XML documentation и GitHub Wiki compatibility table.
`Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki` является объединённым gate для XML documentation, GitHub Wiki API reference и public API documentation wording.
Этот audit запускает вложенные verifier-скрипты через доступный PowerShell executable, поэтому один и тот же gate работает на Windows, Linux и macOS runners.

Platform-specific export verifiers запускаются только на соответствующих runners:

- `tools/Verify-WindowsExport.ps1` - только `windows-latest`;
- `tools/Verify-LinuxExport.ps1` - только `ubuntu-latest`;
- `tools/Verify-MacOSExport.ps1` - только `macos-latest`.

## Mobile/export gap

Job `mobile-export-status` явно фиксирует, что Android/iOS/mobile export smoke checks ещё не входят в активный gate. Это не release-ready статус мобильного экспорта, а прозрачная отметка текущего gap до будущих задач.

Box2D.NET physics candidate проверяется только на desktop matrix через `Verify-Box2DPhysicsCandidate.ps1 -NativeAot`. Android arm64 Release/AOT и iOS arm64 Release/AOT для physics backend остаются gap до задач mobile export/toolchain.

## Локальная проверка CI

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Эта команда проверяет структуру workflow без обращения к GitHub Actions.
