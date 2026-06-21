# Platform input event mapping и Electron2D `InputEvent*`

Статус: реализованный baseline.
Задача: `T-0048`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` получил desktop baseline для platform input events. Нативный backend остаётся internal platform boundary, а пользовательский код получает Electron2D events через `Node._Input(InputEvent)`.

Публичные типы:

- `InputEvent`;
- `InputEventFromWindow`;
- `InputEventWithModifiers`;
- `InputEventKey`;
- `InputEventMouse`;
- `InputEventMouseButton`;
- `InputEventMouseMotion`;
- `Key`;
- `KeyLocation`;
- `MouseButton`;
- `MouseButtonMask`.

## Platform mapping

Internal platform mapper преобразует:

- key down/up в `InputEventKey`;
- mouse button down/up в `InputEventMouseButton`;
- mouse motion в `InputEventMouseMotion`;
- mouse wheel в `InputEventMouseButton` с `MouseButton.Wheel*`;
- text input в один или несколько `InputEventKey` с заполненным `Unicode`.

Отдельный public `InputEventText` не добавлен: Electron2D модель использует `InputEventKey.Unicode` для текстового ввода.

## Dispatch order

Internal dispatcher читает platform events, мапит каждое событие в zero/one/many `InputEvent` и сразу отправляет их через `SceneTree.DispatchInput()`.

Порядок сохраняется:

- platform event queue обрабатывается последовательно;
- события, полученные из одного text input, идут в порядке Unicode scalar values;
- wheel axis events создаются в стабильном порядке: vertical, затем horizontal.

## Ограничения

- `InputMap`, action persistence, deadzones и global `Input` API реализованы в `T-0049` и описаны в [InputMap, action state и persistence baseline](input-map-actions.md).
- Gamepad lifecycle/mapping остаётся задачей `T-0050`.
- Touch, multitouch, virtual keyboard, IME composition, mobile navigation, orientation и safe area остаются задачей `T-0051`.
- UI focus/mouse filter pipeline остаётся задачей `T-0052`.
- `InputEventMouseMotion.Velocity` и `ScreenVelocity` пока равны `Vector2.Zero`, пока frame timing не подключён к input pump.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~SdlInputEventMappingTests"
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
