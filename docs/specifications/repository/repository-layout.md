# Раскладка репозитория и локальных рабочих материалов

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
