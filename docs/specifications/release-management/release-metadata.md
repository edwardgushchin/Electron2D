# Версионирование и release metadata `0.1.0 Preview`

Статус: целевая спецификация.
Задача: `T-0005`.
Обновлено: 2026-06-20.

## Цель

Runtime package должен иметь единый preview-version baseline, а changelog и release notes должны явно отражать clean rewrite state.

## Требуемые значения

- `Version`: `0.1.0-preview`
- `PackageVersion`: `0.1.0-preview`
- `AssemblyVersion`: `0.1.0.0`
- `FileVersion`: `0.1.0.0`
- `InformationalVersion`: `0.1.0-preview`
- `PackageId`: `Electron2D`
- `Authors`: `Electron2D Team`
- `PackageLicenseExpression`: `MIT`
- `PackageReadmeFile`: `README.md`
- `RepositoryType`: `git`

## Документы релиза и локальные черновики

Удалённый репозиторий не должен отслеживать рабочие файлы:

- `CHANGELOG*`
- `RELEASE-NOTES*`

Maintainer может держать такие файлы локально как черновики будущего GitHub Release. Публичная release metadata для репозитория хранится в `README.md`, project metadata и GitHub Release, который создаётся только по явной команде maintainer-а.

## Breaking changes policy для `0.x`

Пока Electron2D находится в ветке `0.x`, публичный API может меняться между preview-сборками. Любое breaking change должно быть явно отражено в публичном release text или локальном release draft перед публикацией GitHub Release; compatibility layer ради старого API не добавляется.

## Верификация

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ReleaseMetadata.ps1
```

Verifier сверяет package metadata в `src/Electron2D/Electron2D.csproj`, отсутствие tracked release draft файлов и упоминание текущей preview-версии в README.
