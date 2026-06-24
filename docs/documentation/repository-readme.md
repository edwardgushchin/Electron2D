# Публичный README репозитория

Обновлено: 2026-06-24.

Этот файл является доменным документом для корневого `README.md`: здесь фиксируются назначение README, структура публичной страницы, ограничения и будущая проверка.

## Ведение документа

- Перед изменением README обновите ожидаемую структуру и ограничения в этом файле.
- README должен описывать продукт для внешнего читателя, а не внутренний процесс разработки, локальный task tracker, release gate или CI-команды.
- Автоматическая проверка README реализуется в `T-0213` поверх C# repository tool из `T-0207`; PowerShell-проверки для README больше не являются целевым контрактом.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для повторной реализации `T-0110`.
Обновлено: 2026-06-24.

## Цель

Корневой `README.md` должен выглядеть как публичная страница продукта в стиле сильных reference README: центрированный hero, аккуратные badges, верхняя навигация, лёгкие emoji-акценты и понятные разделы для человека, который впервые открыл репозиторий.

Главная формулировка продукта:

> Electron2D is an agent-native, cross-platform 2D game engine for .NET.

Второе предложение About:

> Developers and local coding agents work on the same scenes, scripts, resources, diagnostics and undo history through the editor.

Tagline `Agent-native cross-platform 2D game engine` уже встроен в брендовый SVG. README не должен повторять его отдельным `<h3>` или текстовым блоком: rendered preview показывает tagline ровно один раз.

## Обязательная структура

README должен быть полностью на английском и содержать:

- центрированный логотип;
- центрированные badges в двух коротких рядах:
  - contributors, last commit, MIT license;
  - `.NET 10`, `C# 14`, `0.1.0-preview`;
- верхнюю навигацию: About, Features, Platforms, Installation, Quick Start, Documentation, Examples, Feedback, License;
- отдельную центрированную просьбу поставить звезду: `⭐ Star us on GitHub - it motivates us a lot!`;
- emoji-prefixed разделы `About`, `Features`, `Platforms`, `Installation`, `Quick Start`, `Documentation`, `Examples`, `Feedback and Contributing`, `Contributors`, `License`.

Навигация должна оставаться стабильной: если заголовки получают emoji, README использует явные HTML anchors `about`, `features`, `platforms`, `installation`, `quick-start`, `documentation`, `examples`, `feedback-and-contributing`, `contributors` и `license`.

## Features

Порядок Features:

1. **Agent-native workflow**;
2. **Trello-style task board**;
3. **Built-in editor**;
4. **C# scripting**;
5. **Node-based scenes**;
6. **2D rendering**;
7. **2D physics**;
8. **Asset workflow**;
9. **Cross-platform runtime**.

Features должны описывать пользовательские возможности, а не перечислять public types, тестовые harnesses, verifier scripts, release artifacts или внутренние подсистемы.

`Trello-style task board` описывает пользовательскую доску задач для совместной работы: shared columns, task cards, assignees, labels, review states и editor-visible project context. Не описывать её через `ProjectTaskManager`, snapshot model или visual harness.

Запрещены размытые формулировки:

- `small and medium 2D games`;
- `project-local metadata`;
- `primary runtime targets for the preview line`.

Cross-platform runtime должен формулироваться прямо: `Build and run games on Windows, Linux, macOS and Android. iOS and Web are planned as future runtime targets.`

## Platforms

Platforms оформляется как таблица с колонками `Platform`, `Editor` и `Runtime`.

Состояния указываются прямо в ячейках emoji-маркерами:

- `✅ Done`;
- `🕓 Planned`;
- `❌ Not planned`.

Таблица перечисляет:

| Platform | Editor | Runtime |
| --- | --- | --- |
| Windows | `✅ Done` | `✅ Done` |
| Linux | `✅ Done` | `✅ Done` |
| macOS | `✅ Done` | `✅ Done` |
| Android | `❌ Not planned` | `✅ Done` |
| iOS | `❌ Not planned` | `🕓 Planned` |
| Web | `❌ Not planned` | `🕓 Planned` |

