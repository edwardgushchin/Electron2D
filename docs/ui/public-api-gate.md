# UI public API gate

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0129`.
Дата: 2026-06-22.

## Цель

Перед началом `Electron2D.Editor` публичный UI API runtime должен быть закрыт как полный supported surface, а не как набор частичных строк в таблице совместимости. Gate подтверждает, что все типы из generated Wiki category `API-UI-and-Text.md` реализованы, протестированы, задокументированы и имеют статус `Supported` в `API-Compatibility.md`.

## Scope

Gate распространяется на все public types из категории `UI and Text`:

- базовый `Control` layer, focus, mouse filter, grow direction и size flags;
- UI containers: `Container`, `BoxContainer`, `HBoxContainer`, `VBoxContainer`, `GridContainer`, `MarginContainer`, `CenterContainer`, `ScrollContainer`;
- theme, style boxes, DPI scaling и tooltip source API;
- text baseline: `Font`, `Label`, horizontal/vertical alignment;
- basic controls: panels, buttons, text input, ranges, slider, progress bar, texture controls и nine-patch control;
- structured controls: item list, tree, popup menu and tab container.

Editor-only widgets не должны становиться публичным API runtime. Если будущая editor-задача требует нового публичного UI type/member, gate должен оставаться незакрытым до отдельной runtime-задачи.

## Требования

- `API-UI-and-Text.md` является источником списка UI/Text типов.
- Для каждого UI/Text типа должна существовать строка в `API-Compatibility.md`.
- Каждая такая строка должна иметь статус `Supported`.
- Статус нельзя менять вручную без подтверждения реализации: спецификация, документация, XML comments, generated Wiki pages и automated tests должны соответствовать фактическому поведению.
- CI должен запускать отдельный verifier UI gate после проверки generated Wiki API reference.
- `Electron2D.Editor` и зависящие editor tasks остаются заблокированными до прохождения verifier.

## Приемочные критерии

- `tools/Verify-UiPublicApiGate.ps1 -WikiPath .github/wiki` падает на любой UI/Text строке со статусом `Partial`, `Experimental` или `Planned`.
- `tools/Verify-UiPublicApiGate.ps1 -WikiPath .github/wiki` проходит, когда все UI/Text строки имеют статус `Supported`.
- `tools/Verify-ApiCompatibility.ps1 -WikiPath .github/wiki/API-Compatibility.md` проходит.
- `tools/Update-ApiWiki.ps1 -OutputPath .github/wiki -Check` проходит.
- `tools/Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki` проходит.
- `tools/Verify-PublicApiXmlDocs.ps1 -FailOnIssues` проходит.
- `tools/Run-Tests.ps1` проходит.

## Фактическое состояние, ограничения и проверки

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
