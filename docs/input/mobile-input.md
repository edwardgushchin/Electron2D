# Mobile input baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0051`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [InputMap, action state и persistence baseline](input-map-actions.md), [Gamepad input baseline](gamepad-input.md).

## Назначение

`0.1.0 Preview` должен иметь минимальный mobile input surface, достаточный для touch-first игр и будущих Android/iOS smoke checks. Public API не должен раскрывать native handles, platform pointers или backend-specific event objects.

Задача закрывает baseline:

- touch press/release/cancel events;
- touch drag events для multitouch pointers;
- committed text input через существующий `InputEventKey.Unicode`;
- mobile back/menu navigation key constants;
- virtual keyboard methods;
- screen orientation state;
- display safe area state.

Gesture recognizers, sensors, accelerometer, gyroscope, hardware keyboard callbacks, cutouts list, text controls and full mobile export smoke runners остаются отдельными задачами.

## Public API

Добавляются public event resources:

- `InputEventScreenTouch`;
- `InputEventScreenDrag`.

Добавляется public display/input support surface:

- `DisplayServer`;
- `DisplayServer.ScreenOrientation`;
- `DisplayServer.VirtualKeyboardType`;
- `DisplayServer.GetDisplaySafeArea()`;
- `DisplayServer.ScreenGetOrientation(int screen = -1)`;
- `DisplayServer.ScreenSetOrientation(DisplayServer.ScreenOrientation orientation, int screen = -1)`;
- `DisplayServer.VirtualKeyboardGetHeight()`;
- `DisplayServer.VirtualKeyboardHide()`;
- `DisplayServer.VirtualKeyboardShow(string existingText, Rect2 position = default, DisplayServer.VirtualKeyboardType type = DisplayServer.VirtualKeyboardType.Default, int maxLength = -1, int cursorStart = -1, int cursorEnd = -1)`;
- `Key.Back`;
- `Key.Menu`.

## Touch events

`InputEventScreenTouch` хранит:

- `Index`;
- `Position`;
- `Pressed`;
- `DoubleTap`;
- `Canceled`.

`InputEventScreenDrag` хранит:

- `Index`;
- `Position`;
- `Relative`;
- `ScreenRelative`;
- `Velocity`;
- `ScreenVelocity`;
- `Pressure`;
- `Tilt`;
- `PenInverted`.

Requirements:

- touch down maps to `InputEventScreenTouch` with `Pressed == true`;
- touch up maps to `InputEventScreenTouch` with `Pressed == false`;
- touch cancel maps to `InputEventScreenTouch` with `Pressed == false` and `Canceled == true`;
- touch motion maps to `InputEventScreenDrag`;
- `WindowId` stores the runtime window id;
- `Device` stores the runtime touch device id when it fits into `int`;
- `Index` stores the finger id when it fits into `int`;
- invalid ids fail closed and do not create public events;
- `Position`, `Relative`, `ScreenRelative` and `Pressure` are clamped to finite values.

## Text input and IME

Committed text continues to be delivered as `InputEventKey` events with `Unicode` set and keycode fields left as `Key.None`.

Text editing composition and candidate data are accepted by the internal input boundary without creating a public event resource in this baseline. Text controls can consume committed text now and will get composition UI support in later UI/text tasks.

## Mobile navigation

Mobile back/menu navigation is represented as `InputEventKey`:

- back navigation maps to `Key.Back`;
- menu navigation maps to `Key.Menu`.

These events are delivered through the normal `_Input()` path and can be bound through `InputMap` once project actions include those keys.

## Virtual keyboard

`DisplayServer.VirtualKeyboardShow()` records the requested text, edited rectangle, keyboard type, maximum length and selection range for the platform boundary.

Requirements:

- `existingText == null` is treated as an empty string;
- `maxLength < -1` is clamped to `-1`;
- cursor positions less than `-1` are clamped to `-1`;
- `VirtualKeyboardHide()` clears active keyboard state and height;
- `VirtualKeyboardGetHeight()` returns the last platform-reported keyboard height in pixels, or `0` when hidden.

## Orientation and safe area

`DisplayServer.ScreenGetOrientation()` returns the current screen orientation state. The default value is `Landscape`.

`DisplayServer.ScreenSetOrientation()` records the requested orientation. Invalid enum values are ignored and leave the previous orientation intact.

`DisplayServer.GetDisplaySafeArea()` returns the current unobscured display area. The default value is an empty `Rect2I`; platform startup and mobile window events can update it through the internal boundary.

## Проверки

- Integration tests verify touch down/motion/up/cancel mapping and dispatch order.
- Integration tests verify mobile back/menu key mapping.
- Integration tests verify orientation and safe area state updates from the platform boundary.
- Unit tests verify public API guard includes only the intended new public types.
- API compatibility verifier checks the generated public surface source.
- Source license verifier passes for new source files.

## Фактическое состояние, ограничения и проверки

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
