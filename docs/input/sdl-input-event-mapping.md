# Platform input event mapping и Electron2D `InputEvent*`

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0048`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md), [Node и SceneTree lifecycle](../object-model/node-scene-tree-lifecycle.md).

## Назначение

`0.1-preview` должен принимать базовые platform input events и передавать в `Node._Input(InputEvent)` Electron2D события. Нативные types не входят в публичный API Electron2D: они остаются внутренней платформенной границей.

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

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0048`.
Обновлено: 2026-06-21.

## Что реализовано

Electron2D `0.1-preview` получил desktop baseline для platform input events. Нативный backend остаётся internal platform boundary, а пользовательский код получает Electron2D events через `Node._Input(InputEvent)`.

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
- Gamepad lifecycle/mapping описан в `gamepad-input.md`.
- Touch, virtual keyboard, mobile navigation, orientation и safe area описаны в `mobile-input.md`.
- UI focus/mouse filter pipeline реализован в [Input dispatch, UI focus и mouse filter baseline](input-dispatch-ui-focus.md).
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
