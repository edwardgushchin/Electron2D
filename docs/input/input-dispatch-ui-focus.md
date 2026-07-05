# Input dispatch, UI focus и mouse filter baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0052`.
Обновлено: 2026-06-22.
Связанные документы: [Electron2D 0.1-preview](../releases/0.1-preview.md), [Input event mapping и `InputEvent*`](sdl-input-event-mapping.md), [InputMap, action state и persistence baseline](input-map-actions.md), [Text backend baseline](../rendering/text-backend-baseline.md).

## Назначение

`0.1-preview` должен иметь предсказуемый путь доставки input events в сцену и минимальную UI-ветку для `Control`. Эта задача закрывает базовый порядок распространения событий, handled-state для текущего события, прямоугольный hit-test `Control`, mouse filter и focus ownership.

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

## Фактическое состояние, ограничения и проверки

Статус: реализованный baseline.
Задача: `T-0052`.
Обновлено: 2026-06-22.

## Что реализовано

Electron2D `0.1-preview` получил базовый порядок доставки input events в сцену и минимальный GUI-input путь для `Control`.

Публичные additions:

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

## Порядок dispatch

`SceneTree.DispatchInput()` теперь выполняет один event pass так:

1. обновляет process-wide `Input` state;
2. сбрасывает handled-state root `Viewport`;
3. вызывает `Node._Input(InputEvent)` по текущему tree order;
4. если event не handled, вызывает GUI input dispatch для `Control`;
5. завершает обычные deferred/delete queues через `SceneTree`.

Если пользовательский callback вызывает `Viewport.SetInputAsHandled()` или `Control.AcceptEvent()`, текущий event больше не передаётся следующим node/control callbacks. Состояние `Input` не откатывается: action state уже обновлён до callbacks.

## Mouse filter

Mouse GUI dispatch использует `InputEventMouse.Position` и прямоугольник `Control.GlobalPosition` plus `Control.Size`.

`MouseFilter` работает так:

- `Stop` вызывает `_GuiInput()` и затем помечает event handled, если callback сам не сделал этого раньше;
- `Pass` вызывает `_GuiInput()` и, если event не handled, продолжает bubble path к parent `Control`;
- `Ignore` пропускает self-handler control при mouse hit-test.

Для overlapping siblings первым проверяется более поздний child по scene order. Hidden controls и controls с нулевым или отрицательным размером не получают GUI input.

## Focus

Focus хранится на root `Viewport`. В один момент focused может быть только один visible `Control`.

`GrabFocus()` работает только для control внутри `SceneTree`, visible in tree и с `FocusMode`, отличным от `None`. `ReleaseFocus()` очищает focus, если вызывающий control сейчас focused. `HasFocus()` возвращает `false` для hidden controls, detached controls и invalid controls.

Mouse press по control с `FocusMode.Click` или `FocusMode.All` делает его focused до вызова `_GuiInput()`. Keyboard, gamepad и direct action events без mouse coordinates передаются в `_GuiInput()` только focused control.

## Ограничения

- Focus neighbor graph, keyboard/gamepad navigation between controls, hover signals, tooltip timing and full widgets остаются будущими UI/editor задачами.
- `SceneTree.DispatchInput()` остаётся internal entry point для platform dispatcher и tests; публичный event injection workflow будет отдельной automation/CLI задачей.
- GUI input baseline не выполняет physics picking и не добавляет shortcut/unhandled input callbacks.

## Проверки

Сфокусированная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~InputDispatchControlTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
