# VS Code запуск примеров

Обновлено: 2026-06-24.

Этот файл является единым доменным документом для запуска примеров Electron2D из Visual Studio Code. Он описывает ожидаемый пользовательский workflow, текущую конфигурацию, ограничения и проверки.

## Ведение документа

- Перед изменением `.vscode/launch.json` или `.vscode/tasks.json` обновите ожидаемый контракт в этом файле.
- Затем добавьте или обновите focused tests, измените конфигурацию и выполните проверки.
- Если список примеров меняется, синхронизируйте этот документ, `.vscode` профили и тесты в одном изменении.

## Контракт и ожидаемое поведение

Пользователь должен запускать Platformer из VS Code без ручного ввода команды в терминал, сохраняя обычный debug target VS Code:

1. открыть репозиторий в VS Code;
2. перейти в `Run and Debug`;
3. выбрать конфигурацию `Electron2D: Platformer`;
4. нажать зелёную кнопку запуска;
5. дождаться открытия окна Platformer.

Запуск не должен использовать `examples/reference-platformer/ReferencePlatformer.csproj` напрямую, потому что этот project file является библиотекой. Окно, цикл кадров, ввод и screenshot-support создаёт CLI runner Electron2D:

```text
src/Electron2D.Cli/Electron2D.Cli.csproj -- run --project examples/reference-platformer
```

В `launch.json` это выражено иначе: VS Code сначала выполняет task `Electron2D: build CLI`, затем запускает уже собранный CLI с аргументами `run --project examples/reference-platformer`. На Windows debug target использует apphost `src/Electron2D.Cli/bin/Debug/net10.0/e2d.exe`, чтобы отладчик стартовал обычный исполняемый файл. Базовый `program` остаётся `src/Electron2D.Cli/bin/Debug/net10.0/e2d.dll` для сред, где .NET debug adapter запускает CLI через assembly file.

## Текущая конфигурация

`.vscode/launch.json` содержит один launch profile:

- `Electron2D: Platformer`;
- `type = coreclr`;
- `program = ${workspaceFolder}/src/Electron2D.Cli/bin/Debug/net10.0/e2d.dll`;
- `windows.program = ${workspaceFolder}/src/Electron2D.Cli/bin/Debug/net10.0/e2d.exe`;
- `preLaunchTask = Electron2D: build CLI`;
- `args = run --project ${workspaceFolder}/examples/reference-platformer`.

`launch.json` не использует `inputs`, потому что после удаления второго reference project активным example остаётся только Platformer. `pickString` в VS Code блокирует старт до ручного выбора, поэтому для одного проекта он не подходит. Когда появятся новые примеры, лучше добавить отдельные debug profiles с понятными именами, например `Electron2D: Platformer` и `Electron2D: Another Example`, чтобы зелёная кнопка всегда запускала выбранный профиль без дополнительного всплывающего выбора.

`.vscode/tasks.json` содержит:

- `Electron2D: build CLI` - собирает CLI runner;
- `Electron2D: run Platformer` - ручной task для запуска Platformer из палитры команд VS Code.

## Ограничения

После удаления второго reference project в `T-0211` активным example остаётся только `examples/reference-platformer`. Поэтому профиль запускает его напрямую без окна выбора.

`coreclr` launch profile требует установленное C# расширение VS Code, которое поддерживает .NET debugging. Это именно debug target: брейкпоинты в загруженном managed code могут срабатывать, когда отладчик видит соответствующие символы. Без C# debug adapter можно использовать `Terminal: Run Task...` и task `Electron2D: run Platformer`, но этот task является запасным ручным запуском, а не заменой debug target.

## Проверка

Focused test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~VsCodeExampleLaunchTests" --no-restore -m:1
```

Дополнительная ручная проверка:

1. открыть репозиторий в VS Code;
2. открыть `Run and Debug`;
3. выбрать `Electron2D: Platformer`;
4. нажать зелёную кнопку запуска;
5. убедиться, что открывается окно Platformer.
