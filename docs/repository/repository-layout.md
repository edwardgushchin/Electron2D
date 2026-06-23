# Раскладка репозитория и локальных рабочих материалов

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Создано: 2026-06-22T10:12:07+03:00.

## Назначение

Удалённый репозиторий Electron2D должен содержать только исходники, документацию продукта, тесты, инструменты, CI и поставляемые данные. Рабочие материалы агента и maintainer-а остаются локально в рабочей копии и не публикуются в удалённый repository history.

## Локальные рабочие материалы

Следующие пути являются local-only:

- `TASKS.md`
- `completed-tasks/`
- `dev-diary/`
- `CHANGELOG*`
- `RELEASE-NOTES*`

Они могут существовать в локальной рабочей копии, но не должны попадать в `git ls-files`, staging, commit, push или release source archive.

## Поставляемые данные

Поставляемые данные живут под `data/`:

- `data/templates/electron2d-empty/` - шаблон нового проекта.
- `data/assets/reference-games/` - curated ассеты для будущих reference games.
- `data/assets/branding/` - бренд-пак Electron2D: логотипы, знак, иконки, README-варианты и social preview.

Корневые каталоги `templates/` и `assets/` не используются.

## Автоматический контроль

Integration test должен проверять:

- local-only пути не отслеживаются Git;
- `.gitignore` содержит local-only правила;
- template verifier и export verifiers используют `data/templates/electron2d-empty`;
- reference asset verifier использует `data/assets/reference-games`;
- release metadata verifier проверяет брендовые ассеты, подключённые к README, package metadata и `Electron2D.Editor`;
- корневые каталоги `templates/` и `assets/` отсутствуют.

## Фактическое состояние, ограничения и проверки

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
