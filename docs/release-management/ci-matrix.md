# CI-матрица `0.1-preview`

Обновлено: 2026-07-01.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0003`, дополнения `T-0207`, `T-0209`, `T-0210`, `T-0215`, `T-0231`.
Обновлено: 2026-07-01.

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
- запускать `dotnet run --project eng/Electron2D.Build -- verify ci-matrix`;
- запускать `dotnet run --project eng/Electron2D.Build -- verify no-powershell-workflows`;
- запускать `dotnet run --project eng/Electron2D.Build -- verify licenses`;
- запускать `dotnet run --project eng/Electron2D.Build -- verify source-domain-layout`;
- запускать `dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600` без `--include-baseline`;
- запускать `dotnet run --project eng/Electron2D.Build -- verify box2d-physics-candidate --native-aot`;
- запускать `dotnet run --project eng/Electron2D.Build -- verify project-template`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify user-documentation`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify docs`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify canonical-goal-alignment`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify export-documentation`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify reference-game-assets`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify public-api-xml-docs --fail-on-issues`.
- клонировать `Electron2D.wiki.git` в `.github/wiki`.
- запускать `dotnet run --project eng/Electron2D.Build -- update api-manifest --check`.
- запускать `dotnet run --project eng/Electron2D.Build -- update wiki --output .github/wiki --check`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify ui-public-api-gate --wiki-path .github/wiki`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify public-api-documentation --wiki-path .github/wiki`.
- запускать `dotnet run --project eng/Electron2D.Build -- package --rid <rid>` для платформенного `rid` текущего runner-а: `win-x64`, `linux-x64` или `osx-arm64`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify performance-budgets`.
- запускать `dotnet run --project eng/Electron2D.Build -- verify performance`.
- не содержать `shell: pwsh`, `.ps1` или команду PowerShell как активный рабочий путь.

## Baseline и gaps

CI не запускает `Category=Baseline` по умолчанию, потому что этот красный тест фиксировал отсутствие `Electron2D.Node` до задач объектной модели. Отдельная задача GitHub Actions должна явно сообщать, что короткие проверки Android, iOS и экспорта ещё не являются активным контролем в текущем baseline.

## Верификация

Репозиторий должен содержать локальную проверку:

```bash
dotnet run --project eng/Electron2D.Build -- verify ci-matrix
```

Локальная проверка читает workflow-файл, матрицу настольных платформ, .NET SDK `10.0.x`, запуск `dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600`, C#-проверку Box2D.NET candidate, контроль API manifest, C#-проверку локальной документации, проверку согласования текущей цели, документационные проверки, C#-проверки бюджетов производительности и эталонных метрик производительности (`reference performance metrics`), а также явное упоминание разрыва готовности мобильного экспорта. Отдельная команда `verify no-powershell-workflows` fail-closed проверяет, что отслеживаемые рабочие пути не содержат активный PowerShell workflow.

## Внутренний инструмент репозитория

`eng/Electron2D.Build` является внутренней C#-точкой запуска для проверок, генерации, подготовки пакетов и подготовки релиза. Это слой репозитория и сборки, а не публичный `e2d` CLI и не часть пользовательского пакета среды выполнения, то есть пакета, который получает пользователь движка.

Минимальный набор команд:

