# Unicode, IME и текст справа налево

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

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

## Фактическое состояние, ограничения и проверки

Документ описывает фактически реализованный preview-level baseline после `T-0077`.

## Unicode

`Font`, `CanvasItem.DrawString()` и `Label` работают с .NET `string`, а internal layout перечисляет строку через Unicode scalar values. Благодаря этому surrogate pair, например emoji, попадает в layout как один codepoint.

`Font.HasChar(int charCode)` возвращает `false` для невалидного Unicode scalar value. Для валидного codepoint проверка делегируется текущему font resource и его fallback chain.

## IME

Подтверждённый системный text input передаётся в runtime как последовательность `InputEventKey`:

- `Pressed = true`;
- `Keycode = Key.None`;
- `PhysicalKeycode = Key.None`;
- `KeyLabel = Key.None`;
- `Unicode` хранит codepoint подтверждённого символа.

Если система передала строку из нескольких Unicode scalar values, Electron2D создаёт несколько событий в том же порядке. Это покрывает обычный keyboard text input, подтверждённый ввод через IME и emoji.

`0.1.0 Preview` не реализует public API для промежуточной IME-композиции: нет preedit text, selection range, candidate window control или отдельного `InputEventText`.

## Текст справа налево

Text layout выбирает `RightToLeft`, если строка содержит символ из Hebrew/Arabic Unicode ranges. Glyph records в таком layout идут в визуальном порядке справа налево, а позиции считаются от правого края строки с учётом `HorizontalAlignment`.

Смешанный UI text проходит через обычный `Label` path. Интеграционный тест `LabelSubmitsMixedUnicodeRtlTextThroughReferenceUi` фиксирует строку `Score אב🙂`: Latin text, Hebrew text и emoji оказываются в одном layout, Hebrew glyphs выбираются из fallback font, а emoji остаётся одним codepoint.

## Ограничения

- Полный Unicode Bidirectional Algorithm не реализован.
- Shaping, ligatures и language-specific glyph substitution не реализованы.
- Grapheme cluster navigation, text selection, cursor movement и editable text controls не входят в текущий baseline.
- Multiline layout, wrapping, clipping и overrun behavior остаются задачами будущего UI/text layer.

## Проверки

Сфокусированные проверки:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~TextLayoutSubmissionTests|FullyQualifiedName~SdlInputEventMappingTests" --no-restore -m:1
dotnet test tests\Electron2D.Tests.GoldenData\Electron2D.Tests.GoldenData.csproj --filter "FullyQualifiedName~TextLayoutGoldenTests" --no-restore -m:1
```

Полная проверка проекта:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```
