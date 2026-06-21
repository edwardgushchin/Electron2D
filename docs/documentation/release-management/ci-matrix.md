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
./tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot
./tools/Verify-ProjectTemplate.ps1
./tools/Verify-UserDocumentation.ps1
./tools/Verify-PublicApiXmlDocs.ps1
./tools/Verify-PerformanceBudgets.ps1
```

`Verify-PublicApiXmlDocs.ps1` пока запускается в report mode: он публикует список gaps публичной XML documentation, но не ломает CI до завершения `T-0106`.

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
