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

## Документы релиза

Репозиторий должен содержать:

- `CHANGELOG.md`
- `RELEASE-NOTES.md`

Оба документа должны описывать, что текущий `0.1.0-preview` является clean rewrite baseline после удаления legacy runtime, а не готовым игровым runtime.

## Breaking changes policy для `0.x`

Пока Electron2D находится в ветке `0.x`, публичный API может меняться между preview-сборками. Любое breaking change должно быть явно отражено в `CHANGELOG.md` и `RELEASE-NOTES.md`; compatibility layer ради старого API не добавляется.

## Верификация

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ReleaseMetadata.ps1
```

Verifier сверяет package metadata в `src/Electron2D/Electron2D.csproj`, наличие changelog/release notes и упоминание текущей preview-версии в README.
