# InputMap, action state и persistence baseline

Статус: реализованный baseline.
Задача: `T-0049`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` получил action-level ввод поверх `InputEvent*`.

Публичные типы:

- `InputEventAction` - direct action event для тестов, инструментов и будущей runtime automation.
- `InputMap` - process-wide registry action names, deadzone и bindings.
- `Input` - process-wide action state queries.

`InputMap` поддерживает:

- `AddAction()`, `EraseAction()`, `HasAction()`, `GetActions()`;
- `ActionSetDeadzone()` и `ActionGetDeadzone()`;
- `ActionAddEvent()`, `ActionEraseEvent()`, `ActionEraseEvents()`, `ActionGetEvents()`;
- `EventIsAction()`.

`Input` поддерживает:

- `IsActionPressed()`;
- `IsActionJustPressed()`;
- `GetActionStrength()`;
- `GetVector()`.

## Bindings

В этом baseline action bindings работают для:

- `InputEventKey` по `Keycode` или `PhysicalKeycode`;
- `InputEventMouseButton` по `ButtonIndex`;
- `InputEventJoypadButton` по `ButtonIndex`;
- `InputEventJoypadMotion` по `Axis` и знаку `AxisValue`;
- `InputEventAction` по имени `Action`.

`ActionGetEvents()` возвращает copies, поэтому внешний код не может изменить stored bindings через полученный массив. `GetActions()` возвращает отсортированный список action names.

## Action state

`SceneTree.DispatchInput()` сначала обновляет `Input`, затем вызывает `_Input(InputEvent)` у nodes. Поэтому `_Input()` callbacks видят актуальные значения `Input.IsActionPressed()` и `Input.IsActionJustPressed()`.

Simultaneous bindings отслеживаются отдельно: если action нажата двумя bindings, release одного binding не отпускает action, пока второй binding остаётся active.

Gamepad axis bindings используют analog strength. Если axis value возвращается внутрь deadzone или меняет знак относительно binding, соответствующий action binding отпускается.

`IsActionJustPressed()` очищается после `SceneTree.ProcessFrame()` или `SceneTree.PhysicsFrame()`.

## Deadzone и `GetVector()`

`InputEventAction.Strength` ограничивается диапазоном `0..1`. Если strength ниже или равен action deadzone, action считается released и `GetActionStrength()` возвращает `0`.

`GetVector(negativeX, positiveX, negativeY, positiveY, deadzone)` строит вектор из четырёх action strengths. Если длина больше `1`, результат нормализуется. Если `deadzone` отрицательный, используется среднее значение deadzone четырёх actions.

## Persistence

Публичных file I/O методов на `InputMap` нет. Для внутреннего project settings layer, то есть кода движка/редактора вне public API, используется `InputMapProjectSettings`, который сохраняет deterministic JSON:

- format version;
- action names;
- deadzone;
- keyboard bindings;
- mouse button bindings;
- gamepad button bindings;
- gamepad axis bindings.

Load заменяет action registry только после успешного чтения всего документа. Malformed JSON, unknown event types и invalid enum values приводят к `FormatException`.

Начиная с `T-0076`, общий settings store вкладывает эти actions в `project.e2d.json` под `input.actions` и возвращает диагностику вместо исключения для повреждённых project settings.

## Ограничения

- Touch, virtual keyboard и mobile navigation описаны в `mobile-input.md`; screen touch and drag events не являются action bindings в этом baseline.
- UI focus/mouse filter pipeline реализован в [Input dispatch, UI focus и mouse filter baseline](input-dispatch-ui-focus.md).
- Public project settings API и editor UI для Input Map остаются отдельными задачами.
- `exactMatch` зарезервирован для будущей проверки modifier/device distinctions.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~InputMapActionTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
