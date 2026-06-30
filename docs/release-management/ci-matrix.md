# CI-матрица `0.1.0 Preview`

Обновлено: 2026-06-30.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0003`, дополнения `T-0207`, `T-0215`, `T-0231`.
Обновлено: 2026-06-30.

## Цель

CI должен проверять новый чистый baseline среды выполнения на desktop-платформах Tier 1 и явно показывать, что проверки мобильного экспорта пока являются разрывом готовности до соответствующих задач экспорта.

## Обязательная матрица

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

Каждая desktop-платформа должна:

- использовать `actions/checkout`;
- устанавливать .NET SDK `10.0.x`;
- восстанавливать `src/Electron2D.sln`;
- запускать `dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600` без `--include-baseline`.
- запускать `tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify project-template`.
- запускать `tools/Verify-UserDocumentation.ps1`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify docs`.
- запускать `tools/Verify-CanonicalGoalAlignment.ps1`.
- запускать `tools/Verify-ExportDocumentation.ps1`.
- фиксировать, что strict XML documentation audit ждёт отдельного C#-маршрута и не является целевой командой `T-0214`.
- клонировать `Electron2D.wiki.git` в `.github/wiki`.
- запускать `dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --check`.
- запускать `dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`.
- фиксировать, что consolidated public API documentation audit ждёт отдельного C#-маршрута и не является целевой командой `T-0214`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify performance-budgets`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify performance`.

## Baseline и gaps

CI не запускает `Category=Baseline` по умолчанию, потому что этот красный тест фиксировал отсутствие `Electron2D.Node` до задач объектной модели. Отдельная задача GitHub Actions должна явно сообщать, что короткие проверки Android, iOS и экспорта ещё не являются активным контролем в текущем baseline.

## Верификация

Репозиторий должен содержать локальную проверку:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Локальная проверка читает workflow-файл, матрицу настольных платформ, .NET SDK `10.0.x`, запуск `dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600`, запуск `tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot`, контроль API manifest, C#-проверку локальной документации, проверку согласования текущей цели, документационные проверки, C#-проверки бюджетов производительности и эталонных метрик производительности (`reference performance metrics`), а также явное упоминание разрыва готовности мобильного экспорта.

## Внутренний инструмент репозитория

`eng/Electron2D.Build` является внутренней C#-точкой запуска для проверок, генерации, подготовки пакетов и подготовки релиза. Это слой репозитория и сборки, а не публичный `e2d` CLI и не часть пользовательского пакета среды выполнения, то есть пакета, который получает пользователь движка.

Минимальный набор команд:

```bash
dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600
dotnet run --project eng/Electron2D.Build -- verify
dotnet run --project eng/Electron2D.Build -- verify readme
dotnet run --project eng/Electron2D.Build -- verify docs
dotnet run --project eng/Electron2D.Build -- verify licenses
dotnet run --project eng/Electron2D.Build -- verify manifests
dotnet run --project eng/Electron2D.Build -- verify performance-budgets
dotnet run --project eng/Electron2D.Build -- verify performance
dotnet run --project eng/Electron2D.Build -- verify performance run --scenario <id> [--out <path>] [--timeout-seconds <n>] -- <fileName> [args...]
dotnet run --project eng/Electron2D.Build -- verify release-metadata
dotnet run --project eng/Electron2D.Build -- verify project-template
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- update wiki --check
dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --check
dotnet run --project eng/Electron2D.Build -- update docs --check
dotnet run --project eng/Electron2D.Build -- update docs
dotnet run --project eng/Electron2D.Build -- package --rid win-x64
dotnet run --project eng/Electron2D.Build -- release verify
```

Границы инструмента:

