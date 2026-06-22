# UI public API gate

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
