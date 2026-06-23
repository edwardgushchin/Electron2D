# Пользовательская документация 0.1.0 Preview

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0097`, `T-0099` и `T-0101`.
Обновлено: 2026-06-21.

## Назначение

Пользовательская документация должна дать разработчику проверенный путь от установки до запуска первого проекта Electron2D `0.1.0 Preview`. Документация не должна обещать готовые editor workflows, mobile export или другие возможности, которые ещё не имеют проверки в текущем baseline.

## Обязательные разделы

Документация должна покрывать:

- установку и требования окружения;
- создание первого проекта;
- структуру первой сцены;
- C# scripting baseline;
- ресурсы, импорт и сериализацию;
- physics baseline;
- UI baseline;
- animation baseline;
- Input Map и ввод;
- renderer profiles, feature flags и fallback policy;
- export baseline, включая общую export guide, desktop verifier commands и mobile status gaps;
- troubleshooting;
- release checklist.

## Проверяемость

Команды документации должны ссылаться на существующие локальные verifier scripts:

- `tools\Verify-ProjectTemplate.ps1` для чистого проекта;
- `tools\Run-Tests.ps1` для полного тестового набора;
- platform-specific export verifiers для desktop export.
- `tools\Verify-ExportDocumentation.ps1` для полноты export guide, platform pages, secret policy и mobile limitations.

Документация может показывать ручные команды `dotnet restore`, `dotnet build` и `dotnet run`, но должна явно отделять release-package команды от локального repository verification path.

## Screenshots

До завершения Editor UI задач пользовательская документация не должна включать screenshots. Если screenshots появятся позже, verifier обязан проверять, что все image links указывают на существующие файлы.

## Запреты

- Не описывать непроверенное поведение как готовое.
- Не публиковать secrets, real credentials, private keys или signing payloads.
- Не использовать публичные объяснения через сторонние API-style labels, кроме README.
- Не добавлять устаревшие screenshot placeholders.

## Критерии приёмки

- User guide существует в `docs/user-guide.md`.
- Guide содержит все обязательные разделы.
- Guide ссылается на проверяемые команды, текущие verifier scripts и export guide.
- Guide не содержит запрещённых публичных формулировок.
- Screenshot links отсутствуют или указывают на существующие файлы.
- `tools\Verify-UserDocumentation.ps1` проходит локально и в CI.

## Фактическое состояние, ограничения и проверки

Эта страница описывает проверенный baseline Electron2D `0.1.0 Preview`. Она намеренно не выдаёт незавершённые editor, mobile export и production-rendering задачи за готовые возможности.

Скриншоты в этой версии документации не используются: стабильные экраны редактора появятся после закрытия Editor UI задач. Это лучше, чем держать устаревшие изображения.

<!-- user-doc:installation -->
## Установка

Для работы с текущим baseline нужен .NET SDK `10.0.x`.

Внутри репозитория быстрый способ проверить рабочее окружение:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1
```

Эта команда собирает локальный package `Electron2D`, создаёт проект из `data/templates/electron2d-empty`, выполняет restore/build/run и проверяет output первой сцены и C# script lifecycle.

После публикации package обычный проект будет подключать runtime как NuGet dependency:

```powershell
dotnet add package Electron2D --version 0.1.0-preview
```

До публикации package используйте локальные verifier scripts из репозитория.

<!-- user-doc:first-project -->
## Первый проект

Проверенный template находится в `data/templates/electron2d-empty`.

Минимальный проект содержит:

- `Electron2D.Empty.csproj` - C# project с dependency на `Electron2D`;
- `Program.cs` - entry point, который читает `project.e2d.json`, загружает main scene path и запускает script sample;
- `project.e2d.json` - project settings;
- `scenes/main.scene.json` - первая сцена;
- `Scripts/MainScene.cs` - пример C# script.