- `test` запускает четыре тестовых проекта репозитория через переносимый C#-запускатель: `Electron2D.Tests.Unit`, `Electron2D.Tests.Integration`, `Electron2D.Tests.RuntimeSmoke` и `Electron2D.Tests.GoldenData`. По умолчанию команда добавляет фильтр `Category!=Baseline`; `--include-baseline` отключает этот фильтр, а `--timeout-seconds <n>` задаёт ограничение времени на каждый дочерний `dotnet test`.
- Базовый `verify` возвращает структурированное сообщение о выбранном шаге и не запускает полный релизный прогон.
- `verify readme` выполняет C#-проверку публичного `README.md` и возвращает структурированные диагностики.
- `verify docs` строит ожидаемый manifest локальной документации и NDJSON-shard-файлы C#-логикой, сверяет их с отслеживаемыми файлами, валидирует схему, источники, команды, аудитории, ссылки на API manifest, метаданные генератора Wiki и временный SQLite-кэш поиска.
- `verify licenses` проверяет текст `LICENSE` и MIT-заголовки отслеживаемых C# и PowerShell файлов через C#-код.
- `verify manifests` объединяет лёгкие C#-проверки release metadata и проектного шаблона, где manifest означает JSON-описания проекта, шаблона, стартовой доски задач и релизных метаданных.
- `verify release-metadata` проверяет package metadata, README, брендовые файлы и отсутствие отслеживаемых локальных release/task drafts через C#-код.
- `verify project-template` проверяет состав шаблона `data/templates/electron2d-empty`, JSON-манифесты шаблона и стартовых задач, `.gitignore` и отсутствие репозиторных workflow-файлов в пользовательском шаблоне. Это проверка формы и состава; она не заменяет полную проверку упаковки, восстановления, сборки и запуска.
- `verify api-compatibility --wiki-path .github/wiki` сверяет API manifest с GitHub Wiki `API-Compatibility.md` и запрещает legacy/component public types.
- `verify performance-budgets` проверяет доменный документ `docs/release-management/performance-budgets.md` и возвращает структурированную диагностику без PowerShell-скрипта.
- `verify performance` читает `data/quality/performance-reference-metrics.json`, проверяет схему, список устройств, сценарии, бюджеты p95/p99, отсутствие постоянных управляемых выделений памяти .NET на кадр, локальные пути `evidence` и доказательства batching, а затем пишет машиночитаемый план проверки в `.temp/reference-performance/verification-plan.json` или путь из `--out`. Команда является универсальной проверкой метрик: свежий длительный запуск Platformer, правила прогрева, пороги полного измерения и доказательства будущего релизного контроля остаются в `T-0221`.
- `verify performance run --scenario <id> ... -- <fileName> [args...]` запускает дочернюю команду сценария через переносимый C#-механизм, применяет `--timeout-seconds`, пишет JSON-артефакт `Electron2D.PerformanceScenarioRun` и возвращает структурированные диагностики успеха, ошибки или истечения времени. Этот запускатель является общей основой для будущего `T-0221`; он не требует отдельной проверки, привязанной только к PowerShell (`PowerShell-only gate`).
- XML documentation audit и consolidated public API documentation audit остаются миграционным долгом до отдельного C#-переноса и не объявляются целевыми командами `T-0214`.
- `update docs --check` проверяет синхронизацию `data/documentation/electron2d-local-docs-index.json`, четырёх файлов `data/documentation/local-docs-index/*.ndjson` и временного SQLite-кэша. `update docs` пересоздаёт manifest, shard-файлы и локальный SQLite-кэш через текущий генератор.
- `update wiki --check` генерирует ожидаемые Wiki pages в C# и проверяет API manifest; с `--output .github/wiki` дополнительно сверяет рабочий Wiki clone.
- `update api-manifest --wiki-path .github/wiki --check` пересоздаёт ожидаемый API manifest и сравнивает его с `data/api/electron2d-api-manifest.json`.
- `package --rid <rid>` обязан принимать только точную форму из трёх аргументов: команда `package`, флаг `--rid` и непустой `runtime identifier`, то есть идентификатор целевой платформы .NET. Лишние, переставленные, повторные или пустые аргументы должны завершаться ненулевым кодом и диагностикой `E2D-BUILD-CLI-INVALID-ARGUMENTS`. Пока сборка архивов не реализована, корректная форма команды должна завершаться закрытым отказом, то есть ненулевым кодом, явной причиной блокировки и выбранным `rid` в диагностическом сообщении.
- `release verify` обязан быть отдельным маршрутом для будущей проверки релизного кандидата (`release candidate`). Пока полный релизный сценарий не реализован, команда должна завершаться закрытым отказом и не должна создавать теги, архивы или GitHub Release.

Механизм запуска дочерних процессов внутри инструмента должен быть переносимым между Windows, Linux и macOS: использовать `ProcessStartInfo.ArgumentList`, явно задавать рабочий каталог, захватывать стандартный вывод и поток ошибок, поддерживать отмену и ограничение времени (`timeout`), возвращать код завершения дочернего процесса и сообщать об истечении времени отдельной диагностикой. При внешней отмене от вызывающего кода механизм должен завершить дерево дочернего процесса, дочитать стандартный вывод и поток ошибок, не считать это истечением времени и вернуть диагностический код `E2D-BUILD-PROCESS-CANCELED`. Инструмент не должен строить команды через строки, зависящие от конкретной командной оболочки (`shell`).

