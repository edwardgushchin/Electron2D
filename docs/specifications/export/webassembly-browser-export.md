# WebAssembly browser export

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
    project.e2d.json
    <main scene path>
```

`index.html` - browser host page, в которую package builder вставляет canvas/root element и loader script.

`electron2d.loader.js` - тонкий loader, который отвечает за запуск browser runtime, передачу main scene, readiness state, input event path и browser storage/audio policy state в smoke harness.

`electron2d.webmanifest.json` - non-secret package metadata: project name, engine version, main scene, renderer profile, package format version и browser policies.

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
- Web package builder создаёт `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, `project.e2d.json`, main scene и `assets/**`, но не копирует `.electron2d/tasks/**`.
- CLI `export build-web --skip-publish true` создаёт проверяемый static package layout без workspace job, а обычный `export build-web` fail closed, если WebAssembly build tools не соответствуют текущему SDK.
- CLI `export run-web` создаёт structured smoke artifact с criteria для startup, scene load, rendering readiness, input event path, audio policy state, resource loading и save-data policy.
- Implementation documentation описывает фактический target, layout, diagnostics, limitations и команды проверки.
- Focused export tests, source license/header checks и documentation verifiers проходят.
