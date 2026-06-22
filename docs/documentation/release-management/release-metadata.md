# Версионирование и release metadata `0.1.0 Preview`

Статус: реализованный metadata baseline.
Задача: `T-0005`.
Обновлено: 2026-06-20.

## Package metadata

Runtime package `src/Electron2D/Electron2D.csproj` использует:

- `Version`: `0.1.0-preview`
- `PackageVersion`: `0.1.0-preview`
- `AssemblyVersion`: `0.1.0.0`
- `FileVersion`: `0.1.0.0`
- `InformationalVersion`: `0.1.0-preview`
- `PackageId`: `Electron2D`
- `Authors`: `Electron2D Team`
- `PackageLicenseExpression`: `MIT`
- `PackageReadmeFile`: `README.md`
- `PackageIcon`: `electron2d_windows_icon_128.png`
- `RepositoryType`: `git`

Runtime package пакует `data/assets/branding/icon/electron2d_windows_icon_128.png` в корень package как `electron2d_windows_icon_128.png`. `Electron2D.Editor` использует `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon` для desktop executable.

## Релизные документы и локальные черновики

- `CHANGELOG*` и `RELEASE-NOTES*` являются локальными черновиками maintainer-а и не отслеживаются Git.
- Публичная release metadata для репозитория сейчас живёт в `README.md` и project metadata.
- Breaking changes policy для `0.x` описывает, что публичный API может меняться между preview-сборками без compatibility layer, но каждое breaking change должно быть явно записано в публичном release text или локальном release draft перед публикацией GitHub Release.

## Проверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ReleaseMetadata.ps1
```

Команда проверяет согласованность package metadata, README, брендовых файлов, иконки `Electron2D.Editor` и отсутствие tracked release draft файлов.
