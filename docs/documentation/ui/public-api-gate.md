# UI public API gate

Статус: документация реализации для `T-0129`.
Дата: 2026-06-22.

## Назначение

UI public API gate фиксирует момент, когда runtime UI/Text surface готов для следующих editor-задач. Gate не добавляет новый public runtime API; он связывает уже реализованные UI задачи с повторяемой проверкой GitHub Wiki compatibility table.

## Что проверяется

Verifier `tools/Verify-UiPublicApiGate.ps1` читает generated Wiki page `.github/wiki/API-UI-and-Text.md`, строит список public UI/Text типов и сверяет эти типы с `.github/wiki/API-Compatibility.md`.

Проверка проходит только если каждая UI/Text строка имеет статус `Supported`. Любой `Partial`, `Experimental`, `Planned` или отсутствующая строка проваливает gate и оставляет editor work заблокированным.

## Доказательная база

Текущий UI/Text surface закрыт задачами:

- `T-0029` и `T-0038`: `Font`, `Label`, text layout, Unicode/RTL baseline, fallback и cache.
- `T-0067`: anchors/offsets, minimum size, grow direction, clipping hit-test и focus navigation.
- `T-0068`: containers, size flags, scroll offsets и `EnsureControlVisible`.
- `T-0069`: basic controls, disabled/focused states, mouse/keyboard/gamepad/touch input, text input and drawing.
- `T-0070`: theme lookup, local overrides, DPI scaling, style boxes and tooltips.
- `T-0071`: structured controls for lists, trees, popup menus and tabs.
- `T-0106` and `T-0107`: complete XML documentation and generated Wiki API reference.

## Проверки

Основной UI gate:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki
```

Полный набор проверок перед закрытием gate:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1 -WikiPath .github\wiki\API-Compatibility.md
powershell -ExecutionPolicy Bypass -File tools\Update-ApiWiki.ps1 -OutputPath .github\wiki -Check
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github\wiki
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiXmlDocs.ps1 -FailOnIssues
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
```

CI запускает UI gate после проверки generated Wiki API reference, чтобы устаревший статус `Partial` не мог разблокировать editor tasks.