```bash
dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600
dotnet run --project eng/Electron2D.Build -- verify
dotnet run --project eng/Electron2D.Build -- verify readme
dotnet run --project eng/Electron2D.Build -- verify ci-matrix
dotnet run --project eng/Electron2D.Build -- verify docs
dotnet run --project eng/Electron2D.Build -- verify licenses
dotnet run --project eng/Electron2D.Build -- verify no-powershell-workflows
dotnet run --project eng/Electron2D.Build -- verify source-domain-layout
dotnet run --project eng/Electron2D.Build -- verify box2d-physics-candidate --native-aot
dotnet run --project eng/Electron2D.Build -- verify user-documentation
dotnet run --project eng/Electron2D.Build -- verify canonical-goal-alignment
dotnet run --project eng/Electron2D.Build -- verify export-documentation
dotnet run --project eng/Electron2D.Build -- verify reference-game-assets
dotnet run --project eng/Electron2D.Build -- verify public-api-xml-docs --fail-on-issues
dotnet run --project eng/Electron2D.Build -- verify ui-public-api-gate --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- verify public-api-documentation --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- verify manifests
dotnet run --project eng/Electron2D.Build -- verify performance-budgets
dotnet run --project eng/Electron2D.Build -- verify performance
dotnet run --project eng/Electron2D.Build -- verify performance run --scenario <id> [--out <path>] [--timeout-seconds <n>] -- <fileName> [args...]
dotnet run --project eng/Electron2D.Build -- verify release-metadata
dotnet run --project eng/Electron2D.Build -- verify project-template
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- update wiki --check
dotnet run --project eng/Electron2D.Build -- update wiki --output .github/wiki --check
dotnet run --project eng/Electron2D.Build -- update api-manifest --check
dotnet run --project eng/Electron2D.Build -- update docs --check
dotnet run --project eng/Electron2D.Build -- update docs
dotnet run --project eng/Electron2D.Build -- package --rid win-x64
dotnet run --project eng/Electron2D.Build -- release verify
```

Границы инструмента:

