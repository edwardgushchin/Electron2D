# WebAssembly browser export

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0164`.
Обновлено: 2026-06-23.

## Назначение

Electron2D должен иметь отдельный web export target, который собирает игру в WebAssembly и запускает её в браузере без переписывания игрового кода. Этот target дополняет native desktop/mobile export и не заменяет Windows, Linux, macOS, Android или iOS.

WebAssembly browser export в этой спецификации означает статический browser package: набор файлов, который можно открыть через локальный или внешний статический HTTP server. Публикация на внешний hosting, CDN, store или облачный сервис не входит в `0.1.0 Preview`.

## Export target

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

## Toolchain

Минимальный toolchain:

- .NET SDK;
- .NET WebAssembly build tools, проверяемые как отдельный факт окружения;
- runtime identifier `browser-wasm`;
- локальный static HTTP server для smoke run;
- browser automation harness или документированная ручная browser smoke-команда.

Validator не должен запускать build, browser, deploy или remote hosting. Он получает обнаруженные факты окружения и возвращает deterministic diagnostics.

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

## Package layout

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

## Runtime policies

Browser runtime должен явно учитывать ограничения браузера:

- rendering readiness подтверждается после создания browser canvas/root element и первого готового кадра;
- input принимает pointer/mouse/touch/keyboard events и преобразует их в существующие Electron2D input events без browser-specific public handles;
- audio starts locked: первый звук может быть разрешён только после пользовательского жеста, а smoke фиксирует состояние `userGestureRequired`;
- save data работает через browser sandbox storage, а если persistent storage недоступен, runtime должен вернуть diagnostic и использовать memory-only fallback;
- resource loading идёт из статического package layout; network access за пределы package по умолчанию запрещён;
- browser lifecycle отражает visibility/focus changes и page unload как pause/resume/shutdown signals там, где это возможно;
- dynamic loading of user code at runtime is not allowed.

## Build and run workflow

`e2d export plan-web` должен формировать build workflow без выполнения внешних процессов:

```text
dotnet publish <project.csproj> --configuration <Debug|Release> --runtime browser-wasm --self-contained true --output <outputDirectory>/wwwroot/_framework
```

`e2d export build-web` должен создать host page, loader, webmanifest и перенести project/runtime resources в `wwwroot`. По умолчанию команда пытается выполнить `dotnet publish` перед записью package files. Для deterministic checks разрешён явный режим `--skip-publish true`: он не объявляет внешний publish успешным, но создаёт проверяемый static package layout, который нужен тестам planner/layout/diagnostics.

`e2d export run-web` должен проверить созданный `wwwroot`, сформировать launch URL и сохранить structured smoke artifact. Минимальный smoke artifact проверяет package contract локально: наличие host page, loader, manifest, project settings, main scene, canvas/readiness marker, input event handlers, audio policy и save-data policy. Полноценная browser automation может использовать тот же loader API `window.Electron2DWebRuntimeSmoke.run()`.

Browser run workflow:

1. serve `wwwroot` через локальный static HTTP server;
2. открыть `index.html` в browser automation harness или вручную по launch URL;
3. дождаться readiness signal от loader;
4. проверить startup, main scene load, rendering readiness, input event path, audio policy state, resource loading и save-data policy;
5. сохранить structured smoke artifact с URL, browser name/version, pass/fail, diagnostics и проверенными steps.

Если WebAssembly toolchain отсутствует, команда publish должна fail closed с diagnostics и не объявлять внешний publish успешным. Если browser automation отсутствует, `run-web` обязан как минимум сохранить локальный smoke artifact по package contract и launch instructions; автоматизированный browser launch остаётся расширением поверх этого artifact.

## Критерии приёмки

- Export preset model round-trip поддерживает `WebAssemblyBrowser`.
- Toolchain validator fail closed, если WebAssembly build tools отсутствуют.
- Web planner создаёт deterministic package layout, publish arguments, browser policies и smoke criteria.
- Web planner fail closed для неправильного target, runtime identifier, deployment mode, signing и отсутствующих project settings.
- Web package builder создаёт `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, named project settings file `<ProjectName>.e2d`, main scene и `assets/**`, но не копирует `.electron2d/tasks/**`. Legacy `project.e2d.json` копируется только для старых проектов, где это фактическое имя project settings file.
- CLI `export build-web --skip-publish true` создаёт проверяемый static package layout без workspace job, а обычный `export build-web` fail closed, если WebAssembly build tools не соответствуют текущему SDK.
- CLI `export run-web` создаёт structured smoke artifact с criteria для startup, scene load, rendering readiness, input event path, audio policy state, resource loading и save-data policy.
- Implementation documentation описывает фактический target, layout, diagnostics, limitations и команды проверки.
- Focused export tests, source license/header checks и documentation verifiers проходят.

## Фактическое состояние, ограничения и проверки

Текущая реализация добавляет внутренний WebAssembly browser export planner, package builder, локальный smoke runner и CLI-команды `e2d export plan-web`, `e2d export build-web`, `e2d export run-web`. Planner строит проверяемый план, package builder создаёт static `wwwroot`, а smoke runner сохраняет structured artifact по browser package contract. Remote hosting, CDN, store publication и PWA/service-worker caching не выполняются.

## Target

- Export target: `WebAssemblyBrowser`.
- Runtime identifier: `browser-wasm`.
- Package shape: static browser package under `wwwroot`.
- Verification status: planner, package layout, CLI JSON contract and smoke artifact are covered by integration tests.

