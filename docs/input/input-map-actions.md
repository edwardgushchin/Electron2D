# InputMap, action state и persistence baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0049`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Input event mapping и `InputEvent*`](sdl-input-event-mapping.md).

## Назначение

`0.1.0 Preview` должен дать игре стабильный action-level ввод поверх уже существующих `InputEvent*`. Пользовательский код должен работать с действиями вроде `jump`, `move_left` и `attack`, а не вручную проверять каждую клавишу или кнопку мыши.

Baseline закрывает:

- `InputMap` как общий registry действий, deadzone и input bindings;
- `Input` как общий action state с `IsActionPressed()`, `IsActionJustPressed()`, `GetActionStrength()` и `GetVector()`;
- direct action events для тестов, инструментов и будущей runtime automation;
- simultaneous bindings, когда одно действие нажато несколькими событиями одновременно;
- внутренний serializer action settings для будущего project settings layer.

## Public API

Публичная поверхность `T-0049`:

- `InputEventAction : InputEvent`:
  - `Action`;
  - `Pressed`;
  - `Strength`.
- `InputMap`:
  - `AddAction(string action, float deadzone = 0.5f)`;
  - `EraseAction(string action)`;
  - `HasAction(string action)`;
  - `GetActions()`;
  - `ActionSetDeadzone(string action, float deadzone)`;
  - `ActionGetDeadzone(string action)`;
  - `ActionAddEvent(string action, InputEvent inputEvent)`;
  - `ActionEraseEvent(string action, InputEvent inputEvent)`;
  - `ActionEraseEvents(string action)`;
  - `ActionGetEvents(string action)`;
  - `EventIsAction(InputEvent inputEvent, string action, bool exactMatch = false)`.
- `Input`:
  - `IsActionPressed(string action, bool exactMatch = false)`;
  - `IsActionJustPressed(string action, bool exactMatch = false)`;
  - `GetActionStrength(string action, bool exactMatch = false)`;
  - `GetVector(string negativeX, string positiveX, string negativeY, string positiveY, float deadzone = -1f)`.

`exactMatch` зарезервирован для будущей проверки modifier state и platform-specific distinctions. В текущем baseline клавиатурные bindings сравниваются по logical key или physical key, mouse bindings - по button index, direct action events - по имени action.

## Action registry

`InputMap.AddAction()` создаёт action с deadzone по умолчанию `0.5`. Deadzone должна быть конечным числом в диапазоне `0..1`. Повторный `AddAction()` для существующего action не удаляет bindings и не меняет deadzone.

`InputMap.GetActions()` возвращает стабильный отсортированный список action names. `ActionGetEvents()` возвращает snapshots, чтобы внешний код не менял stored bindings через полученный массив.

Unknown action не должен автоматически появляться при `ActionAddEvent()`, `ActionSetDeadzone()` или query-вызовах. Методы изменения unknown action бросают `ArgumentException`; query-вызовы возвращают безопасные значения: `false`, `0`, пустой список.

## Event matching

Поддерживаемые bindings в `T-0049`:

- `InputEventKey` - key down/up matching по `Keycode`, затем по `PhysicalKeycode`, если соответствующее поле binding не равно `Key.None`;
- `InputEventMouseButton` - matching по `ButtonIndex`;
- `InputEventAction` - direct action event matching по `Action`.

Text input через `InputEventKey.Unicode` не считается action binding. Mouse motion, touch drag и screen touch events не считаются action bindings в этом baseline. Gamepad bindings описаны отдельным baseline.

## Action state

`SceneTree.DispatchInput()` должен обновлять `Input` перед вызовом `_Input(InputEvent)`, чтобы `_Input()` callbacks могли читать актуальное состояние action.

Для keyboard и mouse bindings state хранит активные physical bindings per action. Если действие нажато двумя bindings одновременно, release одного binding не отпускает action, пока остаётся второй active binding.

`IsActionJustPressed()` возвращает `true` только в кадре, где action перешёл из released/zero strength в pressed/positive strength. Переходные flags очищаются после `SceneTree.ProcessFrame()` и `SceneTree.PhysicsFrame()`.

`InputEventAction` задаёт strength напрямую. Если `Pressed == false`, strength action становится `0`. Если `Pressed == true`, strength ограничивается диапазоном `0..1` и сравнивается с deadzone action. Strength ниже или равный deadzone считается released.

## Vector strength

`Input.GetActionStrength()` возвращает:

- `1` для pressed digital action;
- direct action strength для direct action event;
- `0` для released action, unknown action и action ниже deadzone.

`Input.GetVector(negativeX, positiveX, negativeY, positiveY, deadzone)` строит вектор:

- `x = strength(positiveX) - strength(negativeX)`;
- `y = strength(positiveY) - strength(negativeY)`.

Если длина вектора больше `1`, результат нормализуется. Если `deadzone >= 0` и длина результата меньше или равна `deadzone`, возвращается `Vector2.Zero`. Если `deadzone < 0`, используется среднее значение deadzone четырёх actions.

## Persistence

`T-0049` не добавляет публичные file I/O методы на `InputMap`. Вместо этого вводится internal serializer для будущих project settings. Он должен сохранять и читать:

- schema version;
- action names;
- deadzone;
- keyboard bindings;
- mouse button bindings.

Serializer пишет deterministic JSON: actions отсортированы по имени, events отсортированы по type и value. Unknown event types или malformed values fail closed через `FormatException`; успешная load operation заменяет registry целиком только после полного чтения документа.

## Критерии приёмки

- Spec exists before production implementation.
- Red tests подтверждают отсутствие `InputMap`, `Input`, `InputEventAction` и internal persistence serializer.
- Integration tests покрывают action registry, event matching, simultaneous bindings, just-pressed lifetime, deadzone и `GetVector()`.
- Integration tests покрывают save/load round-trip action settings.
- Public API guard и GitHub Wiki source обновлены для новых public types.
- Implementation documentation описывает фактический baseline и ограничения.

## Фактическое состояние, ограничения и проверки

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