- `test` запускает четыре тестовых проекта репозитория через переносимый C#-запускатель: `Electron2D.Tests.Unit`, `Electron2D.Tests.Integration`, `Electron2D.Tests.RuntimeSmoke` и `Electron2D.Tests.GoldenData`. По умолчанию команда добавляет фильтр `Category!=Baseline`; `--include-baseline` отключает этот фильтр, а `--timeout-seconds <n>` задаёт ограничение времени на каждый дочерний `dotnet test`.
- Базовый `verify` возвращает структурированное сообщение о выбранном шаге и не запускает полный релизный прогон.
- `verify readme` выполняет C#-проверку публичного `README.md` и возвращает структурированные диагностики.
- `verify ci-matrix` проверяет `.github/workflows/ci.yml`: настольную матрицу, .NET SDK, все обязательные C#-маршруты, платформенные package-шаги и явный статус разрыва мобильного экспорта.
- `verify docs` строит ожидаемый manifest локальной документации и NDJSON-shard-файлы C#-логикой, сверяет их с отслеживаемыми файлами, валидирует схему, источники, команды, аудитории, ссылки на API manifest, метаданные генератора Wiki и временный SQLite-кэш поиска.
- `verify licenses` проверяет текст `LICENSE` и MIT-заголовки отслеживаемых C# файлов через C#-код.
- `verify no-powershell-workflows` проверяет отслеживаемые Git пути, относящиеся к действующим рабочим правилам репозитория: файлы GitHub Actions, `AGENTS.md`, локальные навыки, активные доменные документы, `TASKS.md`, пересоздаваемые метаданные документации, `data/quality` и бывший корневой путь `tools/`, если такой путь снова появится в Git. Команда отказывает при сомнении: она отклоняет отслеживаемые `tools/*.ps1`, `pwsh`, `.ps1` и команды PowerShell как активный рабочий путь. Неотслеживаемые черновики не входят в этот контракт, а исторические, миграционные и отказные упоминания допускаются только через точные записи C#-разрешающего списка.
- `verify no-powershell-workflows allowed-mentions` является отдельным диагностическим вызовом. Он выводит разрешённые исторические, миграционные и отказные упоминания с полями `path`, `lineNumber` и `message`; если в той же области есть активные команды PowerShell, вызов выводит ошибки и возвращает ненулевой код завершения. Этот вызов не заменяет строгую проверку `verify no-powershell-workflows`.
- `verify source-domain-layout` проверяет крупные каталоги `src/Electron2D`, разрешённые вложенные домены и публичные namespace-правила.
- `verify box2d-physics-candidate --native-aot` запускает Release smoke-проект Box2D.NET, проверяет обязательные фрагменты документа `docs/physics/box2d-net-validation.md`, а с `--native-aot` публикует и запускает NativeAOT smoke для текущего runtime identifier.
- `verify user-documentation`, `verify canonical-goal-alignment`, `verify export-documentation` и `verify reference-game-assets` являются C#-переносом соответствующих доменных проверок пользовательской документации, канонической цели, export-документации и набора ассетов reference games.
- `verify public-api-xml-docs --fail-on-issues`, `verify ui-public-api-gate --wiki-path <path>` и `verify public-api-documentation --wiki-path <path>` проверяют полноту XML-комментариев публичного API, готовность UI/Text строк compatibility table и объединённый контур публичной API-документации без PowerShell-посредника.
- `verify manifests` объединяет лёгкие C#-проверки release metadata и проектного шаблона, где manifest означает JSON-описания проекта, шаблона, стартовой доски задач и релизных метаданных.
- `verify release-metadata` проверяет package metadata, README, брендовые файлы и отсутствие отслеживаемых локальных релизных черновиков `CHANGELOG*` и `RELEASE-NOTES*` через C#-код; tracked `TASKS.md` остаётся рабочим журналом задач репозитория.
- `verify project-template` проверяет состав шаблона `data/templates/electron2d-empty`, JSON-манифесты шаблона и стартовых задач, `.gitignore` и отсутствие репозиторных workflow-файлов в пользовательском шаблоне. Это проверка формы и состава; она не заменяет полную проверку упаковки, восстановления, сборки и запуска.
- `verify api-compatibility --wiki-path .github/wiki` сверяет manual API profile, API manifest и GitHub Wiki `API-Compatibility.md`, а также запрещает legacy/component public types.
- `verify performance-budgets` проверяет доменный документ `docs/release-management/performance-budgets.md` и возвращает структурированную диагностику.
- `verify performance` читает `data/quality/performance-reference-metrics.json`, проверяет схему, список устройств, сценарии, бюджеты p95/p99, отсутствие постоянных управляемых выделений памяти .NET на кадр, локальные пути `evidence` и доказательства batching, а затем пишет машиночитаемый план проверки в `.temp/reference-performance/verification-plan.json` или путь из `--out`. Команда является универсальной проверкой метрик: свежий длительный запуск Platformer, правила прогрева, пороги полного измерения и доказательства будущего релизного контроля остаются в `T-0221`.
- `verify performance run --scenario <id> ... -- <fileName> [args...]` запускает дочернюю команду сценария через переносимый C#-механизм, применяет `--timeout-seconds`, пишет JSON-артефакт `Electron2D.PerformanceScenarioRun` и возвращает структурированные диагностики успеха, ошибки или истечения времени. Этот запускатель является общей основой для будущего `T-0221`.
- `update docs --check` проверяет синхронизацию `data/documentation/electron2d-local-docs-index.json`, четырёх файлов `data/documentation/local-docs-index/*.ndjson` и временного SQLite-кэша. `update docs` пересоздаёт manifest, shard-файлы и локальный SQLite-кэш через текущий генератор.
- `update wiki --check` генерирует ожидаемые Wiki pages в C# и проверяет API manifest; с `--output .github/wiki` дополнительно сверяет рабочий Wiki clone.
- `update api-manifest --check` пересоздаёт ожидаемый API manifest из compiled public surface, XML documentation и `data/api/electron2d-public-api-profile.json`, затем сравнивает его с `data/api/electron2d-api-manifest.json`.
- `package --rid <rid>` обязан принимать только точную форму из трёх аргументов: команда `package`, флаг `--rid` и непустой `runtime identifier`, то есть идентификатор целевой платформы .NET. Лишние, переставленные, повторные или пустые аргументы должны завершаться ненулевым кодом и диагностикой `E2D-BUILD-CLI-INVALID-ARGUMENTS`. Поддерживаемые значения: `win-x64`, `linux-x64`, `osx-arm64`; другие значения возвращают `E2D-BUILD-PACKAGE-RID-UNSUPPORTED`.
- `package --rid <rid>` создаёт локальный релизный набор в `artifacts/release/0.1-preview/<rid>/`: пакет библиотеки среды выполнения `Electron2D`, выходные файлы `dotnet publish` для `Electron2D.Editor`, выходные файлы `dotnet publish` для `e2d`, `README.md`, `LICENSE`, `release-manifest.json`, основной архив и файл SHA-256. Команда не копирует скрипты PowerShell, рабочий журнал задач, дневники, архивы завершённых задач, доказательства внешнего аудита и внутренний `eng/Electron2D.Build` в пользовательский релизный архив.
- `release verify` проверяет локальный набор черновых релизных файлов для `win-x64`, `linux-x64` и `osx-arm64`: имена архивов, SHA-256, форму манифеста, обязательные каталоги `library/`, `editor/`, `tools/e2d/` и политику запрещённых файлов. Команда не создаёт тег, GitHub Release, черновик релиза или публикацию.

