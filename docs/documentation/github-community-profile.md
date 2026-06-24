# GitHub community profile репозитория

Обновлено: 2026-06-24.

Этот документ фиксирует публичное оформление GitHub repository profile Electron2D: metadata, community files, issue templates, pull request template и проверки через GitHub CLI.

## Цель

Репозиторий должен быть понятен внешнему читателю до первого clone: GitHub About заполнен, README ведёт к Wiki и feedback channels, а GitHub показывает высокий community profile health без фиктивных release artifacts.

## Публичные metadata

GitHub repository details должны содержать:

- Description: `Agent-native, cross-platform 2D game engine for .NET.`
- Website: `https://github.com/edwardgushchin/Electron2D/wiki`
- Topics: `2d-game-engine`, `agent-native`, `assets`, `cross-platform`, `csharp`, `dotnet`, `editor`, `game-development`, `game-engine`, `physics`, `rendering`, `scripting`.

Home page widgets:

- Releases можно показывать на главной странице репозитория.
- Deployments и Packages не нужно показывать, пока там нет полезного публичного содержимого.

## Community files

Публичный профиль репозитория должен иметь:

- `README.md` — публичная входная страница продукта.
- `LICENSE` — MIT license.
- `CODE_OF_CONDUCT.md` — правила поведения для публичного участия.
- `CONTRIBUTING.md` — короткий внешний guide для issues, pull requests, документации, тестов и безопасности.
- `SECURITY.md` — куда сообщать security issues без публикации секретов в issues.
- `SUPPORT.md` — где искать помощь: Wiki, Issues и Discussions.
- `.github/ISSUE_TEMPLATE/bug_report.yml` — форма воспроизводимого bug report.
- `.github/ISSUE_TEMPLATE/feature_request.yml` — форма feature request/design proposal.
- `.github/ISSUE_TEMPLATE/documentation.yml` — форма documentation issue.
- `.github/ISSUE_TEMPLATE/config.yml` — настройки issue chooser.
- `.github/PULL_REQUEST_TEMPLATE.md` — краткий PR checklist.

`CONTRIBUTING.md`, `SECURITY.md`, `SUPPORT.md`, issue forms и PR template пишутся на английском, потому что это публичная GitHub-витрина и они должны совпадать по языку с `README.md`.

## Release policy

GitHub Release, tag, release artifact и release publication не создаются только ради community profile score.

Публичный release разрешён только после отдельной явной команды maintainer-а и после release gate, описанного в `docs/release-management/release-packaging.md`. Пока release packaging не готов, отсутствие release считается честным ограничением, а не недоработкой community files.

## Ограничения

- Ссылка README на `https://github.com/edwardgushchin/Electron2D/tree/main/examples/platformer` не меняется этой задачей. Реальный каталог появляется в `T-0212`.
- Community files не должны требовать от внешнего contributor-а знания локального `TASKS.md`, ignored dev diary или внутренних acceptance notes.
- Issue/PR templates не должны просить secrets, private keys, tokens, private customer data или production credentials.
- Community files не должны публиковать release readiness claims до закрытия release gate.

## Проверка

Read-only GitHub CLI checks:

```bash
gh repo view edwardgushchin/Electron2D --json description,homepageUrl,repositoryTopics,licenseInfo,isPrivate,latestRelease
gh api repos/edwardgushchin/Electron2D/community/profile
```

Ожидаемый результат после push:

- `description` и `documentation` заполнены.
- `files.readme`, `files.license`, `files.code_of_conduct_file`, `files.contributing`, `files.issue_template` и `files.pull_request_template` присутствуют.
- `health_percentage` повышается относительно исходных `57`.
- `latestRelease` может оставаться `null`, пока release gate не закрыт.

## Фактическое состояние

Статус: community files добавлены для публичного GitHub profile без создания release.

Оставшиеся ограничения для полной витрины:

- README-ссылка на `examples/platformer` станет реальной после `T-0212`.
- Первый GitHub Release появится только после release packaging и отдельной явной команды.