Проверенный запуск template делает verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1
```

Ручной запуск уже созданного проекта выполняется стандартной командой .NET:

```powershell
dotnet run --project .\Electron2D.Empty.csproj
```

Ожидаемый output содержит:

```text
Electron2D empty scene loaded: scenes/main.scene.json
Electron2D C# script lifecycle: _EnterTree,_Ready
Electron2D C# script services: tree=True,text=True
```

<!-- user-doc:first-scene -->
## Первая сцена

Main scene задаётся в `project.e2d.json`:

```json
{
  "mainScene": "scenes/main.scene.json"
}
```

Текущий scene file baseline остаётся JSON-first и diff-friendly. Runtime template проверяет наличие файла сцены и завершает запуск с ошибкой, если путь отсутствует или пустой.

<!-- user-doc:scripting -->
## C# scripting

Текущий scripting baseline использует обычные C# classes, которые наследуются от `Node`.

Проверенный пример находится в `data/templates/electron2d-empty/Scripts/MainScene.cs`. Он показывает lifecycle callbacks `_EnterTree()` и `_Ready()`, а также доступ к runtime services через `GetTree()` и `RenderingServer`.

Editor workflow для создания script file, attach к узлу, встроенного редактирования code text и сборки проекта реализован как внутренняя модель редактора. Пользовательский script остаётся обычным `.cs` файлом проекта и компилируется вместе с проектом.

<!-- user-doc:resources -->
## Resources

В текущем baseline реализованы:

- stable `ResourceUid`;
- internal `.e2res` resource document model;
- import cache отдельно от source assets;
- PNG/JPEG texture metadata import;
- TTF/OTF font metadata import;
- shader source artifact import;
- scene/resource serialization baseline;
- AOT-safe metadata registry для custom `Resource` serialization.

Реальный pixel decoding/GPU upload, glyph rasterization, public `ResourceLoader`/`ResourceSaver` и editor import UI ещё не являются готовым пользовательским workflow.

<!-- user-doc:physics -->
## Physics

Текущий physics baseline покрывает:

- `PhysicsServer2D` RID boundary;
- `StaticBody2D`, `RigidBody2D`, `Area2D`, `CollisionShape2D`, `RayCast2D`;
- `Shape2D` resources;
- collision layers и masks;
- overlap signals;
- direct space ray/point/shape queries;
- fixed physics tick;
- базовое движение `RigidBody2D`;
- `CharacterBody2D` kinematic movement;
- debug collision shape snapshots.

Production solver, full contact manifold, gravity integration, rigid-rigid collision и mobile physics AOT proof остаются отдельными задачами.

<!-- user-doc:ui -->
## UI

Текущий UI baseline включает `Control`, `Label`, alignment types и text measurement/draw baseline. Полный набор controls, containers, theme overrides, tooltips и editor-internal widgets ещё не закрыт.

Документация по UI должна использовать только реализованные controls, пока задачи `Control` layout, containers и basic controls не завершены.

<!-- user-doc:animation -->
## Animation

Текущий animation baseline включает:

- `SpriteFrames`;
- `AnimatedSprite2D`;
- `Animation`;
- `AnimationLibrary`;
- `AnimationPlayer`;
- `Tween`.

Editor timeline data model и специализированные editor views ещё не закрыты. Runtime animation data уже можно проверять через tests, но не нужно документировать незавершённый editor timeline как готовый UI.

<!-- user-doc:input-map -->
## Input Map

Текущий input baseline включает:

- `InputEventKey`, `InputEventMouseButton`, `InputEventMouseMotion`;
- gamepad input types;
- mobile touch/drag/navigation state;
- `InputMap`, action bindings и `Input.GetVector()`;
- persistence через project settings.

Editor Input Map UI ещё не готов. Для текущего baseline action settings проверяются через tests и JSON project settings.

<!-- user-doc:renderer-profiles -->
## Renderer profiles

Текущий renderer baseline имеет два public profiles: `Compatibility` и `Standard`.

Проверяйте профиль и возможности через `RenderingServer.CurrentProfile` и `RenderingServer.HasFeature(...)`. Это важнее, чем проверять платформу вручную: automatic fallback на Android может выбрать более простой профиль, если устройство не проходит smoke-check.

Подробная страница: [Renderer profiles](renderer-profiles.md).

<!-- user-doc:export -->
## Export

Desktop export baseline:

- Windows x64: `tools\Verify-WindowsExport.ps1`;
- Linux x64 glibc: `tools\Verify-LinuxExport.ps1`;
- macOS arm64: `tools\Verify-MacOSExport.ps1` на macOS arm64 host.
- WebAssembly browser planning: `e2d export plan-web --project <project-root> --format json`.
- WebAssembly browser package: `e2d export build-web --project <project-root> --output exports/web --skip-publish true --format json`.
- WebAssembly browser smoke artifact: `e2d export run-web --project <project-root> --output exports/web --url http://127.0.0.1:8080/index.html --smoke-output .electron2d/export-smoke/web-smoke.json --format json`.