Механизм запуска дочерних процессов внутри инструмента должен быть переносимым между Windows, Linux и macOS: использовать `ProcessStartInfo.ArgumentList`, явно задавать рабочий каталог, захватывать стандартный вывод и поток ошибок, поддерживать отмену и ограничение времени (`timeout`), возвращать код завершения дочернего процесса и сообщать об истечении времени отдельной диагностикой. При внешней отмене от вызывающего кода механизм должен завершить дерево дочернего процесса, дочитать стандартный вывод и поток ошибок, не считать это истечением времени и вернуть диагностический код `E2D-BUILD-PROCESS-CANCELED`. Инструмент не должен строить команды через строки, зависящие от конкретной командной оболочки (`shell`).

Диагностики инструмента должны выводиться в структурированном виде, пригодном для автоматических тестов. Каждый элемент обязан содержать имя команды, имя шага и поля `severity`, `code`, `message`; для дочерних процессов дополнительно фиксируются код завершения и признак истечения времени, когда они применимы.

## Фактическое состояние, ограничения и проверки

Статус: реализованная CI-конфигурация и рабочий C#-слой репозитория для `T-0207` с переносом README/docs/API/manifest-проверок в `T-0213`/`T-0214`, release packaging в `T-0209`, удалением активного PowerShell пути в `T-0210`, test/performance-команд в `T-0215` и shard-контрактом локальной документации `T-0231`.
Задача: `T-0003`, дополнения `T-0207`, `T-0209`, `T-0210`, `T-0213`, `T-0214`, `T-0215` и `T-0231`.
Обновлено: 2026-07-01.

## Workflow

CI описан в workflow-файле `.github/workflows/ci.yml`.

Основной job `tests` запускается на:

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

На каждой платформе workflow-файл устанавливает .NET SDK `10.0.x`, восстанавливает `src/Electron2D.sln` и запускает:

```bash
dotnet run --project eng/Electron2D.Build -- verify ci-matrix
dotnet run --project eng/Electron2D.Build -- verify no-powershell-workflows
dotnet run --project eng/Electron2D.Build -- verify licenses
dotnet run --project eng/Electron2D.Build -- verify source-domain-layout
dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600
dotnet run --project eng/Electron2D.Build -- verify box2d-physics-candidate --native-aot
dotnet run --project eng/Electron2D.Build -- verify project-template
dotnet run --project eng/Electron2D.Build -- verify user-documentation
dotnet run --project eng/Electron2D.Build -- verify docs
dotnet run --project eng/Electron2D.Build -- verify canonical-goal-alignment
dotnet run --project eng/Electron2D.Build -- verify export-documentation
dotnet run --project eng/Electron2D.Build -- verify reference-game-assets
dotnet run --project eng/Electron2D.Build -- verify public-api-xml-docs --fail-on-issues
dotnet run --project eng/Electron2D.Build -- update api-manifest --check
dotnet run --project eng/Electron2D.Build -- update wiki --output .github/wiki --check
dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- verify ui-public-api-gate --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- verify public-api-documentation --wiki-path .github/wiki
dotnet run --project eng/Electron2D.Build -- verify performance-budgets
dotnet run --project eng/Electron2D.Build -- verify performance
```

