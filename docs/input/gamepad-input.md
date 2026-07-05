# Gamepad input baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0050`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md), [InputMap, action state и persistence baseline](input-map-actions.md).

## Назначение

`0.1-preview` должен принимать gamepad lifecycle, button и axis события через internal platform input boundary и передавать пользовательскому коду стабильные Electron2D events. Public API не должен раскрывать platform handles, native pointers или backend-specific controller objects.

Задача закрывает desktop gamepad baseline:

- подключение и отключение устройств;
- button down/up events;
- analog axis motion;
- action bindings для gamepad buttons и axes через `InputMap`;
- состояние connected devices, button states и axis values через `Input`;
- vibration API с безопасным no-op, если устройство не подключено или не поддерживает vibration.

Touch, multitouch, virtual keyboard, committed text input, mobile navigation, orientation и safe area описаны в отдельном mobile input baseline.

## Public API

Добавляется только целевой input API:

- `JoyAxis`;
- `JoyButton`;
- `InputEventJoypadButton`;
- `InputEventJoypadMotion`;
- `Input.GetConnectedJoypads()`;
- `Input.GetJoyAxis(int device, JoyAxis axis)`;
- `Input.GetJoyName(int device)`;
- `Input.GetJoyVibrationDuration(int device)`;
- `Input.GetJoyVibrationStrength(int device)`;
- `Input.IsJoyButtonPressed(int device, JoyButton button)`;
- `Input.IsJoyKnown(int device)`;
- `Input.StartJoyVibration(int device, float weakMagnitude, float strongMagnitude, float duration = 0f)`;
- `Input.StopJoyVibration(int device)`.

`InputEventJoypadButton` хранит:

- `ButtonIndex`;
- `Pressed`;
- `Pressure`.

`InputEventJoypadMotion` хранит:

- `Axis`;
- `AxisValue`.

## Device lifecycle

Internal platform device events must update the process-wide `Input` state:

- connected devices are visible through `Input.GetConnectedJoypads()` in ascending order;
- unknown device ids return empty name, `false` for known state, zero axis values and released buttons;
- disconnect removes button, axis and vibration state for that device;
- receiving a button or axis event for an unknown device creates a connected placeholder state with an empty name and unknown mapping.

## Buttons

Button events are delivered to `Node._Input(InputEvent)` as `InputEventJoypadButton`.

Requirements:

- `Device` stores the stable runtime device id;
- `ButtonIndex` maps to `JoyButton`;
- `Pressed` matches button down/up state;
- `Pressure` is clamped to `0.0` through `1.0`; digital button events use `1.0` while pressed and `0.0` while released;
- `Input.IsJoyButtonPressed(device, button)` reflects the last dispatched state.

## Axes

Axis events are delivered to `Node._Input(InputEvent)` as `InputEventJoypadMotion`.

Requirements:

- `Device` stores the stable runtime device id;
- `Axis` maps to `JoyAxis`;
- `AxisValue` is normalized to `-1.0` through `1.0`;
- invalid or unknown axes fail closed and do not create public events;
- `Input.GetJoyAxis(device, axis)` reflects the last dispatched value.

## InputMap actions

`InputMap` must accept gamepad bindings:

- `InputEventJoypadButton` bindings match by `ButtonIndex`;
- `InputEventJoypadMotion` bindings match by `Axis` and sign of `AxisValue`;
- action strength for button bindings uses `Pressure` when it is greater than zero and otherwise uses `1.0` for pressed digital events;
- action strength for axis bindings uses absolute axis value after action deadzone;
- when an axis value returns inside the deadzone, the action binding is released;
- state keys include the runtime device id so the same binding can be pressed by several devices independently.

## Vibration

`Input.StartJoyVibration()` and `Input.StopJoyVibration()` operate on process-wide device state.

Requirements:

- `weakMagnitude` and `strongMagnitude` are finite and clamped to `0.0` through `1.0`;
- `duration` is finite and less than or equal to zero for an indefinite effect;
- unsupported or unknown devices do not throw and do not record active vibration;
- supported devices store requested strength and duration until stopped, disconnected or replaced by another vibration request;
- `Input.GetJoyVibrationStrength()` returns `(weak, strong)`;
- `Input.GetJoyVibrationDuration()` returns the requested duration value for the active effect, or `0.0` when none is active.

## Dispatch order

Internal platform dispatcher must process gamepad events in queue order:

1. consume lifecycle updates;
2. convert button/axis events to Electron2D `InputEvent` objects;
3. dispatch each mapped event through `SceneTree.DispatchInput()`.

Lifecycle updates are not delivered to `_Input()` as public event resources. User code observes them through the `Input` query methods.

## Проверки

- Integration tests verify connected/disconnected device state.
- Integration tests verify button and axis events reach `_Input()` in platform queue order.
- Integration tests verify `InputMap` action matching for buttons and signed axes, including release when an axis returns inside the deadzone.
- Integration tests verify vibration requests are stored for supported devices and are no-op for unsupported/unknown devices.
- API compatibility verifier checks new public Electron2D types in GitHub Wiki source.
- Source license verifier passes for new C# files.

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0050`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1-preview` получил desktop gamepad baseline поверх internal platform input boundary. Public API работает только с Electron2D types и не раскрывает platform handles.

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
