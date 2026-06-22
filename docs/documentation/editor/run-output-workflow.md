# Run/output workflow редактора

Статус: реализуется в `T-0084`.
Обновлено: 2026-06-22.

## Назначение

Run/output workflow - это модель внутри редактора, которая управляет запуском пользовательского проекта и собирает понятные результаты запуска. Она не является публичным API движка и не требует от пользователя запускать отдельные команды вручную.

## Поведение

Модель должна уметь запускать проектную scene из `project.e2d.json` и выбранную current scene. Для current scene редактор передаёт override в процесс запуска, не меняя project settings file на диске.

Перед запуском выполняется build. Если build неуспешен, процесс игры не стартует, а редактор показывает compiler diagnostics: путь к файлу, строку, колонку, код ошибки и сообщение. Эти данные используются output panel и будущими кликабельными ошибками.

После успешного build редактор запускает собранный `.dll` напрямую через `dotnet`, а не через `dotnet run`. Это значит, что active run session соответствует процессу игры, поэтому `Stop` может завершить его без промежуточного CLI-процесса.

Output console собирает stdout и stderr запущенного процесса. Если процесс падает с exception, stderr сохраняется вместе со stack trace, чтобы ошибка была видна без повторного запуска из терминала.

Shader diagnostics берутся из import metadata или результата import pipeline и показываются рядом с compiler diagnostics. Обязательные поля: file, line, column и message.

Frame timing хранит последний frame time, средний frame time, FPS и количество samples. Эти значения предназначены для status bar/overlay редактора.

`Stop` завершает active run session. После stop редактор должен быть готов к следующему запуску.

## Проверка

Основная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorRunWorkflowTests" --no-restore -m:1
```

Прямая smoke-команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --run-workflow-smoke .temp\editor-run-workflow
```

Smoke подтверждает build diagnostics, `Run Project`, `Run Current Scene`, output console, stack trace, shader diagnostic, stop и repeated run/stop cycles.
