# Документация реализации: базовые UI controls

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