Подробный entry point: [Export guide](../export/export-guide.md).

Android и iOS export сейчас заблокированы окружением и real-device/simulator smoke requirements. Не используйте mobile export как готовый release path, пока соответствующие задачи не будут закрыты:

- [Android arm64 export](../export/android-arm64-export.md);
- [iOS arm64 export](../export/ios-arm64-export.md).

iOS export имеет CLI planning/staging path:

- `e2d export plan-ios --project <project-root> --format json`;
- `e2d export build-ios --project <project-root> --output exports/ios/debug --skip-publish true --format json`;
- `e2d export run-ios --project <project-root> --output exports/ios/debug --smoke-output .electron2d/export-smoke/ios-smoke.json --format json`.

`run-ios` пишет blocked smoke artifact, если simulator/device evidence отсутствует; это не закрывает iOS release gate.

WebAssembly browser export имеет planner, package builder и локальный smoke artifact. `build-web --skip-publish true` создаёт host page, loader, manifest и runtime resources без внешнего publish; обычный `build-web` пытается выполнить `dotnet publish` только при подходящих WebAssembly build tools. `run-web` сохраняет structured smoke artifact и launch URL; remote hosting deploy не выполняется. Подробности: [WebAssembly browser export](../export/webassembly-browser-export.md).

После правки export-документации запускайте:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ExportDocumentation.ps1
```

<!-- user-doc:troubleshooting -->
## Troubleshooting

Если template не запускается:

- проверьте .NET SDK `10.0.x`;
- выполните `dotnet restore src\Electron2D.sln`;
- запустите `powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1`;
- проверьте, что `project.e2d.json` содержит непустой `mainScene`;
- проверьте, что `scenes/main.scene.json` копируется в output.

Если desktop export не проходит:

- запустите platform-specific verifier на соответствующей host OS;
- не передавайте реальные signing secrets в repository files;
- проверьте runtime identifier: `win-x64`, `linux-x64` или `osx-arm64`;
- проверьте, что export preset использует `selfContained: true`.

Если документация меняется:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UserDocumentation.ps1
```

Verifier проверяет обязательные разделы, ключевые команды, запрещённые публичные формулировки и image links.

Подробная страница: [Troubleshooting guide и release checklist](troubleshooting-release-checklist.md).

<!-- user-doc:release-checklist -->
## Release checklist

Перед ручной проверкой preview-кандидата выполните project template check, полный test runner, документационные checks, API/Wiki checks и desktop export checks для поддерживаемых host OS.

Минимальный локальный набор:

- `tools\Verify-ProjectTemplate.ps1`;
- `tools\Run-Tests.ps1`;
- `tools\Verify-UserDocumentation.ps1`;
- `tools\Verify-ExportDocumentation.ps1`;
- `tools\Verify-WindowsExport.ps1`;
- `tools\Verify-LinuxExport.ps1`;
- `tools\Verify-MacOSExport.ps1`.

Android/iOS export smoke, reference games performance metrics, leak verification и GitHub Release publication не считаются закрытыми без отдельных задач и явной команды пользователя.
WebAssembly browser release acceptance требует свежий browser smoke artifact для проверяемого project package.

Подробный список: [Troubleshooting guide и release checklist](troubleshooting-release-checklist.md).
