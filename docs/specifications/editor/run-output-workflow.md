# Run/output workflow редактора

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