`verify ci-matrix` является структурной проверкой `.github/workflows/ci.yml`: она фиксирует обязательные C#-маршруты, матрицу платформ, package-шаги и явный статус мобильного разрыва.
`verify docs` является C#-проверкой локальной документации: пересоздаваемый manifest, NDJSON-shard-файлы, временный SQLite-кэш поиска, команды `e2d docs search/type/member/example`, источник примеров и документационный контур должны оставаться синхронизированными.
`verify no-powershell-workflows` является финальным контролем `T-0210`: workflow, инструкции и активные документы не должны возвращать PowerShell как рабочий путь. Для разбора разрешённых исторических строк используется отдельный вызов `verify no-powershell-workflows allowed-mentions`; он печатает местоположение и объяснение разрешения, но не участвует в CI как пропускной контроль.
`verify canonical-goal-alignment` является контролем для исторических материалов о целях и архитектуре: старое component-first или four-platform позиционирование не должно возвращаться как актуальный контракт.
`update api-manifest --check` является контролем машиночитаемого API manifest: отслеживаемый Git JSON должен совпадать со скомпилированным public API, XML documentation и manual API profile.
`verify api-compatibility --wiki-path .github/wiki` является C#-проверкой соответствия manual API profile, API manifest и generated compatibility page.
`verify public-api-documentation --wiki-path .github/wiki` сверяет XML documentation, GitHub Wiki API reference и публичную документацию без отдельного скриптового слоя.
Проверки в `eng/Electron2D.Build` возвращают структурированные JSON-диагностики.

Платформенная упаковка запускается только на соответствующих GitHub Actions runners:

- `dotnet run --project eng/Electron2D.Build -- package --rid win-x64` - только `windows-latest`;
- `dotnet run --project eng/Electron2D.Build -- package --rid linux-x64` - только `ubuntu-latest`;
- `dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64` - только `macos-latest`.

## Mobile/export gap

Задача `mobile-export-status` явно фиксирует, что короткие проверки Android, iOS и мобильного экспорта ещё не входят в активный контроль. Это не статус готовности мобильного экспорта к релизу, а прозрачная отметка текущего разрыва до будущих задач.

Box2D.NET physics candidate проверяется только на desktop matrix через `dotnet run --project eng/Electron2D.Build -- verify box2d-physics-candidate --native-aot`. Android arm64 Release/AOT и iOS arm64 Release/AOT для physics backend остаются gap до задач mobile export/toolchain.

## Локальная проверка CI

```bash
dotnet run --project eng/Electron2D.Build -- verify ci-matrix
```

Эта команда проверяет структуру workflow-файла без обращения к GitHub Actions.

Внутренняя точка запуска для проверок:

```bash
dotnet run --project eng/Electron2D.Build -- verify
```

На этапе `T-0207` этот инструмент предоставил рабочий C#-каркас команд и переносимый механизм запуска дочерних процессов. В `T-0213` команды `verify readme`, `verify docs`, `update docs --check` и `update docs` начали выполнять реальные проверки или генерацию. В `T-0214` локальная документация, API manifest, вывод GitHub Wiki, политика лицензий и проверки JSON-манифестов имеют C#-команды `eng/Electron2D.Build` как единственную целевую поверхность. В `T-0210` активные CI, инструкции и проверяемые документы переключены на C#-маршруты, tracked `tools/*.ps1` удаляются из рабочего пути, корневой каталог `tools/` отсутствует, а оставшийся API manifest generator перенесён в `eng\Electron2D.ApiManifestGenerator`. В `T-0215` общий запускатель тестов и проверка метрик производительности (`performance metrics`) также перешли на C#-команды.

Фактическое поведение текущего C#-инструмента:

