# Unicode, IME и текст справа налево

Статус: целевая спецификация для `T-0077`.
Обновлено: 2026-06-21.
Связанные документы: [Electron2D 0.1.0 Preview](../releases/0.1.0-preview.md), [Translation resource, locale switching и `Tr`](translation-runtime.md), [Text backend baseline](../rendering/text-backend-baseline.md), [Input event mapping и `InputEvent*`](../input/sdl-input-event-mapping.md).

## Назначение

Electron2D `0.1.0 Preview` должен явно описать и проверить минимальный уровень поддержки Unicode, IME и текста справа налево для игр, редактора и экспортируемых desktop-сборок.

`IME` означает Input Method Editor: системный способ ввода текста для языков и раскладок, где пользователь набирает промежуточную композицию, а приложение получает уже подтверждённый текст. В `0.1.0 Preview` Electron2D фиксирует только подтверждённый текст, а не UI композиции.

## Уровень поддержки

### Unicode

- Текст в `Font`, `CanvasItem.DrawString()` и `Label` обрабатывается как Unicode string.
- Layout перечисляет текст через Unicode scalar values, поэтому surrogate pair, например emoji, считается одним codepoint.
- `Font.HasChar(int charCode)` принимает только валидные Unicode scalar values.
- Если glyph не найден в основном font и fallback font, layout всё равно сохраняет glyph record с `GlyphAvailable = false`.

### IME

- Подтверждённый системный text input преобразуется в один или несколько `InputEventKey`.
- Каждый Unicode scalar value из подтверждённого текста становится отдельным событием.
- Для таких событий `Pressed = true`, `Keycode = Key.None`, `PhysicalKeycode = Key.None`, `KeyLabel = Key.None`, а `Unicode` хранит codepoint.
- Preview baseline не добавляет public `InputEventText`, cursor composition API, preedit range API или UI-кандидаты IME.

### Текст справа налево

- Single-line layout выбирает `TextLayoutDirection.RightToLeft`, если строка содержит символ из Hebrew/Arabic Unicode ranges.
- В этом режиме glyph records сохраняются в визуальном порядке справа налево и получают позиции от правого края строки.
- Смешанный LTR/RTL/emoji текст должен проходить через тот же `Label`/`DrawString()` path, что и обычный UI text.
- Preview baseline не реализует полноценный Unicode Bidirectional Algorithm, shaping, ligatures, grapheme cluster caret navigation, multiline layout, wrapping или clipping.

## Публичный API

Новая публичная поверхность для `T-0077` не добавляется. Задача подтверждает поведение уже реализованных типов:

- `InputEventKey.Unicode`;
- `Font`;
- `CanvasItem.DrawString()`;
- `Label`;
- `HorizontalAlignment`;
- `VerticalAlignment`.

Низкоуровневые handles платформенного ввода, text shaping engine и renderer backend не должны попадать в публичный API.

## Проверки

Acceptance набор:

- integration test подтверждает, что committed IME text мапится в `InputEventKey.Unicode` по Unicode scalar values, включая символ за пределами Basic Multilingual Plane;
- integration test через `Label` подтверждает mixed LTR/RTL/emoji layout в UI path;
- документация описывает текущий support level и ограничения без обещания full text engine;
- существующие text layout, fallback font, cache и translation tests продолжают проходить.
