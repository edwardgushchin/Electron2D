# Спецификация: базовые UI controls

## Статус
- Задача: `T-0069`
- Статус: утверждено для реализации
- Последнее обновление: `2026-06-22T03:18:00+03:00`

## Контекст
Electron2D 0.1.0 Preview должен иметь базовый набор runtime controls до начала работ над редактором. Эти controls нужны для игровых HUD, debug-панелей, меню, экранов настроек и будущего редактора, но задача реализует только runtime API библиотеки `Electron2D`.

UI API считается готовым для этой задачи только когда типы, свойства, методы, сигналы, XML-документация, тесты и GitHub Wiki отражают фактическую реализацию. В таблице совместимости нельзя оставлять эти controls как `Partial`, если задача закрывается.

## Область реализации
Нужно добавить публичные runtime-типы:

- `Panel`
- `BaseButton`
- `Button`
- `TextureButton`
- `CheckBox`
- `LineEdit`
- `Range`
- `Slider`
- `ProgressBar`
- `TextureRect`
- `NinePatchRect`

## Публичное поведение

### Общие требования
- Все controls наследуются от `Control` напрямую или через базовый UI-класс.
- Все публичные свойства, методы, поля и вложенные enum имеют XML-документацию полного формата проекта.
- Каждый исходный файл содержит MIT-заголовок проекта.
- Controls используют существующую систему дерева сцены, input dispatch, focus и сигналов.
- Controls не добавляют legacy compatibility API.

### Panel
- `Panel` является простым фоновым `Control`.
- `_Draw` должен отправлять команду заливки прямоугольника размера `Size`.
- `Panel` не добавляет публичных свойств сверх унаследованных.

### BaseButton, Button, CheckBox, TextureButton
- `BaseButton` является базой для кнопочных controls.
- `BaseButton` поддерживает сигналы `button_down`, `button_up`, `pressed`, `toggled`.
- `BaseButton` поддерживает свойства:
  - `ActionMode`
  - `ButtonPressed`
  - `Disabled`
  - `ToggleMode`
- Disabled-кнопка не принимает focus и игнорирует input.
- При `ActionMode.ButtonRelease` активация происходит при release внутри control или при завершении клавиатурного/геймпадного нажатия.
- При `ActionMode.ButtonPress` активация происходит при press.
- Toggle-кнопка меняет `ButtonPressed` при активации и отправляет `toggled(bool)`.
- `Button` добавляет текстовое содержимое через `Text`.
- `CheckBox` является toggle-кнопкой с флажком и текстом.
- `TextureButton` выбирает текстуру по состоянию и поддерживает:
  - `TextureNormal`
  - `TexturePressed`
  - `TextureHover`
  - `TextureDisabled`
  - `TextureFocused`
  - `TextureClickMask`
  - `IgnoreTextureSize`
  - `StretchMode`

### LineEdit
- `LineEdit` является однострочным текстовым input control.
- `LineEdit` поддерживает сигналы `text_changed`, `text_submitted`, `text_change_rejected`.
- `LineEdit` поддерживает свойства:
  - `Text`
  - `PlaceholderText`
  - `Editable`
  - `Secret`
  - `SecretCharacter`
  - `MaxLength`
  - `CaretColumn`
  - `HorizontalAlignment`
- `LineEdit` должен принимать фокус по mouse/touch/keyboard focus flow.
- Printable key input вставляет символы в caret.
- `Backspace`, `Delete`, `Left`, `Right`, `Home`, `End`, `Enter` и `KpEnter` должны работать для базового редактирования.
- `MaxLength` ограничивает ввод, а отклоненный остаток отправляется через `text_change_rejected`.
- `Enter` и `KpEnter` отправляют `text_submitted(Text)`.
- При `Editable == false` изменение текста через input игнорируется.

### Range, Slider, ProgressBar
- `Range` является базой для controls со скалярным значением.
- `Range` поддерживает сигнал `value_changed(double)`.
- `Range` поддерживает свойства:
  - `MinValue`
  - `MaxValue`
  - `Step`
  - `Page`
  - `Value`
  - `Ratio`
  - `Rounded`
  - `AllowGreater`
  - `AllowLesser`
  - `ExpEdit`
- `Range.SetValueNoSignal(double)` меняет значение без сигнала.
- Значение clamps/snap-ится согласно bounds, `Step`, `Rounded`, `AllowGreater`, `AllowLesser`.
- `Slider` добавляет ввод мышью, touch, клавиатурой и геймпадом для изменения `Value`.
- `Slider.Editable == false` запрещает пользовательские изменения.
- `ProgressBar` визуализирует значение `Range` и не принимает input.

### TextureRect
- `TextureRect` отображает `Texture2D` внутри своего `Control` rect.
- `TextureRect` поддерживает свойства:
  - `Texture`
  - `ExpandMode`
  - `StretchMode`
  - `FlipH`
  - `FlipV`
- Minimum size зависит от `ExpandMode`.
- Stretch mode должен рассчитывать destination rect для scale/keep/keep centered/aspect/aspect centered/aspect covered.

### NinePatchRect
- `NinePatchRect` отображает `Texture2D` как прямоугольный UI resource с patch margins.
- `NinePatchRect` поддерживает свойства:
  - `Texture`
  - `RegionRect`
  - `DrawCenter`
  - `PatchMarginLeft`
  - `PatchMarginTop`
  - `PatchMarginRight`
  - `PatchMarginBottom`
  - `AxisStretchHorizontal`
  - `AxisStretchVertical`
- Minimum size равен сумме patch margins по соответствующим осям.

## Приемочные критерии
- Добавлены unit-тесты публичного API базовых controls.
- Добавлены integration-тесты input, focus, signals и drawing behavior для базовых controls.
- `dotnet test src/Electron2D.sln` проходит.
- `dotnet build src/Electron2D.sln -c Release` проходит.
- `tools/Update-ApiWiki.ps1` обновляет GitHub Wiki без локального сайта.
- В `.github/wiki/API-Compatibility.md` строки для controls из этой задачи имеют статус `Supported`.
- Активная задача перенесена в `completed-tasks/2026/06 Июнь.md` только после успешных проверок.