Диагностики инструмента должны выводиться в структурированном виде, пригодном для автоматических тестов. Каждый элемент обязан содержать имя команды, имя шага и поля `severity`, `code`, `message`; для дочерних процессов дополнительно фиксируются код завершения и признак истечения времени, когда они применимы.

## Фактическое состояние, ограничения и проверки

Статус: реализованная CI-конфигурация и рабочий C#-слой репозитория для `T-0207` с переносом README/docs/API/manifest-проверок в `T-0213`/`T-0214`, test/performance-команд в `T-0215` и shard-контрактом локальной документации `T-0231`.
Задача: `T-0003`, дополнения `T-0207`, `T-0213`, `T-0214`, `T-0215` и `T-0231`.
Обновлено: 2026-06-30.

## Workflow

CI описан в workflow-файле `.github/workflows/ci.yml`.

Основной job `tests` запускается на:

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

На каждой платформе workflow-файл устанавливает .NET SDK `10.0.x`, восстанавливает `src/Electron2D.sln` и запускает:

```bash
dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600
./tools/Verify-Box2DPhysicsCandidate.ps1 -NativeAot
dotnet run --project eng/Electron2D.Build -- verify project-template
./tools/Verify-UserDocumentation.ps1
dotnet run --project eng/Electron2D.Build -- verify docs
./tools/Verify-CanonicalGoalAlignment.ps1
./tools/Verify-ExportDocumentation.ps1
dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --check
dotnet run --project eng/Electron2D.Build -- update wiki --check --output .github/wiki
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- verify performance-budgets
dotnet run --project eng/Electron2D.Build -- verify performance
```

Строгая проверка публичной XML documentation должна стать C#-проверкой отдельным переносом: недокументированный или неполный public API должен ломать CI после подключения этого маршрута.
`verify docs` является C#-проверкой локальной документации: пересоздаваемый manifest, NDJSON-shard-файлы, временный SQLite-кэш поиска, команды `e2d docs search/type/member/example`, источник примеров и документационный контур должны оставаться синхронизированными.
`Verify-CanonicalGoalAlignment.ps1` является контролем для исторических материалов о целях и архитектуре: старое component-first или four-platform позиционирование не должно возвращаться как актуальный контракт.
`update api-manifest --wiki-path .github/wiki --check` является контролем машиночитаемого API manifest: отслеживаемый Git JSON должен совпадать со скомпилированным public API, XML documentation и GitHub Wiki compatibility table.
`verify api-compatibility --wiki-path .github/wiki` является C#-проверкой соответствия API manifest и compatibility table.
Объединённая проверка публичной API-документации должна стать C#-проверкой отдельным переносом и сверять XML documentation, GitHub Wiki API reference и public API documentation wording.
Проверки, уже перенесённые в `eng\Electron2D.Build`, возвращают структурированные JSON-диагностики. Проверки XML documentation и consolidated public API documentation пока остаются миграционным долгом и не объявляются текущими маршрутами.

Платформенные проверки экспорта запускаются только на соответствующих GitHub Actions runners:

- `tools/Verify-WindowsExport.ps1` - только `windows-latest`;
- `tools/Verify-LinuxExport.ps1` - только `ubuntu-latest`;
- `tools/Verify-MacOSExport.ps1` - только `macos-latest`.

## Mobile/export gap

Задача `mobile-export-status` явно фиксирует, что короткие проверки Android, iOS и мобильного экспорта ещё не входят в активный контроль. Это не статус готовности мобильного экспорта к релизу, а прозрачная отметка текущего разрыва до будущих задач.

Box2D.NET physics candidate проверяется только на desktop matrix через `Verify-Box2DPhysicsCandidate.ps1 -NativeAot`. Android arm64 Release/AOT и iOS arm64 Release/AOT для physics backend остаются gap до задач mobile export/toolchain.

