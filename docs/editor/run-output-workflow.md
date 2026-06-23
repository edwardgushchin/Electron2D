# Run/output workflow редактора

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `0.1.0 Preview`.
Задача: `T-0084`.
Обновлено: 2026-06-22.

## Цель

Редактор должен давать разработчику проверяемый цикл запуска без внешней оболочки команд: собрать проект, запустить main scene, запустить выбранную scene, остановить запущенный процесс, показать вывод процесса, ошибки компиляции, shader diagnostics, stack trace и базовые показатели frame timing.

Модель работает внутри редактора и не добавляет новый публичный runtime API. Внешний контракт для проекта остаётся обычным .NET project plus `project.e2d.json`.

## Требования

- `Run Project` собирает проект и запускает scene из `project.e2d.json`.
- `Run Current Scene` собирает проект и запускает выбранный scene file без изменения `project.e2d.json`.
- Если build падает, runtime process не стартует, а редактор показывает compiler diagnostics с file, line, column, code и message.
- Shader diagnostics, уже полученные import pipeline, должны попадать в тот же diagnostics view с file, line, column и message.
- Output console должна хранить stdout и stderr текущего run session в порядке получения строк.
- Runtime exception должен сохранять stack trace так, чтобы пользователь видел место падения.
- `Stop` должен завершать active run session и очищать состояние запуска без падения следующего run.
- Повторные run/stop cycles не должны оставлять активный session после stop.
- Frame timing должен хранить количество samples, последний frame time, средний frame time и FPS.

## Проверка

Integration smoke `--run-workflow-smoke` должен создать временный проект и подтвердить:

1. build diagnostics с ошибкой C# до запуска;
2. успешный `Run Project`;
3. успешный `Run Current Scene` с override scene;
4. output console содержит строки обоих успешных запусков;
5. runtime exception даёт stack trace;
6. shader diagnostic содержит line/column;
7. stop завершает долгий process;
8. несколько run/stop cycles подряд не оставляют active session;
9. frame timing имеет ненулевые samples, FPS и frame time.

## Фактическое состояние, ограничения и проверки

Статус: реализуется в `T-0084`.
Обновлено: 2026-06-22.

## Назначение

Run/output workflow - это модель внутри редактора, которая управляет запуском пользовательского проекта и собирает понятные результаты запуска. Она не является публичным API движка и не требует от пользователя запускать отдельные команды вручную.

## Поведение

Модель должна уметь запускать проектную scene из `project.e2d.json` и выбранную current scene. Для current scene редактор передаёт override в процесс запуска, не меняя project settings file на диске.

Перед запуском выполняется build. Если build неуспешен, процесс игры не стартует, а редактор показывает compiler diagnostics: путь к файлу, строку, колонку, код ошибки и сообщение. Эти данные используются output panel и будущими кликабельными ошибками.

После успешного build редактор запускает собранный `.dll` напрямую через `dotnet`, а не через `dotnet run`. Это значит, что active run session соответствует процессу игры, поэтому `Stop` может завершить его без промежуточного CLI-процесса.

`EditorRunSession` публикует `ProcessId` active game process. Managed debugger использует этот id только для attach к процессу игры, запущенному самим Editor; произвольный attach к чужим процессам не является частью baseline `0.1.0`.

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
