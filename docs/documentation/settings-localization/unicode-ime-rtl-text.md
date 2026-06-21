# Unicode, IME и текст справа налево

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
