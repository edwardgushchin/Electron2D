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
- `RepositoryType`: `git`

## Релизные документы

- `CHANGELOG.md` описывает текущее состояние preview baseline.
- `RELEASE-NOTES.md` фиксирует ограничения: старый runtime удалён, новый Godot-like runtime ещё строится задачами из `TASKS.md`.
- Breaking changes policy для `0.x` описывает, что публичный API может меняться между preview-сборками без compatibility layer, но каждое breaking change должно быть явно записано в changelog и release notes.

## Проверка

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ReleaseMetadata.ps1
```

Команда проверяет согласованность package metadata, changelog, release notes и README.