## Локальная проверка CI

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-CiMatrix.ps1
```

Эта команда проверяет структуру workflow-файла без обращения к GitHub Actions.

Новая внутренняя точка запуска для последующей миграции проверок:

```bash
dotnet run --project eng/Electron2D.Build -- verify
```

На этапе `T-0207` этот инструмент предоставил рабочий C#-каркас команд и переносимый механизм запуска дочерних процессов. В `T-0213` команды `verify readme`, `verify docs`, `update docs --check` и `update docs` начали выполнять реальные проверки или генерацию. В `T-0214` локальная документация, API manifest, вывод GitHub Wiki, политика лицензий и проверки JSON-манифестов имеют C#-команды `eng\Electron2D.Build` как единственную целевую поверхность. В `T-0215` общий запускатель тестов и проверка метрик производительности (`performance metrics`) также перешли на C#-команды. Старые `tools/*.ps1` для этих участков допустимы только как историческое или нецелевое состояние до удаления и зачистки старых вызовов в отдельной `T-0210`.

Фактическое поведение текущего C#-инструмента:

- `test` последовательно запускает четыре тестовых проекта через `dotnet test`, по умолчанию добавляет `--filter Category!=Baseline`, поддерживает `--include-baseline` и `--timeout-seconds <n>`, возвращает код завершения упавшего дочернего процесса и пишет структурированные диагностики `E2D-BUILD-TEST-*`.
- базовый `verify` возвращает код `0` и диагностический код `E2D-BUILD-ROUTED`; он только подтверждает маршрут команды и не запускает полный набор проверок.
- `verify readme` проверяет публичный `README.md` по C#-контракту и при успехе возвращает диагностический код `E2D-BUILD-README-VERIFY-PASSED`.
- `verify docs` строит ожидаемый manifest локальной документации и NDJSON-shard-файлы C#-логикой `eng\Electron2D.Build`, сверяет их с `data/documentation/electron2d-local-docs-index.json` и `data/documentation/local-docs-index/*.ndjson`, затем валидирует JSON-схему, обязательные метаданные, ссылки на API manifest, хеши документов, `sources.wiki.generator = "eng/Electron2D.Build update wiki"` и временный SQLite-кэш.
- `update docs --check` проверяет синхронизацию пересоздаваемого manifest, shard-файлов и временного SQLite-кэша, а `update docs` пересоздаёт `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/*.ndjson` и локальный SQLite-кэш.
- `update wiki --check` и `update wiki --check --output .github/wiki` выполняют C#-проверку сгенерированных Wiki pages и API manifest.
- `update api-manifest --wiki-path .github/wiki --check` выполняет C#-проверку JSON manifest.
- `verify api-compatibility --wiki-path .github/wiki` выполняет C#-проверку API manifest против `API-Compatibility.md` и запрещённого legacy/component public surface.
- `verify licenses`, `verify release-metadata`, `verify project-template` и `verify manifests` выполняют C#-проверки политики лицензий, релизных метаданных и проектного шаблона.
- `verify performance-budgets` проверяет `docs/release-management/performance-budgets.md` и возвращает `E2D-BUILD-PERFORMANCE-BUDGETS-PASSED` при совпадении обязательных ориентиров.
- `verify performance` проверяет `data/quality/performance-reference-metrics.json`, бюджеты, пути из поля `evidence`, batching и пишет `.temp/reference-performance/verification-plan.json` с форматом `Electron2D.ReferencePerformanceVerificationPlan`.
- `verify performance run --scenario <id> ... -- <fileName> [args...]` запускает дочернюю команду сценария, пишет JSON-артефакт `Electron2D.PerformanceScenarioRun` и возвращает `E2D-BUILD-PERFORMANCE-RUN-PASSED`, `E2D-BUILD-PERFORMANCE-RUN-FAILED` или `E2D-BUILD-PERFORMANCE-RUN-TIMEOUT`.
- неизвестная команда возвращает ненулевой код и диагностический код `E2D-BUILD-CLI-UNKNOWN-COMMAND`.
- `package` без точной формы `package --rid <rid>` возвращает ненулевой код и диагностический код `E2D-BUILD-CLI-INVALID-ARGUMENTS`.
- `package --rid <rid>` возвращает ненулевой код, диагностический код `E2D-BUILD-PACKAGE-BLOCKED` и поле `runtimeIdentifier`; архивы, `release-manifest.json`, файлы контрольных сумм, каталог `artifacts/` и выходные каталоги релиза не создаются.
- `release verify` возвращает ненулевой код и диагностический код `E2D-BUILD-RELEASE-VERIFY-BLOCKED`; теги, архивы и GitHub Release не создаются.
- `ProcessRunner` возвращает структурированные диагностические коды `E2D-BUILD-PROCESS-EXITED`, `E2D-BUILD-PROCESS-TIMEOUT`, `E2D-BUILD-PROCESS-CANCELED` и `E2D-BUILD-PROCESS-START-FAILED` для завершения дочернего процесса, истечения времени, внешней отмены и ошибки запуска.

Проверка, выполненная для этого поведения:

```bash
dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~RepositoryBuildToolTests
```