- `test` последовательно запускает четыре тестовых проекта через `dotnet test`, по умолчанию добавляет `--filter Category!=Baseline`, поддерживает `--include-baseline` и `--timeout-seconds <n>`, возвращает код завершения упавшего дочернего процесса и пишет структурированные диагностики `E2D-BUILD-TEST-*`.
- базовый `verify` возвращает код `0` и диагностический код `E2D-BUILD-ROUTED`; он только подтверждает маршрут команды и не запускает полный набор проверок.
- `verify readme` проверяет публичный `README.md` по C#-контракту и при успехе возвращает диагностический код `E2D-BUILD-README-VERIFY-PASSED`.
- `verify ci-matrix` возвращает `E2D-BUILD-CI-MATRIX-PASSED`, если workflow содержит обязательную матрицу, C#-проверки, package-шаги для `win-x64`, `linux-x64`, `osx-arm64` и явный `mobile-export-status`.
- `verify no-powershell-workflows` возвращает `E2D-BUILD-NO-POWERSHELL-WORKFLOWS-PASSED`, если отслеживаемые Git пути из рабочей области проверки не содержат активного рабочего процесса PowerShell.
- `verify no-powershell-workflows allowed-mentions` возвращает `E2D-BUILD-NO-POWERSHELL-ALLOWED-MENTION` для каждой разрешённой исторической, миграционной или отказной строки и завершает вывод сводкой `E2D-BUILD-NO-POWERSHELL-ALLOWED-MENTIONS-REPORTED`; при активной команде PowerShell он возвращает `E2D-BUILD-NO-POWERSHELL-ACTIVE-WORKFLOW` и ненулевой код.
- `verify docs` строит ожидаемый manifest локальной документации и NDJSON-shard-файлы C#-логикой `eng/Electron2D.Build`, сверяет их с `data/documentation/electron2d-local-docs-index.json` и `data/documentation/local-docs-index/*.ndjson`, затем валидирует JSON-схему, обязательные метаданные, ссылки на API manifest, хеши документов, `sources.wiki.generator = "eng/Electron2D.Build update wiki"` и временный SQLite-кэш.
- `update docs --check` проверяет синхронизацию пересоздаваемого manifest, shard-файлов и временного SQLite-кэша, а `update docs` пересоздаёт `data/documentation/electron2d-local-docs-index.json`, `data/documentation/local-docs-index/*.ndjson` и локальный SQLite-кэш.
- `update wiki --check` и `update wiki --output .github/wiki --check` выполняют C#-проверку сгенерированных Wiki pages и API manifest.
- `update api-manifest --check` выполняет C#-проверку JSON manifest.
- `verify api-compatibility --wiki-path .github/wiki` выполняет C#-проверку manual API profile, API manifest и generated `API-Compatibility.md`, а также запрещённого legacy/component public surface.
- `verify licenses`, `verify release-metadata`, `verify project-template` и `verify manifests` выполняют C#-проверки политики лицензий, релизных метаданных и проектного шаблона.
- `verify performance-budgets` проверяет `docs/release-management/performance-budgets.md` и возвращает `E2D-BUILD-PERFORMANCE-BUDGETS-PASSED` при совпадении обязательных ориентиров.
- `verify performance` проверяет `data/quality/performance-reference-metrics.json`, бюджеты, пути из поля `evidence`, batching и пишет `.temp/reference-performance/verification-plan.json` с форматом `Electron2D.ReferencePerformanceVerificationPlan`.
- `verify performance run --scenario <id> ... -- <fileName> [args...]` запускает дочернюю команду сценария, пишет JSON-артефакт `Electron2D.PerformanceScenarioRun` и возвращает `E2D-BUILD-PERFORMANCE-RUN-PASSED`, `E2D-BUILD-PERFORMANCE-RUN-FAILED` или `E2D-BUILD-PERFORMANCE-RUN-TIMEOUT`.
- неизвестная команда возвращает ненулевой код и диагностический код `E2D-BUILD-CLI-UNKNOWN-COMMAND`.
- `package` без точной формы `package --rid <rid>` возвращает ненулевой код и диагностический код `E2D-BUILD-CLI-INVALID-ARGUMENTS`.
- `package --rid <rid>` для `win-x64`, `linux-x64` и `osx-arm64` создаёт локальный релизный набор, пишет `E2D-BUILD-PACKAGE-CREATED` и поле `runtimeIdentifier`; неподдерживаемый `rid` возвращает `E2D-BUILD-PACKAGE-RID-UNSUPPORTED` без архивов.
- `release verify` проверяет уже созданные локальные релизные файлы и возвращает `E2D-BUILD-RELEASE-VERIFY-PASSED`; теги, архивы вне `artifacts/release/0.1-preview/` и GitHub Release не создаются.
- `ProcessRunner` возвращает структурированные диагностические коды `E2D-BUILD-PROCESS-EXITED`, `E2D-BUILD-PROCESS-TIMEOUT`, `E2D-BUILD-PROCESS-CANCELED` и `E2D-BUILD-PROCESS-START-FAILED` для завершения дочернего процесса, истечения времени, внешней отмены и ошибки запуска.

Проверка, выполненная для этого поведения:

```bash
dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter FullyQualifiedName~RepositoryBuildToolTests
```
