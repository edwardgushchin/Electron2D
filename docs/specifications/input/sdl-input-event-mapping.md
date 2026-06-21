# SDL input event mapping и Godot-like `InputEvent*`

Статус: целевая спецификация для `T-0048`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Node и SceneTree lifecycle](../object-model/node-scene-tree-lifecycle.md).

## Назначение

`0.1.0 Preview` должен принимать базовые SDL3 input events и передавать в `Node._Input(InputEvent)` Godot-like события. SDL types не входят в публичный API Electron2D: они остаются внутренней платформенной границей.

В этой задаче закрывается desktop baseline:

- keyboard key down/up;
- mouse button down/up;
- mouse motion;
- mouse wheel;
- SDL text input.

Gamepad, touch, multitouch, IME composition, mobile back/navigation, orientation и safe area остаются отдельными задачами из input backlog.

## Public API

Публичный baseline должен использовать только Godot-like типы:

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

Отдельный публичный `InputEventText` не добавляется, потому что Godot-like путь для текстового ввода в этом baseline - `InputEventKey.Unicode`.

## Keyboard

SDL `KeyDown` и `KeyUp` мапятся в `InputEventKey`.

Требования:

- `Pressed` соответствует down/up состоянию;
- `Echo` соответствует SDL repeat;
- `Keycode` хранит layout key label в Godot-like `Key`;
- `PhysicalKeycode` хранит US QWERTY physical key в Godot-like `Key`;
- `KeyLabel` совпадает с `Keycode` в текущем baseline;
- `Unicode` для key down/up без SDL text input равен `0`;
- `ShiftPressed`, `AltPressed`, `CtrlPressed`, `MetaPressed` берутся из SDL key modifiers;
- left/right modifiers выставляют `KeyLocation`.

## Text input

SDL `TextInput` мапится в один или несколько `InputEventKey`.

Требования:

- каждый Unicode scalar value становится отдельным `InputEventKey`;
- `Pressed` равен `true`;
- `Keycode`, `PhysicalKeycode` и `KeyLabel` равны `Key.None`;
- `Unicode` хранит code point;
- invalid или empty UTF-8 text fail closed: событие не создаётся.

## Mouse button

SDL `MouseButtonDown` и `MouseButtonUp` мапятся в `InputEventMouseButton`.

Требования:

- `ButtonIndex` использует Godot-like `MouseButton`;
- `Pressed` соответствует down/up состоянию;
- `DoubleClick` выставляется, если SDL clicks больше или равен `2`;
- `Position` и `GlobalPosition` получают SDL mouse coordinates;
- `Device` и `WindowId` сохраняют SDL `Which` и `WindowID`.

## Mouse motion

SDL `MouseMotion` мапится в `InputEventMouseMotion`.

Требования:

- `Position` и `GlobalPosition` получают SDL mouse coordinates;
- `Relative` и `ScreenRelative` получают SDL relative coordinates;
- `ButtonMask` конвертируется из SDL button flags в Godot-like `MouseButtonMask`;
- `Velocity` и `ScreenVelocity` остаются `Vector2.Zero`, пока runtime frame timing не подключён к input pump.

## Mouse wheel

SDL `MouseWheel` мапится в `InputEventMouseButton`, как в Godot-like модели wheel button constants.

Требования:

- положительный vertical wheel создаёт `MouseButton.WheelUp`;
- отрицательный vertical wheel создаёт `MouseButton.WheelDown`;
- положительный horizontal wheel создаёт `MouseButton.WheelRight`;
- отрицательный horizontal wheel создаёт `MouseButton.WheelLeft`;
- SDL flipped wheel direction инвертирует оси перед выбором `MouseButton`;
- `Pressed` равен `true`;
- `Factor` хранит абсолютную величину wheel delta;
- `Position` и `GlobalPosition` получают SDL mouse coordinates.

## Dispatch order

Internal SDL dispatcher должен сохранять порядок SDL event queue:

1. poll next SDL event;
2. convert it to zero, one or several Godot-like `InputEvent` objects;
3. dispatch each mapped event через `SceneTree.DispatchInput()` before polling the next SDL event.

Если один SDL event создаёт несколько `InputEvent`, их порядок должен соответствовать порядку scalar values или осей wheel mapping внутри этого event.

## Проверки

- Integration tests проверяют keyboard key down/up mapping.
- Integration tests проверяют mouse button, mouse motion и wheel mapping.
- Integration tests проверяют SDL text input в `InputEventKey.Unicode`.
- Integration tests проверяют dispatch order через `SceneTree.DispatchInput()`.
- API compatibility verifier проверяет все новые public Godot-like types в GitHub Wiki source.
- Source license verifier проходит для новых C# files.
