# Mobile input baseline

Статус: реализованный baseline.
Задача: `T-0051`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` получил минимальный mobile input surface без public native handles.

Публичные event resources:

- `InputEventScreenTouch`;
- `InputEventScreenDrag`.

Публичный display/input support surface:

- `DisplayServer`;
- `DisplayServer.ScreenOrientation`;
- `DisplayServer.VirtualKeyboardType`;
- `DisplayServer.GetDisplaySafeArea()`;
- `DisplayServer.ScreenGetOrientation(int screen = -1)`;
- `DisplayServer.ScreenSetOrientation(DisplayServer.ScreenOrientation orientation, int screen = -1)`;
- `DisplayServer.VirtualKeyboardGetHeight()`;
- `DisplayServer.VirtualKeyboardHide()`;
- `DisplayServer.VirtualKeyboardShow(...)`;
- `Key.Back`;
- `Key.Menu`.

## Touch events

Touch press, release and cancel events are delivered to `_Input()` as `InputEventScreenTouch`.

`InputEventScreenTouch` stores:

- `WindowId`;
- `Device`;
- `Index`;
- `Position`;
- `Pressed`;
- `DoubleTap`;
- `Canceled`.

Touch motion events are delivered to `_Input()` as `InputEventScreenDrag`.

`InputEventScreenDrag` stores:

- `WindowId`;
- `Device`;
- `Index`;
- `Position`;
- `Relative`;
- `ScreenRelative`;
- `Velocity`;
- `ScreenVelocity`;
- `Pressure`;
- `Tilt`;
- `PenInverted`.

Invalid touch device or finger ids fail closed and do not create public events.

## Text input and IME

Committed text input continues to arrive as `InputEventKey` with `Unicode` set and keycode fields left as `Key.None`.

Composition and candidate updates are accepted by the internal input boundary without public event resources in this baseline. Text controls and composition UI remain part of later UI/text tasks.

## Mobile navigation

Back and menu navigation are represented as `InputEventKey`:

- back navigation uses `Key.Back`;
- menu navigation uses `Key.Menu`.

These events follow the regular `_Input()` path.

## DisplayServer state

`DisplayServer` stores a compact process-wide state for mobile input and export pipelines.

`ScreenSetOrientation()` records the requested orientation. Invalid enum values are ignored. `ScreenGetOrientation()` returns the current state.

`GetDisplaySafeArea()` returns the last platform-reported safe area, or an empty `Rect2I` before platform startup reports one.

`VirtualKeyboardShow()` records the requested text, edited rectangle, keyboard type, maximum length and selection range. `VirtualKeyboardHide()` clears the request and sets `VirtualKeyboardGetHeight()` to `0`.

## Ограничения

- Gesture recognizers are not implemented in this baseline.
- Sensors, accelerometer and gyroscope are not implemented in this baseline.
- Text composition UI is not implemented until text controls are available.
- Real Android/iOS device smoke runners are covered by later export tasks.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~MobileInputTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
