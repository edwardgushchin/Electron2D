# Документация реализации: структурные UI controls

## Назначение
Структурные UI controls Electron2D предоставляют runtime API для списков, деревьев, вкладок и контекстных меню. Эти controls нужны игровым меню, debug-инструментам и будущему редактору, но сами не являются editor-проектом.

Редактор не должен начинаться, пока gate `T-0129` не подтвердит полный `Supported` статус UI public API в GitHub Wiki.

## Состав
- `ItemList` хранит плоский список selectable items с text, optional icon и signals выбора.
- `Tree` хранит и отображает иерархию `TreeItem`.
- `TreeItem` является объектом данных для строк `Tree`.
- `PopupMenu` хранит всплывающий список команд с id/index activation signals.
- `TabContainer` раскладывает direct child controls как страницы вкладок.

`PropertyGrid`, `Dock` и `CodeDiagnosticsView` не являются публичными runtime types. Эти названия относятся к будущим внутренним виджетам редактора, которые должны строиться поверх публичных runtime controls.

## Input и selection
`ItemList`, `Tree`, `PopupMenu` и `TabContainer` принимают mouse/touch input через существующий GUI dispatch. Focus включён по умолчанию там, где control должен реагировать на keyboard activation.

`ItemList` выбирает элемент по строке. В single selection режиме новый выбор снимает предыдущий. В multi selection режиме элементы могут выбираться независимо. Disabled и non-selectable элементы не выбираются через user input.

`Tree` выбирает видимый item по строке. Если root скрыт, он не участвует в hit-test. Collapsed item скрывает descendants.

`PopupMenu` активирует enabled non-separator item по строке и скрывается после успешной activation.

`TabContainer` переключает вкладку по header. Disabled или hidden вкладки пропускаются при pointer и программном выборе следующей/предыдущей доступной вкладки.

## Сигналы
`ItemList` отправляет:
- `item_selected`
- `multi_selected`
- `item_activated`

`Tree` отправляет:
- `item_selected`
- `multi_selected`
- `item_activated`
- `item_collapsed`

`PopupMenu` отправляет:
- `index_pressed`
- `id_pressed`

`TabContainer` отправляет:
- `tab_changed`

## Layout и drawing
Controls используют существующий canvas drawing baseline. Размер строки и tab header берутся из theme constants с fallback-значениями. Если theme font отсутствует, controls сохраняют state/input behavior и пропускают текстовое рисование.

`TabContainer` наследуется от `Container` и размещает текущий child page ниже области вкладок. Non-control children не участвуют в layout.

## Проверки
Для задачи должны проходить:

```powershell
dotnet test src/Electron2D.sln
dotnet build src/Electron2D.sln -c Release
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiXmlDocs.ps1 -FailOnIssues
powershell -ExecutionPolicy Bypass -File tools\Update-ApiWiki.ps1 -OutputPath .github\wiki -Check
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1 -WikiPath .github\wiki\API-Compatibility.md
```
