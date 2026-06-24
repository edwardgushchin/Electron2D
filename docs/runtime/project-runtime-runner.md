# Project runtime runner

Обновлено: 2026-06-24.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для исправления playable reference games `0.1.0 Preview`.
Обновлено: 2026-06-24.
Связанные документы: [Reference platformer](../examples/reference-platformer.md), [Headless runtime automation](headless-runtime-automation.md), [Runtime debug bridge и scene inspection](runtime-debug-bridge.md).

## Назначение

Electron2D должен иметь минимальный project runtime runner — инфраструктурный механизм запуска проекта, который не становится отдельным публичным API-типом и не вызывается из пользовательского кода. Пользовательский игровой код создаёт `Node`, `Node2D`, UI controls и resources, описывает поведение через callbacks (`_Ready`, `_Process`, `_PhysicsProcess`, `_Input`, `_Draw`) и остаётся внутри существующего профиля Electron2D, совместимого с выбранным API-подмножеством Godot. Окно, event loop, перевод платформенного ввода в `InputEvent`, продвижение кадров, построение canvas draw commands, показ кадра и screenshot выполняет runner проекта: CLI или Editor.

Reference games не должны иметь собственный оконный loop, `Program.cs`, прямые вызовы backend/window/input библиотек, `Console.ReadKey` или ASCII-псевдоинтерфейс. Игровые скрипты должны быть обычными `Node`/`Control`-скриптами на API Electron2D. Runner движка не должен попадать в API manifest, GitHub Wiki или compatibility таблицы как публичный тип и не должен быть доступен проектам reference games через `InternalsVisibleTo`.

## Public API boundary

Запрещено добавлять эти публичные API-типы:

- `Electron2DApplication`;
- `Electron2DRunOptions`;
- `Electron2DRunResult`;
- любой другой отдельный public static application/bootstrap class, отсутствующий в выбранном API-подмножестве Godot.

Минимальный непубличный runner, доступный CLI, Editor и автоматическим тестам, имеет право принимать:

- main `Node` или готовый `SceneTree`;
- title и logical window size;
- `FrameLimit`, где `0` означает обычный интерактивный запуск до закрытия окна, а число больше `0` означает автоматический smoke/script run;
- `FixedDelta`;
- optional `ScreenshotPath`;
- `QuitOnEscape`;
- clear color.

Runner возвращает machine-readable результат для CLI/tests: `Succeeded`, `WindowCreated`, `WindowShown`, `FramePresented`, `EventPumpObserved`, `InputEventsDispatched`, `FrameCount`, `DrawCommands`, window/pixel size, video driver, screenshot path/status и diagnostic message.

Правила:

- runner может быть internal API движка, доступным тестам, CLI и Editor через контролируемую сборочную границу;
- runner не должен расширять public API manifest и не должен требовать XML documentation как публичный API;
- публичные игровые скрипты используют только существующие `SceneTree`, `Node`, `Node2D`, `CanvasItem`, `Control`, resource, rendering и input API;
- невалидные параметры fail closed через исключение до открытия окна.

## Project launch contract

`e2d run --project <project-root>` является пользовательской точкой запуска проекта в Preview. Команда должна:

- прочитать `project.e2d.json` и применить project settings, включая Input Map и display settings;
- найти `mainScene`, прочитать root `script` из scene JSON и загрузить C# assembly проекта;
- создать root node из пользовательского script class через обычную сценовую модель проекта;
- передать `ProjectRoot` в script, если script предоставляет такое свойство;
- открыть runtime window через непубличный runner движка;
- для автоматических проверок принять `--play-script <commands>` и `--screenshot <path>`, вызвать script-level playable entrypoint только как test/acceptance hook и вернуть machine-readable результат.

Reference games должны собираться как project assemblies и запускаться через `e2d run --project ...`, а не через `dotnet run` пользовательского проекта.

## Runtime loop

Loop должен:

