# Input dispatch, UI focus и mouse filter baseline

Статус: реализованный baseline.
Задача: `T-0052`.
Обновлено: 2026-06-22.

## Что реализовано

Electron2D `0.1.0 Preview` получил базовый порядок доставки input events в сцену и минимальный GUI-input путь для `Control`.

Публичные additions:

- `Viewport.SetInputAsHandled()`;
- `Node.GetViewport()`;
- `Control._GuiInput(InputEvent inputEvent)`;
- `Control.AcceptEvent()`;
- `Control.GrabFocus()`;
- `Control.ReleaseFocus()`;
- `Control.HasFocus()`;
- `Control.MouseFilter`;
- `Control.FocusMode`;
- `MouseFilter`;
- `FocusMode`.

## Порядок dispatch

`SceneTree.DispatchInput()` теперь выполняет один event pass так:

1. обновляет process-wide `Input` state;
2. сбрасывает handled-state root `Viewport`;
3. вызывает `Node._Input(InputEvent)` по текущему tree order;
4. если event не handled, вызывает GUI input dispatch для `Control`;
5. завершает обычные deferred/delete queues через `SceneTree`.

Если пользовательский callback вызывает `Viewport.SetInputAsHandled()` или `Control.AcceptEvent()`, текущий event больше не передаётся следующим node/control callbacks. Состояние `Input` не откатывается: action state уже обновлён до callbacks.

## Mouse filter

Mouse GUI dispatch использует `InputEventMouse.Position` и прямоугольник `Control.GlobalPosition` plus `Control.Size`.

`MouseFilter` работает так:

- `Stop` вызывает `_GuiInput()` и затем помечает event handled, если callback сам не сделал этого раньше;
- `Pass` вызывает `_GuiInput()` и, если event не handled, продолжает bubble path к parent `Control`;
- `Ignore` пропускает self-handler control при mouse hit-test.

Для overlapping siblings первым проверяется более поздний child по scene order. Hidden controls и controls с нулевым или отрицательным размером не получают GUI input.

## Focus

Focus хранится на root `Viewport`. В один момент focused может быть только один visible `Control`.

`GrabFocus()` работает только для control внутри `SceneTree`, visible in tree и с `FocusMode`, отличным от `None`. `ReleaseFocus()` очищает focus, если вызывающий control сейчас focused. `HasFocus()` возвращает `false` для hidden controls, detached controls и invalid controls.

Mouse press по control с `FocusMode.Click` или `FocusMode.All` делает его focused до вызова `_GuiInput()`. Keyboard, gamepad и direct action events без mouse coordinates передаются в `_GuiInput()` только focused control.

## Ограничения

- Focus neighbor graph, keyboard/gamepad navigation between controls, hover signals, tooltip timing and full widgets остаются будущими UI/editor задачами.
- `SceneTree.DispatchInput()` остаётся internal entry point для platform dispatcher и tests; публичный event injection workflow будет отдельной automation/CLI задачей.
- GUI input baseline не выполняет physics picking и не добавляет shortcut/unhandled input callbacks.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~InputDispatchControlTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
