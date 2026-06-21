# Platform input event mapping и Electron2D `InputEvent*`

Статус: целевая спецификация для `T-0048`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Node и SceneTree lifecycle](../object-model/node-scene-tree-lifecycle.md).

## Назначение

`0.1.0 Preview` должен принимать базовые platform input events и передавать в `Node._Input(InputEvent)` Electron2D события. Нативные types не входят в публичный API Electron2D: они остаются внутренней платформенной границей.

В этой задаче закрывается desktop baseline:

- keyboard key down/up;
- mouse button down/up;
- mouse motion;
- mouse wheel;
- committed text input.

Gamepad, touch, mobile navigation, orientation и safe area описаны отдельными input baseline документами. Этот документ фиксирует keyboard, mouse, wheel и committed text mapping.

## Public API

Публичный baseline должен использовать только Electron2D типы:

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

Отдельный публичный `InputEventText` не добавляется, потому что Electron2D путь для текстового ввода в этом baseline - `InputEventKey.Unicode`.

## Keyboard

Platform key down/up мапятся в `InputEventKey`.

Требования:

- `Pressed` соответствует down/up состоянию;
- `Echo` соответствует platform repeat;
- `Keycode` хранит layout key label в Electron2D `Key`;
- `PhysicalKeycode` хранит US QWERTY physical key в Electron2D `Key`;
- `KeyLabel` совпадает с `Keycode` в текущем baseline;
- `Unicode` для key down/up без text input равен `0`;
- `ShiftPressed`, `AltPressed`, `CtrlPressed`, `MetaPressed` берутся из platform key modifiers;
- left/right modifiers выставляют `KeyLocation`.

## Text input

Committed text input мапится в один или несколько `InputEventKey`.

Требования:

- каждый Unicode scalar value становится отдельным `InputEventKey`;
- `Pressed` равен `true`;
- `Keycode`, `PhysicalKeycode` и `KeyLabel` равны `Key.None`;
- `Unicode` хранит code point;
- invalid или empty UTF-8 text fail closed: событие не создаётся.

## Mouse button

Platform mouse button down/up мапятся в `InputEventMouseButton`.

Требования:

- `ButtonIndex` использует Electron2D `MouseButton`;
- `Pressed` соответствует down/up состоянию;
- `DoubleClick` выставляется, если platform click count больше или равен `2`;
- `Position` и `GlobalPosition` получают platform mouse coordinates;
- `Device` и `WindowId` сохраняют platform device/window identifiers.

## Mouse motion

Platform mouse motion мапится в `InputEventMouseMotion`.

Требования:

- `Position` и `GlobalPosition` получают platform mouse coordinates;
- `Relative` и `ScreenRelative` получают platform relative coordinates;
- `ButtonMask` конвертируется из platform button flags в Electron2D `MouseButtonMask`;
- `Velocity` и `ScreenVelocity` остаются `Vector2.Zero`, пока runtime frame timing не подключён к input pump.

## Mouse wheel

Platform mouse wheel мапится в `InputEventMouseButton`, как в Electron2D модели wheel button constants.

Требования:

- положительный vertical wheel создаёт `MouseButton.WheelUp`;
- отрицательный vertical wheel создаёт `MouseButton.WheelDown`;
- положительный horizontal wheel создаёт `MouseButton.WheelRight`;
- отрицательный horizontal wheel создаёт `MouseButton.WheelLeft`;
- platform flipped wheel direction инвертирует оси перед выбором `MouseButton`;
- `Pressed` равен `true`;
- `Factor` хранит абсолютную величину wheel delta;
- `Position` и `GlobalPosition` получают platform mouse coordinates.

## Dispatch order

Internal platform dispatcher должен сохранять порядок platform event queue:

1. poll next platform event;
2. convert it to zero, one or several Electron2D `InputEvent` objects;
3. dispatch each mapped event через `SceneTree.DispatchInput()` before polling the next platform event.

Если один platform event создаёт несколько `InputEvent`, их порядок должен соответствовать порядку scalar values или осей wheel mapping внутри этого event.

## Проверки

- Integration tests проверяют keyboard key down/up mapping.
- Integration tests проверяют mouse button, mouse motion и wheel mapping.
- Integration tests проверяют committed text input в `InputEventKey.Unicode`.
- Integration tests проверяют dispatch order через `SceneTree.DispatchInput()`.
- API compatibility verifier проверяет все новые public Electron2D types в GitHub Wiki source.
- Source license verifier проходит для новых C# files.