iOS и Web показываются только как future runtime targets и не входят в mandatory `0.1.0 Preview` gate. README не должен превращать Web в отдельный Roadmap-раздел.

## Installation

Installation показывает обычный пользовательский путь:

```bash
git clone https://github.com/edwardgushchin/Electron2D.git
cd Electron2D
dotnet build src/Electron2D.sln -c Release
```

## Quick Start

Quick Start запускает Editor:

```bash
dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -c Release
```

README не должен показывать PowerShell, `.ps1`, repository verifier commands, packaging rehearsal или CI-команды.

## Documentation

Documentation содержит только одну пользовательскую ссылку:

```text
https://github.com/edwardgushchin/Electron2D/wiki
```

README не должен ссылаться на локальные `docs/README.md`, `.github/wiki/*.md`, release contract, task tracker или сам этот доменный документ.

## Examples

Examples содержит только `Platformer`.

Шаблон `data/templates/electron2d-empty` является внутренней основой создания проектов и не показывается в публичном README как пример.

`UI-heavy reference game` больше не должен появляться в README. Полное удаление проекта, документов, тестов, ассетов и release matrix ссылок выполняется отдельной задачей `T-0211`.

Блок выглядит так:

```markdown
- **[Platformer](https://github.com/edwardgushchin/Electron2D/tree/main/examples/platformer)** - A complete 2D platformer built with Electron2D.
```

Старое имя `reference-platformer` не должно появляться ни в видимом тексте, ни в URL README. До фактического переименования каталога `T-0110` остаётся `open`.

## Feedback и Contributors

Feedback должен содержать реальные ссылки на GitHub Issues и Pull Requests:

- `https://github.com/edwardgushchin/Electron2D/issues`;
- `https://github.com/edwardgushchin/Electron2D/pulls`.

Feedback также должен ссылаться на `CONTRIBUTING.md`, чтобы внешний contributor видел правила до открытия pull request.

Contributors должен ссылаться на contributors graph.

## Запрещённые формулировки и ссылки

README не должен содержать:

- `C#-first`;
- `baseline`;
- `release gate`;
- `dry-run`;
- `T-` task IDs;
- `TASKS.md`;
- `PowerShell`;
- `.ps1`;
- `pwsh`;
- `Status` как отдельный раздел;
- `Roadmap` как отдельный раздел;
- `UI-heavy`;
- `reference-platformer`;
- `ReferencePlatformer`.

До переименования проекта `ReferencePlatformer` в `Platformer` README может описывать пример как `Platformer`, но не должен раскрывать старое внутреннее имя в видимом тексте или URL.

## Проверка

Целевая проверка после миграции repository automation:

```bash
dotnet run --project eng/Electron2D.Build -- verify readme
```

Проверка должна:

- проверять обязательную структуру README;
- проверять, что rendered tagline отображается ровно один раз;
- проверять star callout и emoji-акценты на публичных разделах;
- проверять отсутствие запрещённых формулировок в `README.md`, а не raw grep по всему репозиторию;
- проверять единственную документационную ссылку на GitHub Wiki;
- проверять отсутствие UI-heavy и старого имени Platformer в видимом тексте и URL;
- проверять отсутствие PowerShell-команд;
- визуально проверять rendered GitHub preview или локальный HTML/Markdown preview artifact.

## Фактическое состояние

Статус: контракт исправлен после rejection предыдущего README; автоматическая C#-проверка ещё не реализована.

`README.md` переписан как публичная продуктовая страница и больше не содержит внутренние task IDs, PowerShell-команды, UI-heavy или старое имя Platformer. Ссылка `Platformer` ведёт на будущий GitHub-каталог `examples/platformer`. `T-0110` остаётся `open`, потому что acceptance требует `T-0212`, C# verifier и visual preview check.
