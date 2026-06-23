# Troubleshooting guide и release checklist

Эта страница описывает проверенный troubleshooting path для Electron2D `0.1.0 Preview`. Она не заменяет задачи export, editor и mobile smoke: если проверка ещё не реализована, она отмечена как release gap.

## Быстрая диагностика

Начинайте с проверок, которые не меняют проект:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-UserDocumentation.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```

Если ошибка появляется только в конкретной подсистеме, переходите к соответствующему разделу ниже.

## Import issues

Симптомы:

- ресурс не появляется в import cache;
- scene/resource file ссылается на несуществующий `uid://`;
- после rename или move ресурс загружается как новый;
- sidecar settings не применяются.

Проверки:

- убедитесь, что source asset лежит внутри project root;
- проверьте, что sidecar file не содержит secrets и использует ожидаемое имя рядом с source asset;
- выполните полный тестовый набор, потому что в нём есть resource serialization, import cache и data stability checks;
- при ручной проверке удаляйте только generated import cache, а не source asset.

Безопасное действие: сначала восстановите source asset и UID-связи, затем выполните reimport. Не редактируйте cache artifact вручную как источник правды.

## Build issues

Симптомы:

- `dotnet restore` не находит SDK или package;
- template запускается локально, но CI падает;
- compiler diagnostics указывают на generated или copied files;
- `project.e2d.json` не находит main scene.

Проверки:

```powershell
dotnet --version
dotnet restore src\Electron2D.sln
powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1
```

Ожидайте .NET SDK `10.0.x`. Если template verifier падает, сначала проверьте `project.e2d.json`, `scenes/main.scene.json` и копирование content files в output.

## Shader issues

Симптомы:

- shader source не проходит import;
- diagnostics указывают на неверную строку;
- artifact отсутствует для нужной target platform;
- runtime пытается использовать shader path, который не прошёл import.

Проверки:

- используйте только текущий `Electron2D canvas shader v1` source format;
- проверьте import-time diagnostics с file, line, column, stage и target platform;
- убедитесь, что target platform перечислена в sidecar settings или default import profile;
- для iOS и других AOT-oriented targets используйте precompiled artifacts, а не runtime compilation.

Ограничения preview: visual shader editor, compute shaders, geometry shaders и runtime shader authoring не являются готовым пользовательским workflow.

## Export issues

Симптомы:

- desktop package не публикуется;
- package запускается на host OS, но не переносится на целевую систему;
- signing configuration отсутствует или раскрывает секреты;
- runtime identifier указан неверно;
- WebAssembly browser plan/package не содержит ожидаемый `wwwroot` layout;
- WebAssembly browser smoke artifact показывает failed criteria.

Проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-WindowsExport.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-LinuxExport.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-MacOSExport.ps1
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export plan-web --project <project-root> --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export build-web --project <project-root> --output exports/web --skip-publish true --format json
dotnet run --project src\Electron2D.Cli\Electron2D.Cli.csproj -- export run-web --project <project-root> --output exports/web --url http://127.0.0.1:8080/index.html --smoke-output .electron2d/export-smoke/web-smoke.json --format json
```

Запускайте platform-specific verifier на подходящей host OS. Desktop baseline ожидает runtime identifiers `win-x64`, `linux-x64` и `osx-arm64`, а package должен быть self-contained. WebAssembly browser workflow ожидает runtime identifier `browser-wasm`, static `wwwroot` package layout, loader/manifest files и smoke criteria в JSON artifact.

Не записывайте реальные signing secrets, private keys, passwords или account credentials в repository files. В документации и примерах допустимы только non-secret placeholders.

`build-web --skip-publish true` не запускает внешний publish и предназначен для deterministic package checks. Обычный `build-web` требует WebAssembly build tools, соответствующие активному SDK. `run-web` сохраняет structured smoke artifact и launch URL; remote hosting deploy не выполняется.

## Mobile lifecycle issues

Симптомы:

- touch input не совпадает с desktop mouse path;
- orientation или safe area выглядит неверно;
- pause/resume ломает scene state;
- audio или resources не восстанавливаются после lifecycle event;
- Android или iOS package нужен как release artifact.

Текущий статус: Android и iOS export остаются blocked release gaps до закрытия соответствующих export задач и real-device/simulator smoke. Документация может описывать ожидаемые проверки, но не должна считать mobile export готовым release path.

Минимальный будущий smoke должен покрыть launch, render, input, audio, resources, filesystem, pause/resume, orientation, safe area and shutdown.

## Runtime diagnostics issues

Симптомы:

- ошибка в user code скрывается без сообщения;
- lifecycle callback падает и ломает дальнейшую обработку;
- signal, deferred call или group call теряет exception context;
- stack trace не помогает понять, какой объект вызвал ошибку.

Проверки:

- воспроизведите ошибку минимальной сценой;
- проверьте lifecycle callbacks `_EnterTree`, `_Ready`, `_Process`, `_PhysicsProcess`;
- проверьте signals, deferred calls и group calls отдельно;
- убедитесь, что diagnostics содержит kind, object instance id, method name и exception type.

Runtime diagnostics в текущем baseline означает внутренний механизм движка, который собирает сведения об ошибках пользовательского кода и позволяет тестам проверить, что движок продолжает работать после безопасно обработанной ошибки.

<!-- user-doc:release-checklist-detail -->
## Release checklist

Перед тем как считать preview-кандидат готовым к ручной проверке, выполните:

- project template check: `tools\Verify-ProjectTemplate.ps1`;
- полный test runner: `tools\Run-Tests.ps1`;
- user documentation check: `tools\Verify-UserDocumentation.ps1`;
- public API XML documentation check;
- GitHub Wiki API reference check;
- API compatibility check;
- source license header check;
- source domain layout check;
- desktop export checks для Windows, Linux и macOS на подходящих host OS;
- проверку, что changelog, release notes и package metadata соответствуют версии preview.

Не считать закрытыми без отдельных задач:

- Android APK/AAB release smoke;
- iOS project/signing/simulator or device smoke;
- WebAssembly browser publish/smoke artifact;
- reference games performance metrics;
- leak verification для graphics, audio, physics и scene load/unload cycles;
- GitHub Release publication.

GitHub Release не публикуется без явной команды пользователя.
