# Script workspace редактора

Статус: реализовано для `T-0158` и дополнено language services для `T-0159`.
Обновлено: 2026-06-23.

## Назначение

`Script` — центральное рабочее пространство `Electron2D.Editor` для встроенного редактирования C# scripts проекта. Оно показывает файлы script-ов, вкладки открытых документов, область кода, поиск/замену, навигацию по строкам, диагностику, правую панель сведений о текущем `CodeDocument` и UI-состояния C# language services.

Текущая реализация model-first: сначала создаётся проверяемая модель состояния UI, а не постоянное окно с циклом ввода. `EditorScriptWorkspaceSnapshot` описывает состояние базового editor UI, а отдельный smoke path `EditorScriptLanguageServicesSmoke` проверяет completion, hover, signature help и diagnostics overlay. Постоянный desktop event loop, то есть цикл обработки окна, pointer/keyboard input и repaint, добавляется следующими задачами поверх этих snapshot-контрактов.

## Предварительный UI-контракт

Перед реализацией workspace закрыт минимальный публичный UI-контракт для editor text surface: `TextEdit`, `CodeEdit`, `SyntaxHighlighter`, `CodeHighlighter`, `PopupMenu`, `TabContainer`, `Tree`, `ItemList`, `SplitContainer`, `ScrollBar`, `LineEdit`, `Label`, `Button`, IME, clipboard, selection, caret navigation, Unicode, monospace font, large documents, scrolling, gutter drawing и mouse hit testing.

Snapshot сохраняет этот список как `PrerequisiteManifest`. Smoke-проверка требует `PrerequisiteManifestClosed=True`, чтобы Script workspace не обходил UI public API gate внутренней одноразовой реализацией.

## Snapshot model

Snapshot рабочего пространства содержит:

- workspace switcher `2D`, `Script`, `Game`, `Tasks` и выбранный workspace `Script`;
- операции с файлами: create, rename и delete script file;
- open tabs с active/dirty state;
- active `CodeDocument` с `DocumentId`, path, text, current revision, persisted revision, dirty state, diagnostics и semantic version;
- editor surface: line numbers, syntax token categories, auto indentation, spaces policy, bracket/quote matching, code folding marker, current line, caret и selection;
- search/replace, project search result count и go-to-line;
- команды clipboard, undo/redo, save file и save all;
- `TextBufferEditSession`, то есть внутреннюю сессию редактирования текста, которая пишет изменения в `CodeDocument` и не создаёт отдельные project operations на каждый typed character;
- grouped undo id для действий AI/refactoring, которые должны откатываться одной операцией на уровне workspace;
- agent save conflict diagnostic и external file change result;
- `WorkspaceJobInputIdentity` для build/test/run jobs, которые используют тот же snapshot identity, что и остальной project system.

Language services UI поверх `Script` workspace показывает:

- completion popup рядом с caret/current line;
- selected completion item и keyboard focus state;
- hover/Quick Info panel рядом с symbol;
- signature help с active parameter;
- live diagnostics panel с code, severity и source location;
- правую секцию `Language Services` с document identity, semantic state, stale response marker и configuration hash.

Фактическая C# semantic model описана отдельно: [C# language services в Script workspace](../scripting/editor-language-services.md).

## Visual harness

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-workspace-smoke .temp\script-workspace
```

Создаёт:

- `.temp/script-workspace/script-workspace.state.json`;
- `.temp/script-workspace/visual/script-workspace.png`;
- `.temp/script-workspace/visual/script-workspace.analysis.json`.

PNG является deterministic screenshot artifact для обязательной визуальной проверки UI-задач. JSON analysis содержит bounds вкладок, editor surface, search/replace, правого `Code Document`, conflict marker, snapshot identity, счётчик clickable controls, результат проверки text overflow и список forbidden UI matches.

В текущей проверке агент открыл `script-workspace.png` и подтвердил:

- `Script` выбран в workspace switcher;
- слева видны `FileSystem` и `Symbols`;
- по центру видны вкладки, search/replace controls, gutter со строками, code editor, caret и selection;
- справа видны `Inspector`, `Code Document`, `Dirty Revision`, `Diagnostic`, `Conflict` и `Agent Workspace`;
- снизу видна панель `Diagnostics`;
- текст не выходит за границы контейнеров и не перекрывает соседние элементы;
- `3D`, `AssetLib`, GDScript UI, `.gd` и disabled 3D controls визуально отсутствуют.

## Проверки

Focused test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkspaceTests"
```

Smoke-команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-workspace-smoke .temp\script-workspace
```

Language services smoke-команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-language-services-smoke .temp\script-language-services
```

Документационный verifier после изменения справки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1
```
