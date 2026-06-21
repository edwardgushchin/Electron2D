# Input dispatch, UI focus и mouse filter baseline

Статус: целевая спецификация для `T-0052`.
Обновлено: 2026-06-22.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Input event mapping и `InputEvent*`](sdl-input-event-mapping.md), [InputMap, action state и persistence baseline](input-map-actions.md), [Text backend baseline](../rendering/text-backend-baseline.md).

## Назначение

`0.1.0 Preview` должен иметь предсказуемый путь доставки input events в сцену и минимальную UI-ветку для `Control`. Эта задача закрывает базовый порядок распространения событий, handled-state для текущего события, прямоугольный hit-test `Control`, mouse filter и focus ownership.

Baseline не добавляет полноценные widgets, navigation graph, shortcuts, tooltips, hover signals или text editing controls. Эти части остаются задачами UI/editor layers.

## Public API

Публичная поверхность задачи:

- `Viewport.SetInputAsHandled()`;
- `Node.GetViewport()`;
- `Control._GuiInput(InputEvent inputEvent)`;
- `Control.AcceptEvent()`;
- `Control.GrabFocus()`;
- `Control.ReleaseFocus()`;
- `Control.HasFocus()`;
- `Control.MouseFilter`;
- `Control.FocusMode`;
- `MouseFilter`;
- `FocusMode`.

Новые публичные members должны иметь полную XML-документацию и попасть в generated GitHub Wiki API reference.

## Dispatch order

`SceneTree.DispatchInput()` должен:

1. отклонять `null` input event;
2. обновлять `Input` до пользовательских callbacks;
3. сбрасывать handled-state root `Viewport` для текущего события;
4. вызывать `Node._Input(InputEvent)` по текущему tree order;
5. если событие не handled, выполнить GUI input dispatch для `Control`;
6. завершить traversal и deferred/delete queues так же, как остальные scene passes.

Если `Viewport.SetInputAsHandled()` вызывается во время `_Input()` или `_GuiInput()`, текущий event больше не передаётся следующим nodes или control handlers. Global `Input` state при этом уже обновлён и не откатывается.

## Control mouse dispatch

Mouse events используют `InputEventMouse.Position` в координатах root viewport. В baseline позиция `Control.GlobalPosition` и `Control.Size` задают прямоугольник hit-test.

Правила:

- hidden controls и controls с нулевым или отрицательным размером не получают GUI input;
- `MouseFilter.Ignore` пропускает control и его self-handler для mouse dispatch;
- `MouseFilter.Stop` вызывает `_GuiInput()` и затем помечает событие handled, если handler сам не изменил состояние раньше;
- `MouseFilter.Pass` вызывает `_GuiInput()` и, если событие не handled, bubble-путь продолжается к parent `Control`;
- среди overlapping siblings сначала проверяется более поздний child по scene order, потому что он расположен выше в UI draw order baseline.

## Focus dispatch

Focus baseline хранится на root `Viewport`: в один момент focused может быть только один `Control`.

Правила:

- `GrabFocus()` делает control focused, если он находится внутри `SceneTree`, видим и `FocusMode` не равен `None`;
- `ReleaseFocus()` очищает focus, если текущий focused control совпадает с вызывающим control;
- `HasFocus()` возвращает `true` только для текущего visible focused control;
- mouse press по control с `FocusMode.Click` или `FocusMode.All` вызывает `GrabFocus()` до `_GuiInput()`;
- keyboard/gamepad/action events без mouse coordinates отправляются в `_GuiInput()` только focused control.

## Проверки

- Integration tests проверяют, что `_Input()` видит обновлённый `Input` state и может остановить дальнейшую доставку через `Viewport.SetInputAsHandled()`.
- Integration tests проверяют `Control.MouseFilter.Stop`, `Pass` и `Ignore` на вложенных и overlapping controls.
- Integration tests проверяют focus grab/release и delivery keyboard GUI input только focused control.
- Public API guard, XML documentation verifier, API compatibility verifier и GitHub Wiki generator проходят после обновления public surface.
