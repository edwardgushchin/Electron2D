# CI-матрица `0.1.0 Preview`

Обновлено: 2026-06-26.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0003`, дополнение `T-0207`.
Обновлено: 2026-06-26.

## Цель

CI должен проверять новый clean runtime baseline на Tier 1 desktop-платформах и явно показывать, что mobile/export проверки пока являются gap до соответствующих задач экспорта.

## Обязательная матрица

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

Каждая desktop-платформа должна:

- использовать `actions/checkout`;
- устанавливать .NET SDK `10.0.x`;
- восстанавливать `src/Electron2D.sln`;
- запускать `tools/Run-Tests.ps1` без `-IncludeBaseline`.
- запускать `tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot`.
- запускать `tools/Verify-ProjectTemplate.ps1`.
- запускать `tools/Verify-UserDocumentation.ps1`.
- запускать `tools/Verify-LocalDocumentation.ps1`.
- запускать `tools/Verify-CanonicalGoalAlignment.ps1`.
- запускать `tools/Verify-ExportDocumentation.ps1`.
- запускать `tools/Verify-PublicApiXmlDocs.ps1 -FailOnIssues`.
- клонировать `Electron2D.wiki.git` в `.github/wiki`.
- запускать `tools/Update-ApiManifest.ps1 -WikiPath .github/wiki -Check`.
- запускать `tools/Update-ApiWiki.ps1 -OutputPath .github/wiki -Check`.
- запускать `tools/Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki`.
- запускать `tools/Verify-PerformanceBudgets.ps1`.

## Baseline и gaps

CI не запускает `Category=Baseline` по умолчанию, потому что этот red-test фиксирует отсутствие `Electron2D.Node` до задач объектной модели. Отдельный job должен явно сообщать, что Android/iOS/export smoke checks ещё не являются активным gate в текущем baseline.

## Верификация

Репозиторий должен содержать локальный verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Verifier проверяет наличие workflow, desktop-матрицу, .NET SDK `10.0.x`, запуск `tools/Run-Tests.ps1`, запуск `tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot`, API manifest gate, local documentation gate, canonical goal alignment audit, документационные проверки и явное упоминание mobile/export status gap.

## Внутренний инструмент репозитория

`eng/Electron2D.Build` является внутренней C#-точкой запуска для проверок, генерации, подготовки пакетов и подготовки релиза. Это слой репозитория и сборки, а не публичный `e2d` CLI и не часть пользовательского runtime package, то есть пакета, который получает пользователь движка.

Минимальный набор команд:

```bash
dotnet run --project eng/Electron2D.Build -- test
dotnet run --project eng/Electron2D.Build -- verify
dotnet run --project eng/Electron2D.Build -- verify readme
dotnet run --project eng/Electron2D.Build -- verify docs
dotnet run --project eng/Electron2D.Build -- update wiki --check
dotnet run --project eng/Electron2D.Build -- package --rid win-x64
dotnet run --project eng/Electron2D.Build -- release verify
```

Границы инструмента:

- `test` и базовый `verify` могут выполнять только лёгкие проверки репозитория или возвращать структурированное сообщение о выбранном шаге; они не должны неявно запускать полный релизный прогон.
- `verify readme`, `verify docs` и `update wiki --check` являются отдельными маршрутами для будущей миграции существующих проверок из PowerShell в C#.
- `package --rid <rid>` обязан принимать только точную форму из трёх аргументов: команда `package`, флаг `--rid` и непустой runtime identifier, то есть идентификатор целевой платформы .NET. Лишние, переставленные, повторные или пустые аргументы должны завершаться ненулевым кодом и диагностикой `E2D-BUILD-CLI-INVALID-ARGUMENTS`. Пока сборка архивов не реализована, корректная форма команды должна завершаться закрытым отказом, то есть ненулевым кодом, явной причиной блокировки и выбранным `rid` в диагностическом сообщении.
- `release verify` обязан быть отдельным маршрутом для будущей проверки релизного кандидата (`release candidate`). Пока полный релизный сценарий не реализован, команда должна завершаться закрытым отказом и не должна создавать теги, архивы или GitHub Release.

Механизм запуска дочерних процессов внутри инструмента должен быть переносимым между Windows, Linux и macOS: использовать `ProcessStartInfo.ArgumentList`, явно задавать рабочий каталог, захватывать стандартный вывод и поток ошибок, поддерживать отмену и ограничение времени (`timeout`), возвращать код завершения дочернего процесса и сообщать об истечении времени отдельной диагностикой. При внешней отмене от вызывающего кода механизм должен завершить дерево дочернего процесса, дочитать стандартный вывод и поток ошибок, не считать это истечением времени и вернуть диагностический код `E2D-BUILD-PROCESS-CANCELED`. Инструмент не должен строить команды через строки, зависящие от конкретной командной оболочки (`shell`).

Диагностики инструмента должны выводиться в структурированном виде, пригодном для автоматических тестов. Каждый элемент обязан содержать имя команды, имя шага и поля `severity`, `code`, `message`; для дочерних процессов дополнительно фиксируются код завершения и признак истечения времени, когда они применимы.

## Фактическое состояние, ограничения и проверки

Статус: реализованная CI-конфигурация и новый рабочий C#-слой репозитория для `T-0207`.
Задача: `T-0003`, дополнение `T-0207`.
Обновлено: 2026-06-26.

## Workflow

CI описан в `.github/workflows/ci.yml`.

Основной job `tests` запускается на:

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

На каждой платформе workflow устанавливает .NET SDK `10.0.x`, восстанавливает `src/Electron2D.sln` и запускает:

```powershell
./tools/Run-Tests.ps1
./tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot
./tools/Verify-ProjectTemplate.ps1
./tools/Verify-UserDocumentation.ps1
./tools/Verify-LocalDocumentation.ps1
./tools/Verify-CanonicalGoalAlignment.ps1
./tools/Verify-ExportDocumentation.ps1
./tools/Verify-PublicApiXmlDocs.ps1 -FailOnIssues
./tools/Update-ApiManifest.ps1 -WikiPath .github/wiki -Check
./tools/Update-ApiWiki.ps1 -OutputPath .github/wiki -Check
./tools/Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki
./tools/Verify-PerformanceBudgets.ps1
```

`Verify-PublicApiXmlDocs.ps1 -FailOnIssues` является gate публичной XML documentation: недокументированный или неполный public API ломает CI.
`Verify-LocalDocumentation.ps1` является gate локальной документации: generated local-docs index, `e2d docs search/type/member/example`, examples source и documentation pipeline должны оставаться синхронизированными.
`Verify-CanonicalGoalAlignment.ps1` является gate для исторических goal/architecture материалов: старое component-first или four-platform позиционирование не должно возвращаться как актуальный контракт.
`Update-ApiManifest.ps1 -WikiPath .github/wiki -Check` является gate machine-readable API manifest: tracked JSON должен совпадать с compiled public API, XML documentation и GitHub Wiki compatibility table.
`Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki` является объединённым gate для XML documentation, GitHub Wiki API reference и public API documentation wording.
Этот audit запускает вложенные verifier-скрипты через доступный PowerShell executable, поэтому один и тот же gate работает на Windows, Linux и macOS runners.

Platform-specific export verifiers запускаются только на соответствующих runners:

- `tools/Verify-WindowsExport.ps1` - только `windows-latest`;
- `tools/Verify-LinuxExport.ps1` - только `ubuntu-latest`;
- `tools/Verify-MacOSExport.ps1` - только `macos-latest`.

## Mobile/export gap

Job `mobile-export-status` явно фиксирует, что Android/iOS/mobile export smoke checks ещё не входят в активный gate. Это не release-ready статус мобильного экспорта, а прозрачная отметка текущего gap до будущих задач.

Box2D.NET physics candidate проверяется только на desktop matrix через `Verify-Box2DPhysicsCandidate.ps1 -NativeAot`. Android arm64 Release/AOT и iOS arm64 Release/AOT для physics backend остаются gap до задач mobile export/toolchain.

## Локальная проверка CI

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Эта команда проверяет структуру workflow без обращения к GitHub Actions.

Новая внутренняя точка запуска для последующей миграции проверок:

```bash
dotnet run --project eng/Electron2D.Build -- verify
```

На этапе `T-0207` этот инструмент предоставляет рабочий C#-каркас команд и переносимый механизм запуска дочерних процессов. Полная замена существующих PowerShell-проверок остаётся отдельной работой, поэтому workflow CI пока продолжает запускать перечисленные выше `tools/*.ps1`.

Фактическое поведение `T-0207`:

- `test`, `verify`, `verify readme`, `verify docs` и `update wiki --check` возвращают код `0` и диагностический код `E2D-BUILD-ROUTED`; они только подтверждают маршрут команды и не запускают полный набор проверок.
- неизвестная команда возвращает ненулевой код и диагностический код `E2D-BUILD-CLI-UNKNOWN-COMMAND`.
- `package` без точной формы `package --rid <rid>` возвращает ненулевой код и диагностический код `E2D-BUILD-CLI-INVALID-ARGUMENTS`.
- `package --rid <rid>` возвращает ненулевой код, диагностический код `E2D-BUILD-PACKAGE-BLOCKED` и поле `runtimeIdentifier`; архивы, `release-manifest.json`, файлы контрольных сумм, каталог `artifacts/` и выходные каталоги релиза не создаются.
- `release verify` возвращает ненулевой код и диагностический код `E2D-BUILD-RELEASE-VERIFY-BLOCKED`; теги, архивы и GitHub Release не создаются.
- `ProcessRunner` возвращает структурированные диагностические коды `E2D-BUILD-PROCESS-EXITED`, `E2D-BUILD-PROCESS-TIMEOUT`, `E2D-BUILD-PROCESS-CANCELED` и `E2D-BUILD-PROCESS-START-FAILED` для завершения дочернего процесса, истечения времени, внешней отмены и ошибки запуска.

Проверка, выполненная для этого поведения:

```bash
dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~RepositoryBuildToolTests
```
