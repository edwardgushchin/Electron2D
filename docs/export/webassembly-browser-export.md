# WebAssembly browser export

Обновлено: 2026-06-24.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0164`.
Обновлено: 2026-06-24.

## Назначение

Electron2D должен иметь отдельный web export target, который собирает игру в WebAssembly и запускает её в браузере без переписывания игрового кода. Этот target дополняет нативный экспорт для настольных и мобильных платформ и не заменяет Windows, Linux, macOS, Android или iOS.

WebAssembly browser входит в `runtimeTargets` и в `releaseVerificationTargets` текущего preview-релиза, но остаётся отдельным release verification tier от самой runtime matrix. Локальный package-contract smoke artifact полезен для диагностики; финальный release gate требует browser launch evidence, runtime probes или отдельного изменения `docs/releases/0.1.0-preview.md`.

WebAssembly browser export в этой спецификации означает статический browser package: набор файлов, который можно открыть через локальный или внешний статический HTTP server. Публикация на внешний hosting, CDN, store или облачный сервис не входит в `0.1.0 Preview`.

## Целевой export target

Preset для web export использует:

- `target`: `WebAssemblyBrowser`;
- `runtimeIdentifier`: `browser-wasm`;
- `configuration`: `Debug` или `Release`;
- `selfContained`: `true`;
- `rendererProfile`: `Automatic`, `Compatibility` или `Standard`;
- `outputDirectory`: корневая папка web export artifacts;
- `includeDebugSymbols`: разрешён для debug workflow;
- `signing.required`: `false`.

`WebAssemblyBrowser` не требует signing credentials. Если preset требует signing, planner должен fail closed: web package не подписывается Android/iOS/macOS credentials.

## Инструменты сборки

Минимальный toolchain:

- .NET SDK;
- .NET WebAssembly build tools, проверяемые как отдельный факт окружения;
- runtime identifier `browser-wasm`;
- локальный static HTTP server для smoke run;
- browser automation harness или документированная ручная browser smoke-команда.

Проверка окружения не должна запускать сборку, браузер, deploy или внешний хостинг. Она получает обнаруженные факты окружения и возвращает deterministic diagnostics.

Stable diagnostics:

- `E2D-EXPORT-WEB-0001` - WebAssembly build tools недоступны;
- `E2D-EXPORT-WEB-0002` - web preset использует target, отличный от `WebAssemblyBrowser`;
- `E2D-EXPORT-WEB-0003` - web preset использует runtime identifier, отличный от `browser-wasm`;
- `E2D-EXPORT-WEB-0004` - web preset не self-contained;
- `E2D-EXPORT-WEB-0005` - web preset требует signing;
- `E2D-EXPORT-WEB-0006` - project file path отсутствует;
- `E2D-EXPORT-WEB-0007` - project settings отсутствуют;
- `E2D-EXPORT-WEB-0008` - project settings некорректны;
- `E2D-EXPORT-WEB-0009` - внешний `dotnet publish` не запустился или завершился с ошибкой;
- `E2D-EXPORT-WEB-0010` - browser package не удалось записать;
- `E2D-EXPORT-WEB-0011` - обязательный файл browser package отсутствует или не читается;
- `E2D-EXPORT-WEB-0012` - путь ресурса выходит за пределы project root;
- `E2D-EXPORT-WEB-0013` - критерий browser smoke не прошёл.

Сообщения diagnostics должны объяснять, какой preset заблокирован и почему. Они не должны раскрывать credential references или локальные секреты.

## Структура browser package

Planner должен возвращать deterministic browser package layout:

```text
<outputDirectory>/
  wwwroot/
    index.html
    electron2d.loader.js
    electron2d.webmanifest.json
    _framework/
    assets/
    <ProjectName>.e2d
    <main scene path>
