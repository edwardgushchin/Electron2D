# SDL input event mapping и Electron2D `InputEvent*`

Статус: реализованный baseline.
Задача: `T-0048`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` получил desktop baseline для SDL input events. SDL3-CS остаётся internal platform boundary, а пользовательский код получает Electron2D events через `Node._Input(InputEvent)`.

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

## SDL mapping

Internal `SdlInputEventMapper` преобразует:

- SDL `KeyDown`/`KeyUp` в `InputEventKey`;
- SDL `MouseButtonDown`/`MouseButtonUp` в `InputEventMouseButton`;
- SDL `MouseMotion` в `InputEventMouseMotion`;
- SDL `MouseWheel` в `InputEventMouseButton` с `MouseButton.Wheel*`;
- SDL `TextInput` в один или несколько `InputEventKey` с заполненным `Unicode`.

Отдельный public `InputEventText` не добавлен: Electron2D модель использует `InputEventKey.Unicode` для текстового ввода.

## Dispatch order

Internal `SdlInputEventDispatcher` poll-ит SDL events, мапит каждое SDL событие в zero/one/many `InputEvent` и сразу отправляет их через `SceneTree.DispatchInput()`.

Порядок сохраняется:

- SDL event queue обрабатывается последовательно;
- события, полученные из одного SDL text input, идут в порядке Unicode scalar values;
- wheel axis events создаются в стабильном порядке: vertical, затем horizontal.

## Ограничения

- `InputMap`, action persistence, deadzones и global `Input` API остаются задачей `T-0049`.
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
