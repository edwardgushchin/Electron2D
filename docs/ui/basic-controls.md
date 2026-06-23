# Спецификация: базовые UI controls

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

## Фактическое состояние, ограничения и проверки

## Назначение
Базовые UI controls Electron2D предоставляют runtime API для простых игровых интерфейсов: панелей, кнопок, текстового ввода, слайдеров, индикаторов прогресса и текстурных прямоугольников.

Эта реализация относится только к runtime библиотеке `Electron2D`. Редактор не должен начинаться, пока весь UI API, включая эти controls, не будет реализован, протестирован, задокументирован и отмечен как `Supported` в GitHub Wiki.

## Состав
- `Panel` рисует фон control.
- `BaseButton` реализует общее поведение кнопок, focus, input и сигналы.
- `Button` добавляет текстовую кнопку.
- `CheckBox` добавляет toggle-кнопку с флажком.
- `TextureButton` выбирает текстуру по состоянию кнопки.
- `LineEdit` реализует однострочный редактируемый текст.
- `Range` хранит числовое значение с bounds, ratio и step.
- `Slider` предоставляет пользовательское изменение `Range`.
- `ProgressBar` визуализирует `Range`.
- `TextureRect` отображает текстуру с режимами растяжения.
- `NinePatchRect` хранит patch margins и отображает UI-текстуру.

## Input и focus
Кнопочные controls принимают mouse, touch, keyboard и gamepad input через существующий `Viewport` dispatch. Disabled-кнопки не получают focus и не активируются.

`LineEdit` принимает printable key input, базовые клавиши редактирования и submit через `Enter`/`KpEnter`. Ограничение `MaxLength` применяется до изменения `Text`.

`Slider` меняет `Value` через pointer position, keyboard arrows, `Home`, `End` и gamepad d-pad.

## Сигналы
Controls используют существующую систему сигналов `Object.Connect` и `Object.EmitSignal`.

Кнопки отправляют:
- `button_down`
- `button_up`
- `pressed`
- `toggled`

`LineEdit` отправляет:
- `text_changed`
- `text_submitted`
- `text_change_rejected`

`Range` отправляет:
- `value_changed`

## Rendering
Controls используют canvas drawing commands из `CanvasItem`. Для текстурных UI controls добавлен внутренний путь рисования texture rect, чтобы публичный API `CanvasItem` не расширялся преждевременно.

## Проверки
Для задачи должны проходить:

```powershell
dotnet test src/Electron2D.sln
dotnet build src/Electron2D.sln -c Release
```

GitHub Wiki обновляется командой проекта:

```powershell
pwsh tools/Update-ApiWiki.ps1
```
