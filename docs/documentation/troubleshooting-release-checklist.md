# Troubleshooting guide и release checklist

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0101`.
Обновлено: 2026-06-21.

## Назначение

Пользовательская документация Electron2D `0.1.0 Preview` должна содержать отдельную страницу с практическим troubleshooting guide и release checklist. Страница нужна для разработчика, который проверяет локальное окружение, первый проект, импорт ресурсов, сборку, shader artifacts, export presets, mobile lifecycle gaps и runtime diagnostics перед тем, как считать preview-сборку готовой к ручной проверке.

Документация должна описывать только фактически проверенный baseline. Если часть release path ещё не закрыта задачами `0.1.0 Preview`, она должна быть явно обозначена как gap, а не как готовая возможность.

## Обязательные области troubleshooting

Страница должна покрывать следующие области с конкретными симптомами, проверками и безопасными действиями:

- `import` issues: source assets, import cache, sidecar settings, stable UID и reimport checks;
- `build` issues: .NET SDK, restore/build/run, template verification и compiler diagnostics;
- `shader` issues: source format, import-time diagnostics, target artifacts и limitations;
- `export` issues: desktop verifiers, runtime identifier, self-contained package, signing references без секретов;
- `mobile lifecycle` issues: touch/orientation/safe area expectations, pause/resume smoke requirements и текущие blocked mobile export gaps;
- `runtime diagnostics` issues: user-code exceptions, lifecycle callbacks, group calls, deferred calls, signals и crash-safe reporting.

## Release checklist

Страница должна включать release checklist, который отделяет:

- обязательные локальные проверки repository baseline;
- desktop export checks;
- documentation/API checks;
- проверки, которые нельзя считать выполненными до закрытия mobile/export/reference-game задач.

Checklist не должен требовать публикации GitHub Release или загрузки release artifact без явной команды пользователя.

## Проверяемость

`dotnet run --project eng/Electron2D.Build -- verify user-documentation` должен проверять:

- наличие страницы `docs/troubleshooting-release-checklist.md`;
- наличие ссылки на неё из `docs/user-guide.md`;
- наличие marker `user-doc:release-checklist` в user guide;
- наличие обязательных областей `import`, `build`, `shader`, `export`, `mobile lifecycle`, `runtime diagnostics`;
- наличие release checklist;
- наличие проверяемых команд: `dotnet run --project eng/Electron2D.Build -- verify project-template`, `dotnet run --project eng/Electron2D.Build -- test`, `dotnet run --project eng/Electron2D.Build -- verify user-documentation`, `dotnet run --project eng/Electron2D.Build -- package --rid win-x64`, `dotnet run --project eng/Electron2D.Build -- package --rid linux-x64`, `dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64`.

## Критерии приёмки

- Troubleshooting guide создан в `docs/troubleshooting-release-checklist.md`.
- User guide ссылается на troubleshooting guide и содержит release checklist section.
- Документация покрывает import, build, shader, export, mobile lifecycle and runtime diagnostics issues.
- Документация не публикует secrets, реальные credentials, private keys или signing payloads.
- `dotnet run --project eng/Electron2D.Build -- verify user-documentation` проверяет новую страницу и проходит локально.

## Фактическое состояние, ограничения и проверки

Эта страница описывает проверенный troubleshooting path для Electron2D `0.1.0 Preview`. Она не заменяет задачи export, editor и mobile smoke: если проверка ещё не реализована, она отмечена как release gap.

## Быстрая диагностика

Начинайте с проверок, которые не меняют проект:

```bash
dotnet run --project eng/Electron2D.Build -- verify project-template
dotnet run --project eng/Electron2D.Build -- verify user-documentation
dotnet run --project eng/Electron2D.Build -- test
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

```bash
dotnet --version
dotnet restore src\Electron2D.sln
dotnet run --project eng/Electron2D.Build -- verify project-template
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

```bash
dotnet run --project eng/Electron2D.Build -- package --rid win-x64
dotnet run --project eng/Electron2D.Build -- package --rid linux-x64
dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64
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

- project template check: `dotnet run --project eng/Electron2D.Build -- verify project-template`;
- полный test runner: `dotnet run --project eng/Electron2D.Build -- test`;
- user documentation check: `dotnet run --project eng/Electron2D.Build -- verify user-documentation`;
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