```

`index.html` - browser host page, в которую package builder вставляет canvas/root element и loader script.

`electron2d.loader.js` - тонкий loader, который отвечает за запуск browser runtime, передачу main scene, readiness state, input event path и browser storage/audio policy state в smoke harness.

`electron2d.webmanifest.json` - non-secret package metadata: project name, engine version, project settings file path, main scene, renderer profile, package format version и browser policies. Для legacy projects допустим fallback `project.e2d.json`, если исходный проект ещё использует старое имя файла.

`_framework/` - publish output runtime/dependency directory для WebAssembly artifacts.

`assets/` - статические игровые ресурсы после import/export processing. Служебные файлы редактора, включая `.electron2d/tasks/**`, local-only task tracker, дневник и completed archives, не входят в browser package.

## Правила browser runtime

Browser runtime должен явно учитывать ограничения браузера:

- rendering readiness подтверждается после создания browser canvas/root element и первого готового кадра;
- input принимает pointer/mouse/touch/keyboard events и преобразует их в существующие Electron2D input events без browser-specific public handles;
- audio starts locked: первый звук может быть разрешён только после пользовательского жеста, а smoke фиксирует состояние `userGestureRequired`;
- save data работает через browser sandbox storage, а если persistent storage недоступен, runtime должен вернуть diagnostic и использовать memory-only fallback;
- resource loading идёт из статического package layout; network access за пределы package по умолчанию запрещён;
- browser lifecycle отражает visibility/focus changes и page unload как pause/resume/shutdown signals там, где это возможно;
- dynamic loading of user code at runtime is not allowed.

## Сборка и запуск

`e2d export plan-web` должен формировать build workflow без выполнения внешних процессов:

```text
dotnet publish <project.csproj> --configuration <Debug|Release> --runtime browser-wasm --self-contained true --output <outputDirectory>/wwwroot/_framework
```

`e2d export build-web` должен создать host page, loader, webmanifest и перенести project/runtime resources в `wwwroot`. По умолчанию команда пытается выполнить `dotnet publish` перед записью package files. Для deterministic checks разрешён явный режим `--skip-publish true`: он не объявляет внешний publish успешным, но создаёт проверяемую статическую структуру пакета, которая нужна тестам planner/layout/diagnostics.

`e2d export run-web` должен проверить созданный `wwwroot`, сформировать launch URL и сохранить structured smoke artifact. Минимальный smoke artifact локально проверяет контракт пакета: наличие host page, loader, manifest, project settings, main scene, canvas/readiness marker, input event handlers, audio policy и save-data policy. Полноценная автоматизация браузера может использовать тот же loader API `window.Electron2DWebRuntimeSmoke.run()`.

Browser run workflow:

1. serve `wwwroot` через локальный static HTTP server;
2. открыть `index.html` в browser automation harness или вручную по launch URL;
3. дождаться readiness signal от loader;
4. проверить startup, main scene load, rendering readiness, input event path, audio policy state, resource loading и save-data policy;
5. сохранить structured smoke artifact с URL, browser name/version, pass/fail, diagnostics и проверенными steps.

Если WebAssembly toolchain отсутствует, команда publish должна завершиться закрытой ошибкой (`fail closed`) с diagnostics и не объявлять внешний publish успешным. Если автоматизация браузера отсутствует, `run-web` обязан как минимум сохранить локальный smoke artifact по package contract и instructions для запуска; автоматизированный browser launch остаётся расширением поверх этого artifact.

## Критерии приёмки

- Export preset model round-trip поддерживает `WebAssemblyBrowser`.
- Toolchain validator fail closed, если WebAssembly build tools отсутствуют.
- Web planner создаёт deterministic package layout, publish arguments, browser policies и smoke criteria.
- Web planner fail closed для неправильного target, runtime identifier, deployment mode, signing и отсутствующих project settings.
- Web package builder создаёт `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, named project settings file `<ProjectName>.e2d`, main scene и `assets/**`, но не копирует `.electron2d/tasks/**`. Legacy `project.e2d.json` копируется только для старых проектов, где это фактическое имя project settings file.
- CLI `export build-web --skip-publish true` создаёт проверяемую статическую структуру пакета без workspace job, а обычный `export build-web` завершается закрытой ошибкой (`fail closed`), если WebAssembly build tools не соответствуют текущему SDK.
- CLI `export run-web` создаёт structured smoke artifact с criteria для startup, scene load, rendering readiness, input event path, audio policy state, resource loading и save-data policy.
- Implementation documentation описывает фактический target, layout, diagnostics, limitations и команды проверки.
- Focused export tests, source license/header checks и documentation verifiers проходят.

## Фактическое состояние, ограничения и проверки

Текущая реализация добавляет внутренний планировщик экспорта для WebAssembly browser, сборщик браузерного пакета, локальный запуск smoke-проверки и CLI-команды `e2d export plan-web`, `e2d export build-web`, `e2d export run-web`. Планировщик строит проверяемый план, сборщик пакета создаёт статический каталог `wwwroot`, а smoke-runner сохраняет structured artifact по контракту браузерного пакета. Внешний хостинг, CDN, публикация в store и PWA/service-worker caching не выполняются.

## Целевой export target

- Export target: `WebAssemblyBrowser`.
- Runtime identifier: `browser-wasm`.
- Форма пакета: статический браузерный пакет в `wwwroot`.
- Статус проверки: планировщик, структура пакета, CLI JSON contract и smoke artifact покрыты интеграционными тестами.

## Требования к окружению

Планирование экспорта для WebAssembly browser и детерминированные проверки пакета могут выполняться на любом настольном окружении с .NET SDK `10.0.x`. Реальный `dotnet publish` требует .NET WebAssembly build tools, которые соответствуют активному SDK. Например, окружение с SDK `10.0.x` не считается готовым к публикации только потому, что установлен `wasm-tools-net9`. Для ручного запуска в браузере нужно раздать `wwwroot` через локальный статический HTTP-сервер и открыть `index.html` в браузере.

## SDK и инструменты сборки

`plan-web` возвращает такую команду публикации:

```text
dotnet publish <project.csproj> --configuration <Debug|Release> --runtime browser-wasm --self-contained true --output <outputDirectory>/wwwroot/_framework
```

Проверка инструментов сборки имеет отдельный флаг `WebAssemblyBuildToolsAvailable`. Когда `WebAssemblyBrowser` preset проверяется без подходящих WebAssembly build tools, validation завершается закрытой ошибкой (`fail closed`) с `E2D-EXPORT-WEB-0001`. `build-web` использует эту проверку перед внешним шагом публикации, если не передан `--skip-publish true` для детерминированной проверки структуры пакета.

## Подписание и учётные ссылки

Экспорт для WebAssembly browser не использует данные для подписания. Web preset с `signing.required: true` завершается закрытой ошибкой (`fail closed`) в планировщике с `E2D-EXPORT-WEB-0005`. Файлы репозитория всё равно не должны содержать passwords, tokens, private keys, certificates и скопированные secret payloads.

## Структура браузерного пакета

`Electron2DWebAssemblyExportPlanner.CreatePlan(...)` возвращает эту детерминированную структуру, а `Electron2DWebAssemblyPackageBuilder.Build(...)` записывает статические файлы:

```text
<outputDirectory>/
  wwwroot/
    index.html
    electron2d.loader.js
    electron2d.webmanifest.json
    _framework/
    assets/
    <ProjectName>.e2d
    <main scene path>
```

Сборщик пакета записывает `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, копирует фактический файл настроек проекта, например `ReferencePlatformer.e2d`, копирует путь к главной сцене и `assets/**`. Manifest хранит путь к project file в `projectFile`, а loader использует его вместо предположения о `project.e2d.json`. Старые проекты, которые всё ещё используют `project.e2d.json`, сохраняют это имя файла. Пакет намеренно не включает `.electron2d/tasks/**`, локальные workflow-файлы или signing secrets.

## Правила браузерной среды выполнения

План фиксирует такие browser-specific policies:

- `staticHosting` - пакет можно раздавать через статический HTTP-сервер;
- `browserSandboxStorage` - сохранение данных должно учитывать ограничения browser storage;
- `audioRequiresUserGesture` - звук остаётся заблокированным до пользовательского жеста;
- `packageLocalResourcesOnly` - ресурсы загружаются из содержимого пакета;
- `noRuntimeUserCodeLoading` - пользовательский код не загружается динамически во время выполнения.

Критерии smoke-проверки:

- `startup`;
- `sceneLoad`;
- `renderingReadiness`;
- `inputEventPath`;
- `audioPolicyState`;
- `resourceLoading`;
- `saveDataPolicy`.

## CLI route `plan-web`

Из корня проекта, который содержит один `.e2d` файл настроек проекта и один `.csproj`:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-web --project <project-root> --format json
```

JSON-оболочка использует:

- `command`: `export plan-web`;
- `route`: `none`;
- `data.mode`: `export.web.plan`;
- `data.target`: `WebAssemblyBrowser`;
- `data.runtimeIdentifier`: `browser-wasm`;
- `data.plan`: deterministic paths, publish arguments, browser policies and smoke criteria.

Эта команда не ставит workspace job в очередь и не запускает внешнюю сборку или процессы браузера.

## CLI route `build-web`

Создать статическую структуру пакета и запустить публикацию, когда доступны подходящие WebAssembly build tools:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-web --project <project-root> --output exports/web --format json
```

Для детерминированных CI-проверок, которые не должны вызывать внешний publish, передайте:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-web --project <project-root> --output exports/web --skip-publish true --format json
```

JSON-оболочка использует `command = "export build-web"`, `route = "none"`, `data.mode = "export.web.build"` и `data.package.files`. Команда не ставит workspace job в очередь. Если publish не пропущен и WebAssembly build tools не соответствуют активному SDK, команда завершается закрытой ошибкой (`fail closed`) до записи ложной успешной публикации.

## CLI route `run-web`

После того как `build-web` создал `wwwroot`, создайте локальный smoke artifact и инструкции запуска:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-web --project <project-root> --output exports/web --url http://127.0.0.1:8080/index.html --smoke-output .electron2d/export-smoke/web-smoke.json --format json
```

Формат артефакта: `Electron2D.WebAssemblySmokeArtifact`. Артефакт записывает launch URL, web root, runtime policies, diagnostics и criteria results для `startup`, `sceneLoad`, `renderingReadiness`, `inputEventPath`, `audioPolicyState`, `resourceLoading` и `saveDataPolicy`. Для ручного запуска в браузере раздайте сгенерированную папку, например:

```powershell
python -m http.server 8080 --directory <project-root>\exports\web\wwwroot
```

Затем откройте `http://127.0.0.1:8080/index.html`.

## Диагностика

Стабильные diagnostic codes для web planner:

- `E2D-EXPORT-WEB-0001` - WebAssembly build tools недоступны;
- `E2D-EXPORT-WEB-0002` - target не `WebAssemblyBrowser`;
- `E2D-EXPORT-WEB-0003` - runtime identifier не `browser-wasm`;
- `E2D-EXPORT-WEB-0004` - export не self-contained;
- `E2D-EXPORT-WEB-0005` - web preset требует signing;
- `E2D-EXPORT-WEB-0006` - пустой project file path;
- `E2D-EXPORT-WEB-0007` - отсутствуют project settings;
- `E2D-EXPORT-WEB-0008` - project settings невалидны;
- `E2D-EXPORT-WEB-0009` - внешний `dotnet publish` не запустился или завершился с ошибкой;
- `E2D-EXPORT-WEB-0010` - browser package не удалось записать;
- `E2D-EXPORT-WEB-0011` - обязательный файл package отсутствует или не читается;
- `E2D-EXPORT-WEB-0012` - путь ресурса выходит за пределы project root;
- `E2D-EXPORT-WEB-0013` - smoke criterion не прошёл.

## Известные ограничения

- Текущий репозиторий создаёт статический пакет и локальный smoke artifact; он не разворачивает этот пакет на внешний хостинг.
- Автоматический запуск браузера может использовать `window.Electron2DWebRuntimeSmoke.run()`, но встроенный CLI smoke является локальной проверкой package contract и артефактом с launch instructions.
- Отладка в браузере, публикация на внешний хостинг, CDN upload, PWA installation и service worker caching находятся вне текущей реализации.
- Поддержка WebAssembly browser не заменяет нативные настольные и мобильные target-платформы.

## Локальная проверка

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~WebAssemblyExportTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~Electron2DCliWorkflowTests.ExportPlanWebReturnsWebAssemblyBrowserPlanWithoutQueueingJob|FullyQualifiedName~Electron2DCliWorkflowTests.ExportBuildWebCreatesBrowserPackageWithoutQueueingJob|FullyQualifiedName~Electron2DCliWorkflowTests.ExportRunWebWritesBrowserSmokeArtifactWithoutQueueingJob"
```