## Host requirements

WebAssembly browser export planning and deterministic package checks can run on any desktop host with .NET SDK `10.0.x`. Actual `dotnet publish` requires .NET WebAssembly build tools that match the active SDK. For example, an SDK `10.0.x` host is not considered publish-ready only because `wasm-tools-net9` is installed. Browser play requires serving `wwwroot` through a static HTTP server and opening `index.html` in a browser.

## SDK and toolchain

`plan-web` returns this publish command:

```text
dotnet publish <project.csproj> --configuration <Debug|Release> --runtime browser-wasm --self-contained true --output <outputDirectory>/wwwroot/_framework
```

The toolchain validator has a separate `WebAssemblyBuildToolsAvailable` flag. When a `WebAssemblyBrowser` preset is validated without matching WebAssembly build tools, validation fails closed with `E2D-EXPORT-WEB-0001`. `build-web` uses that check before running the external publish step unless `--skip-publish true` is passed for deterministic package-layout verification.

## Signing and credentials

WebAssembly browser export does not use signing credentials. A web preset with `signing.required: true` fails closed in the planner with `E2D-EXPORT-WEB-0005`. Repository files must still avoid passwords, tokens, private keys, certificates and copied secret payloads.

## Package layout

`Electron2DWebAssemblyExportPlanner.CreatePlan(...)` returns this deterministic layout, and `Electron2DWebAssemblyPackageBuilder.Build(...)` writes the static files:

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

The package builder writes `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, copies the actual project settings file such as `ReferencePlatformer.e2d`, copies the main scene path, and copies `assets/**`. The manifest stores that project file path in `projectFile`, and the loader uses it instead of assuming `project.e2d.json`. Legacy projects that still use `project.e2d.json` keep that filename. The package intentionally does not include `.electron2d/tasks/**`, local workflow files or signing secrets.

## Browser runtime policy

The plan records these browser-specific policies:

- `staticHosting` - package can be served by a static HTTP server;
- `browserSandboxStorage` - save data must respect browser storage limits;
- `audioRequiresUserGesture` - audio starts locked until a user gesture;
- `packageLocalResourcesOnly` - resources are loaded from package contents;
- `noRuntimeUserCodeLoading` - user code is not dynamically loaded at runtime.

Smoke criteria are:

- `startup`;
- `sceneLoad`;
- `renderingReadiness`;
- `inputEventPath`;
- `audioPolicyState`;
- `resourceLoading`;
- `saveDataPolicy`.

## CLI plan

From a project root that contains one `.e2d` project settings file and one `.csproj`:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-web --project <project-root> --format json
```

The JSON envelope uses:

- `command`: `export plan-web`;
- `route`: `none`;
- `data.mode`: `export.web.plan`;
- `data.target`: `WebAssemblyBrowser`;
- `data.runtimeIdentifier`: `browser-wasm`;
- `data.plan`: deterministic paths, publish arguments, browser policies and smoke criteria.

This command does not queue a workspace job and does not launch external build or browser processes.

## CLI build

Create the static package layout and run publish when the matching WebAssembly toolchain is available:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-web --project <project-root> --output exports/web --format json
```

For deterministic CI checks that must not invoke external publish, pass:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-web --project <project-root> --output exports/web --skip-publish true --format json
```

The JSON envelope uses `command = "export build-web"`, `route = "none"`, `data.mode = "export.web.build"` and `data.package.files`. It does not queue a workspace job. When publish is not skipped and the WebAssembly toolchain does not match the active SDK, the command fails closed before writing a misleading publish success.

## CLI run

After `build-web` created `wwwroot`, create a local smoke artifact and launch instructions:

```powershell
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-web --project <project-root> --output exports/web --url http://127.0.0.1:8080/index.html --smoke-output .electron2d/export-smoke/web-smoke.json --format json
```

The artifact format is `Electron2D.WebAssemblySmokeArtifact`. It records launch URL, web root, runtime policies, diagnostics and criteria results for `startup`, `sceneLoad`, `renderingReadiness`, `inputEventPath`, `audioPolicyState`, `resourceLoading` and `saveDataPolicy`. To play manually in a browser, serve the generated folder, for example:

```powershell
python -m http.server 8080 --directory <project-root>\exports\web\wwwroot
```

Then open `http://127.0.0.1:8080/index.html`.

## Validation

Stable diagnostic codes for the web planner:

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
- `E2D-EXPORT-WEB-0013` - smoke criterion failed.

## Known limitations

- The current repository creates a static package and local smoke artifact; it does not deploy that package to remote hosting.
- Automated browser launch can use `window.Electron2DWebRuntimeSmoke.run()`, but the built-in CLI smoke is a local package-contract check and launch-instruction artifact.
- Browser debugging, remote hosting deploy, CDN upload, PWA installation and service worker caching are outside the current implementation.
- WebAssembly browser support does not replace native desktop/mobile targets.

## Local verification

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~WebAssemblyExportTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~Electron2DCliWorkflowTests.ExportPlanWebReturnsWebAssemblyBrowserPlanWithoutQueueingJob|FullyQualifiedName~Electron2DCliWorkflowTests.ExportBuildWebCreatesBrowserPackageWithoutQueueingJob|FullyQualifiedName~Electron2DCliWorkflowTests.ExportRunWebWritesBrowserSmokeArtifactWithoutQueueingJob"
```
