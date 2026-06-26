# Cross-platform release packaging и draft GitHub Release workflow

Обновлено: 2026-06-26.

Этот файл является доменным документом для будущего release packaging pipeline. Документ фиксирует целевой контракт после отказа от PowerShell-автоматизации; задача `T-0111` остаётся заблокированной до реализации фактической сборки архивов и проверки релизного кандидата внутри C# repository tool.

## Ведение документа

- Перед изменением release packaging обновите этот документ.
- Реализация должна идти через внутренний C# repository tool `eng/Electron2D.Build`, а не через PowerShell scripts.
- GitHub Release publication запрещена без отдельной явной команды maintainer-а и без прохождения полного release candidate gate.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0111`, зависит от `T-0209` и `T-0210`; каркас внутреннего инструмента задан задачей `T-0207`.
Обновлено: 2026-06-26.

## Цель

Repository должен уметь собирать release artifacts для desktop targets и готовить GitHub Release draft, но не публиковать release автоматически. До закрытия `T-0104` этот pipeline является подготовкой инфраструктуры, а не доказательством готовности релиза.

## Artifact matrix

Минимальный desktop artifact set:

| Artifact | Runtime identifier | Runner | Archive |
| --- | --- | --- | --- |
| Windows desktop package | `win-x64` | `windows-latest` | `.zip` |
| Linux desktop package | `linux-x64` | `ubuntu-latest` | `.tar.gz` |
| macOS Apple Silicon package | `osx-arm64` | `macos-latest` | `.tar.gz` |

Каждый artifact должен содержать:

- runtime library package `Electron2D`;
- `Electron2D.Editor`;
- developer tools, включая `e2d`;
- `README.md`, `LICENSE` и release manifest;
- checksum file с SHA-256 для archive.

## C# repository tool

Целевой инструмент:

```bash
dotnet run --project eng/Electron2D.Build -- package --rid win-x64
dotnet run --project eng/Electron2D.Build -- package --rid linux-x64
dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64
dotnet run --project eng/Electron2D.Build -- release verify
```

Инструмент должен быть внутренним build/repository tool, не частью публичного `e2d` CLI и не частью release package.

В рамках `T-0207` инструмент должен уметь принимать эти команды и возвращать структурированные диагностические сообщения. Команда `package` должна принимать только точную форму `package --rid <rid>` с непустым `rid`; лишние, переставленные, повторные или пустые аргументы должны возвращать `E2D-BUILD-CLI-INVALID-ARGUMENTS`. До появления фактической сборки архивов `package --rid <rid>` и `release verify` обязаны завершаться закрытым отказом: не создавать артефакты, не изменять GitHub Release и явно сообщать выбранный `rid` или ошибку формы аргументов.

Обязательные свойства:

- cross-platform process runner;
- timeouts и exit codes;
- structured diagnostics;
- archive creation;
- checksum generation;
- release manifest generation;
- fail-closed validation;
- tests for package naming, manifest shape, forbidden files and draft-release policy.

## Controlled GitHub Release

GitHub Actions workflow должен быть manual `workflow_dispatch`.

Обязательные inputs:

- `version`, по умолчанию `0.1.0-preview`;
- `dry_run`, по умолчанию `true`;
- `create_draft_release`, по умолчанию `false`;
- `configuration`, по умолчанию `Release`.

Publication policy:

- default run не создаёт GitHub Release;
- draft release разрешён только когда maintainer явно задаёт `dry_run=false` и `create_draft_release=true`;
- workflow обязан использовать draft/prerelease режим и не должен переводить release в публичное состояние;
- release notes должны явно говорить, что публикация требует отдельного решения после полного release candidate gate.

## Release process

Prerequisites:

- clean working tree и известный source commit SHA перед не-dry-run packaging;
- .NET SDK `10.0.x` на runner-е;
- зелёные repository gates;
- release notes draft, синхронизированный с README, Wiki API compatibility и текущими release gaps;
- явное решение maintainer-а, что workflow можно запустить с `dry_run=false`.

Tag policy:

- tag format: `v<version>`, например `v0.1.0-preview`;
- tag должен указывать на commit, прошедший `T-0104`;
- workflow может создать draft release для tag/version, но не переводит его в published state.

Rollback policy:

- ошибочный draft release удаляется вручную до публикации;
- ошибочные workflow artifacts не считаются release evidence;
- если tag указывает на неверный commit, maintainer удаляет draft release и tag до повторного запуска;
- опубликованный release нельзя заменять молча: требуется новый release note или patch version.

Manual checks:

- сверить artifact names, manifest `version`, `runtimeIdentifier`, `configuration` и checksum;
- скачать archive из workflow artifacts и проверить наличие `README.md`, `LICENSE`, `library`, `editor`, `tools/e2d` и `release-manifest.json`;
- убедиться, что `.electron2d/tasks/**`, `TASKS.md`, `dev-diary/`, `completed-tasks/`, `CHANGELOG.md` и `RELEASE-NOTES.md` не попали в release archive;
- приложить результаты к `T-0104`, если это не-dry-run release candidate run.

## Проверка

Целевая проверка после миграции:

```bash
dotnet run --project eng/Electron2D.Build -- release verify
```

Финальный критерий удаления PowerShell после `T-0210` не должен быть raw grep по всему репозиторию, потому что migration docs и rejection notes законно содержат эти слова. Проверка выполняется scoped C# verifier-ом с allowlist:

```bash
dotnet run --project eng/Electron2D.Build -- verify repository-automation
```

Проверка должна подтверждать отсутствие tracked `.ps1` scripts, `pwsh` workflow steps и PowerShell-команд в активных production paths, CI, release/package inputs и task/doc workflow references. Historical notes, migration docs и rejection notes допустимы только как явно перечисленные allowlist entries.

## Фактическое состояние

Статус: частично разблокировано каркасом C# repository tool, но фактическая подготовка пакетов и релиза остаётся заблокированной.

В репозитории всё ещё есть автоматизация на PowerShell, поэтому `T-0111` не может считаться готовой. Предыдущий пробный слой на PowerShell был отклонён и не является целевым решением. `T-0207` вводит только внутренний C#-каркас команд и переносимый запуск дочерних процессов; сборка архивов, создание файлов SHA-256, создание release manifest и draft GitHub Release остаются отдельными работами.
