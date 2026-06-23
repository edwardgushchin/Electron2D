# WebAssembly browser export

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
    project.e2d.json
    <main scene path>
```

The package builder writes `index.html`, `electron2d.loader.js`, `electron2d.webmanifest.json`, copies `project.e2d.json`, copies the main scene path, and copies `assets/**`. It intentionally does not include `.electron2d/tasks/**`, local workflow files or signing secrets.

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

From a project root that contains `project.e2d.json` and one `.csproj`:

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
