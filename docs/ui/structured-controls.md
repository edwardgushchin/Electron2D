# Спецификация: структурные UI controls

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Статус
- Задача: `T-0071`
- Статус: утверждено для реализации
- Последнее обновление: `2026-06-22T05:05:00+03:00`

## Контекст
Electron2D 0.1-preview должен иметь runtime UI API, достаточный для игр, отладочных панелей и будущего редактора. Эта задача добавляет структурные controls для списков, деревьев, вкладок и контекстных меню, но не начинает проект `Electron2D.Editor`.

Редактор остаётся заблокированным до закрытия `T-0129`: все UI-related строки в GitHub Wiki `API-Compatibility.md` должны быть реализованы, протестированы, задокументированы и отмечены как `Supported`, а не `Partial`.

## Область реализации
Публичный runtime API этой задачи:

- `ItemList`
- `PopupMenu`
- `TabContainer`
- `Tree`
- `TreeItem`

`PropertyGrid`, `Dock` и `CodeDiagnosticsView` являются будущими внутренними виджетами редактора. Они не должны появляться как публичные типы сборки `Electron2D`.

## Публичное поведение

### Общие требования
- Все controls используют существующие `Control`, `Container`, input dispatch, focus, signals, theme lookup и canvas drawing.
- Новые public types и все public members имеют полную XML documentation по правилам проекта.
- Каждый новый source file содержит MIT-заголовок проекта.
- Реализация не добавляет dependency на WPF, WinForms, Avalonia или другой desktop UI framework.
- Реализация не добавляет compatibility layer и не возвращает legacy/Unity-like API.

### ItemList
- `ItemList` наследуется от `Control`.
- Конструктор включает focus и регистрирует сигналы `item_selected`, `multi_selected`, `item_activated`.
- `ItemList` хранит элементы с text, optional icon, disabled/selectable flags и selection state.
- Поддерживаются свойства `ItemCount`, `SelectMode`, `AllowReselect`, `AllowRmbSelect`, `MaxColumns`, `FixedColumnWidth`, `FixedIconSize`.
- Поддерживаются методы `AddItem`, `AddIconItem`, `SetItemText`, `GetItemText`, `SetItemIcon`, `GetItemIcon`, `SetItemDisabled`, `IsItemDisabled`, `SetItemSelectable`, `IsItemSelectable`, `Select`, `Deselect`, `DeselectAll`, `IsSelected`, `GetSelectedItems`, `GetItemAtPosition`, `Clear`.
- В режиме single selection выбор нового элемента снимает предыдущий выбор.
- В режиме multi selection можно выбрать несколько selectable элементов.
- Disabled или non-selectable элемент не выбирается через user input.
- Mouse/touch press выбирает элемент по строке и отправляет сигнал выбора; двойной mouse press или activation key отправляет `item_activated`.

### Tree и TreeItem
- `Tree` наследуется от `Control`.
- `TreeItem` наследуется от `Object` и создаётся через `Tree.CreateItem`.
- `Tree` хранит один root item, настройки columns, hide root, column titles, selection mode и текущий selected item/column.
- Конструктор `Tree` включает focus, `ClipContents` и регистрирует сигналы `item_selected`, `multi_selected`, `item_activated`, `item_collapsed`.
- `TreeItem` хранит text/icon/selectable/selected/collapsed state по column.
- Поддерживаются методы навигации `GetParent`, `GetTree`, `GetChild`, `GetChildCount`, `GetNext`, `GetPrevious`, `GetNextVisible`.
- `TreeItem.Select` выбирает item в owning tree; `Deselect` и `Tree.DeselectAll` снимают selection.
- `Tree.Clear` удаляет все items и сбрасывает selection.
- При `HideRoot == true` root не занимает строку hit-test/drawing.
- Collapsed item скрывает descendants в visible traversal.

### PopupMenu
- `PopupMenu` наследуется от `Control`.
- Конструктор регистрирует сигналы `id_pressed` и `index_pressed`, скрывает menu по умолчанию и включает focus.
- Menu хранит items с label, id, optional icon, checked/disabled/separator flags.
- Поддерживаются методы `AddItem`, `AddIconItem`, `AddCheckItem`, `AddIconCheckItem`, `AddSeparator`, `SetItemText`, `GetItemText`, `SetItemIcon`, `GetItemIcon`, `SetItemChecked`, `IsItemChecked`, `SetItemDisabled`, `IsItemDisabled`, `SetItemId`, `GetItemId`, `IsItemSeparator`, `GetItemCount`, `Clear`, `Popup`.
- `Popup(Rect2)` показывает menu, задаёт position/size и берёт focus.
- Click/activation key на enabled item отправляет `index_pressed(index)` и `id_pressed(id)`.
- Disabled items и separators не активируются.
- После успешной activation menu скрывается.

### TabContainer
- `TabContainer` наследуется от `Container`.
- Каждый direct child `Control` является вкладкой. Non-control children игнорируются.
- Конструктор регистрирует сигнал `tab_changed`.
- Поддерживаются свойства `CurrentTab`, `TabsVisible`, `AllTabsInFront`, `DeselectEnabled`, `TabAlignment`, `TabsPosition`, `UseHiddenTabsForMinSize`.
- Поддерживаются методы `GetTabCount`, `GetTabControl`, `GetCurrentTabControl`, `GetPreviousTab`, `SetTabTitle`, `GetTabTitle`, `SetTabIcon`, `GetTabIcon`, `SetTabDisabled`, `IsTabDisabled`, `SetTabHidden`, `IsTabHidden`, `GetTabIdxFromControl`, `GetTabIdxAtPoint`, `SelectNextAvailable`, `SelectPreviousAvailable`.
- Изменение `CurrentTab` показывает выбранный child control и скрывает остальные visible tab pages.
- Disabled или hidden вкладка не выбирается через user input.
- Mouse press по tab header меняет текущую вкладку и отправляет `tab_changed(index)`.

## Приемочные критерии
- Добавлены unit-тесты публичного API structured controls.
- Добавлены integration-тесты selection, activation, tab switching и сигналов через `SceneTree.DispatchInput`.
- `PropertyGrid`, `Dock`, `CodeDiagnosticsView` отсутствуют в exported public types сборки `Electron2D`.
- `dotnet test src/Electron2D.sln` проходит.
- `dotnet build src/Electron2D.sln -c Release` проходит.
- `tools/Verify-PublicApiXmlDocs.ps1 -FailOnIssues` проходит.
- `tools/Update-ApiWiki.ps1` генерирует GitHub Wiki в `.github/wiki`.
- В `.github/wiki/API-Compatibility.md` строки для `ItemList`, `PopupMenu`, `TabContainer`, `Tree`, `TreeItem` имеют статус `Supported`.

## Фактическое состояние, ограничения и проверки

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
