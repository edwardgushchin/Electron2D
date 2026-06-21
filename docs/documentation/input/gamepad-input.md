# Gamepad input baseline

Статус: реализованный baseline.
Задача: `T-0050`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1.0 Preview` получил desktop gamepad baseline поверх internal platform input boundary. Public API работает только с Electron2D types и не раскрывает platform handles.

Публичные типы:

- `JoyAxis`;
- `JoyButton`;
- `InputEventJoypadButton`;
- `InputEventJoypadMotion`.

Публичные методы `Input`:

- `GetConnectedJoypads()`;
- `GetJoyAxis(int device, JoyAxis axis)`;
- `GetJoyName(int device)`;
- `GetJoyVibrationDuration(int device)`;
- `GetJoyVibrationStrength(int device)`;
- `IsJoyButtonPressed(int device, JoyButton button)`;
- `IsJoyKnown(int device)`;
- `StartJoyVibration(int device, float weakMagnitude, float strongMagnitude, float duration = 0f)`;
- `StopJoyVibration(int device)`.

## Lifecycle

Internal platform device events update process-wide gamepad state:

- connected devices are returned by `Input.GetConnectedJoypads()` in ascending order;
- metadata-less devices have an empty name and `IsJoyKnown(device) == false`;
- disconnect removes axis, button and vibration state;
- a button or axis event for an unknown device creates a connected placeholder state.

Lifecycle updates are not delivered to `_Input()` as public event resources. User code observes them through `Input` query methods.

## Buttons and axes

Button events are delivered to `_Input()` as `InputEventJoypadButton` and update `Input.IsJoyButtonPressed()`.

Axis events are delivered to `_Input()` as `InputEventJoypadMotion` and update `Input.GetJoyAxis()`. Axis values are normalized to `-1.0` through `1.0`.

Unknown button or axis ids fail closed: no public event is created.

## InputMap

`InputMap` accepts gamepad bindings:

- `InputEventJoypadButton` matches by `ButtonIndex`;
- `InputEventJoypadMotion` matches by `Axis` and the sign of `AxisValue`;
- button action strength uses `Pressure` when available and otherwise uses `1.0` for pressed digital button events;
- axis action strength uses absolute axis value after the action deadzone;
- axis values inside the deadzone release the corresponding action binding;
- runtime state keys include device id, so the same binding can be held by several devices independently.

Internal input settings serialization now persists `joy_button` and `joy_motion` bindings alongside keyboard and mouse bindings.

## Vibration

`StartJoyVibration()` records vibration state only for connected devices marked by the internal platform boundary as vibration-capable.

Unsupported or unknown devices are safe no-ops:

- no exception is thrown;
- `GetJoyVibrationStrength()` returns `Vector2.Zero`;
- `GetJoyVibrationDuration()` returns `0.0`.

For supported devices, magnitudes are clamped to `0.0` through `1.0`. `StopJoyVibration()` clears stored strength and duration.

## Ограничения

- Real native haptic handles remain internal and are not part of public API.
- Device ids are runtime-local and must not be persisted in project files.
- Mobile-specific gamepad lifecycle interactions remain part of the later mobile input and export tasks.
- Gamepad touchpads, sensors, LEDs and raw device-specific effects are not implemented in this baseline.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~GamepadInputTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~InputMapActionTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
