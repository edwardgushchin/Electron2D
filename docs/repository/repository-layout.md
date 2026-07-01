# Раскладка репозитория и локальных рабочих материалов

Обновлено: 2026-07-01.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Создано: 2026-06-22T10:12:07+03:00.

## Назначение

Удалённый репозиторий Electron2D должен содержать исходники, документацию продукта, тесты, инструменты, CI, поставляемые данные и tracked рабочие записи, нужные для continuity между агентами. В корне не должно быть разрозненных рабочих каталогов; такие материалы живут под `data/`, если они должны отслеживаться Git.

## Локальные рабочие материалы

Следующие пути являются tracked рабочими материалами репозитория:

- `TASKS.md`
- `data/completed-tasks/`
- `data/dev-diary/`

Следующие пути являются local-only release drafts:

- `CHANGELOG*`
- `RELEASE-NOTES*`

Tracked рабочие материалы могут попадать в audit package и commit, когда задача требует обновить task state, архив завершённых задач или дневник. Release drafts могут существовать в локальной рабочей копии, но не должны попадать в `git ls-files`, staging, commit, push или release source archive.

## Поставляемые данные

Поставляемые данные живут под `data/`:

- `data/templates/electron2d-empty/` - шаблон нового проекта.
- `data/assets/reference-games/` - curated ассеты для будущих reference games.
- `data/assets/branding/` - бренд-пак Electron2D: логотипы, знак, иконки, README-варианты и social preview.
- `data/schemas/` - опубликованные JSON Schema для проектных, runtime, diagnostics и testing artifacts.
- `data/completed-tasks/` - tracked monthly archives завершённых задач репозитория.
- `data/dev-diary/` - tracked дневник разработки для continuity между агентами.

Корневые каталоги `templates/`, `assets/`, `tools/`, `schemas/`, `completed-tasks/` и `dev-diary/` не используются. Репозиторные C#-инструменты находятся под `eng/`; поставляемый внутри релизного архива путь `tools/e2d/` относится только к содержимому release package, а не к корню исходного репозитория.

## Автоматический контроль

Integration test должен проверять:

- tracked рабочие материалы находятся в `TASKS.md`, `data/completed-tasks/` и `data/dev-diary/`;
- `.gitignore` содержит local-only правила для release drafts;
- template verifier и export verifiers используют `data/templates/electron2d-empty`;
- reference asset verifier использует `data/assets/reference-games`;
- release metadata verifier проверяет брендовые ассеты, подключённые к README, package metadata и `Electron2D.Editor`;
- корневые каталоги `templates/`, `assets/`, `tools/`, `schemas/`, `completed-tasks/` и `dev-diary/` отсутствуют.

## Фактическое состояние, ограничения и проверки

Статус: действует с `T-0138`.

## Рабочие материалы репозитория

Эти файлы и каталоги используются агентами и maintainer-ом как tracked рабочие записи репозитория:

- `TASKS.md`
- `data/completed-tasks/`
- `data/dev-diary/`

Эти release draft-файлы остаются local-only и не отслеживаются Git:

- `CHANGELOG*`
- `RELEASE-NOTES*`

Release draft-файлы остаются в рабочей копии после `git rm --cached`, потому что удаляется только запись из индекса Git, а не локальный файл.

## Data root

Поставляемые шаблоны и ассеты находятся в `data/`:

- `data/templates/electron2d-empty/`
- `data/assets/reference-games/`
- `data/assets/branding/`
- `data/schemas/`
- `data/completed-tasks/`
- `data/dev-diary/`

Все verifiers, editor workflows и export checks читают template, reference assets и брендовые ассеты из этих путей. Брендовый набор используется README, NuGet metadata runtime package и executable-проектом `Electron2D.Editor`.

## Проверки

```bash
dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~SolutionLayoutTests" --no-restore -m:1
dotnet run --project eng/Electron2D.Build -- verify project-template
dotnet run --project eng/Electron2D.Build -- verify reference-game-assets
dotnet run --project eng/Electron2D.Build -- verify release-metadata
```