- создать пользовательское окно с указанным title и размером;
- установить `SceneTree.Root` как `Viewport.Size = RuntimeHostOptions.WindowSize` до `_Ready()` при запуске main `Node` и перед первым кадром при запуске готового `SceneTree`;
- переводить platform input events в публичные `InputEvent*` и отправлять их в `SceneTree`;
- вызывать physics/process/draw callbacks через существующий `SceneTree`;
- строить draw commands через canvas submission;
- показывать последний frame в окне;
- сохранять screenshot, если задан `ScreenshotPath`;
- возвращать machine-readable результат для verifier/tests.

Внутренний backend не раскрывается наружу. Пользовательский код не получает native handles и не зависит от платформенных packages напрямую.

## Acceptance criteria

- `Electron2DApplication`, `Electron2DRunOptions` и `Electron2DRunResult` отсутствуют среди exported public types runtime assembly.
- Project runtime runner реализован в `src/Electron2D/`, а project launch command реализован в `src/Electron2D.Cli/`; ни один из них не является helper внутри `examples/`.
- Focused test создаёт `Node2D`, рисует через `_Draw`, запускает internal project runtime runner, получает visible/window markers и PNG screenshot.
- Focused test подтверждает, что root `Viewport.Size` равен logical window size уже в `_Ready()` и `_Process()`.
- Reference platformer запускается командой `e2d run --project ...` в режиме разработки или через export player, не содержит `Program.cs`, не вызывает `RuntimeHost`, `ProjectRuntimeRunner` или другой engine-owned bootstrap из user code.
- Reference games не содержат `Console.ReadKey`, ASCII frame output, прямые вызовы `SDL`/window backend или custom event loop.
- `e2d run --project examples/reference-platformer --play-script ... --screenshot <path>` возвращает `WindowCreated=True`, `WindowShown=True`, `FramePresented=True`, `DrawCommands > 0`, `ScreenshotPath=<path>` и создаёт PNG нормального размера.
- Implementation documentation в `docs/runtime/` описывает фактический scope и ограничения project runtime runner.
- API compatibility, API manifest, GitHub Wiki generation, source license/header checks и focused reference verifiers проходят, не добавляя out-of-profile public bootstrap API.

## Фактическое состояние, ограничения и проверки

`Project runtime runner` - это внутренний механизм запуска игры из CLI, Editor и export player. Он не является публичным API для пользовательского кода: игровые проекты описывают сцену, ресурсы и скрипты, а runner создаёт окно, продвигает `SceneTree`, отправляет input events, строит draw commands и сохраняет screenshot.

## Текущее поведение

- `RuntimeHost.Run(Node, RuntimeHostOptions?)` создаёт новый `SceneTree`, устанавливает размер root `Viewport` из `RuntimeHostOptions.WindowSize`, добавляет main scene и запускает loop.
- `RuntimeHost.Run(SceneTree, RuntimeHostOptions?)` синхронизирует размер root `Viewport` с `RuntimeHostOptions.WindowSize` перед первым кадром уже готового дерева.
- Размер root `Viewport` доступен уже в `_Ready()` при запуске через overload с main `Node`. Это важно для `Camera2D`, `CanvasLayer`, `Control` anchors, touch normalization через `GetVisibleRect()` и screenshot-проверок.
- `FrameLimit > 0` выполняет bounded smoke loop, то есть короткий автоматический прогон для тестов и verifier-ов. `FrameLimit == 0` оставляет окно работать до закрытия пользователем или до Escape, если `QuitOnEscape == true`.
- `ScreenshotPath`, если задан, записывает PNG последнего отрисованного кадра.

## Границы

- Runner остаётся internal и не добавляет public bootstrap types вроде `Electron2DApplication`, `Electron2DRunOptions` или `Electron2DRunResult`.
- Пользовательские scripts не должны напрямую вызывать runner, оконный backend или platform input backend.
- Runtime host сейчас использует logical window size из options как размер root viewport. Отдельный resize event path для live window resizing остаётся вне этой правки.

## Проверки

Focused проверка runtime host:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RuntimeHostTests"
```
