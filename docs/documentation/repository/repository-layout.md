# Раскладка репозитория и локальных рабочих материалов

Статус: действует с `T-0138`.

## Local-only материалы

Эти файлы и каталоги используются агентами и maintainer-ом локально, но не отслеживаются Git:

- `TASKS.md`
- `completed-tasks/`
- `dev-diary/`
- `CHANGELOG*`
- `RELEASE-NOTES*`

Они остаются в рабочей копии после `git rm --cached`, потому что удаляется только запись из индекса Git, а не локальный файл.

## Data root

Поставляемые шаблоны и ассеты находятся в `data/`:

- `data/templates/electron2d-empty/`
- `data/assets/reference-games/`
- `data/assets/branding/`

Все verifiers, editor workflows и export checks читают template, reference assets и брендовые ассеты из этих путей. Брендовый набор используется README, NuGet metadata runtime package и executable-проектом `Electron2D.Editor`.

## Проверки

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~SolutionLayoutTests" --no-restore -m:1
powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferenceGameAssets.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-ReleaseMetadata.ps1
```
