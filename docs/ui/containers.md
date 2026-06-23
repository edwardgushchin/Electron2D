# UI containers

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0068`.
Дата: 2026-06-22.

## Цель

Electron2D `0.1.0 Preview` должен иметь базовый набор UI-контейнеров, достаточный для вложенного layout: общий `Container`, линейные `HBoxContainer`/`VBoxContainer`, табличный `GridContainer`, контейнер внешних отступов `MarginContainer`, центрирующий `CenterContainer` и прокручиваемый `ScrollContainer`.

Контейнеры работают только с прямыми дочерними `Control`. Остальные дочерние `Node` остаются в дереве, но не участвуют в layout. Готовые widgets, scrollbar nodes, tooltips, theme resources и editor-only panels остаются отдельными задачами.

## Общий контракт

- `Container` наследуется от `Control`.
- `Container.FitChildInRect(Control child, Rect2 rect)` размещает прямой дочерний `Control` в локальном прямоугольнике контейнера.
- `Container.QueueSort()` помечает контейнер как требующий пересчёта layout; во время runtime frame контейнер пересчитывает дочерние controls.
- Контейнеры не меняют parent/owner ребёнка и не добавляют compatibility API.
- Контейнеры учитывают только `Control.GetCombinedMinimumSize()` и публичные size flags дочернего control.

## Size flags и theme constants

- `Control.SizeFlagsHorizontal` и `Control.SizeFlagsVertical` задают, как direct parent container распределяет доступное место.
- `SizeFlags.ShrinkBegin` оставляет control в начале выделенного прямоугольника и не растягивает его.
- `SizeFlags.Fill` разрешает растянуть control внутри выделенного прямоугольника.
- `SizeFlags.Expand` участвует в распределении дополнительного места по основной оси контейнера.
- `SizeFlags.ExpandFill` объединяет `Expand` и `Fill`.
- `SizeFlags.ShrinkCenter` и `SizeFlags.ShrinkEnd` размещают control по центру или в конце выделенного прямоугольника, когда `Fill` не задан.
- `SizeFlagsStretchRatio` задаёт вес распределения дополнительного места между expand-children и должен быть положительным finite value.
- `AddThemeConstantOverride()` и `GetThemeConstant()` дают минимальный публичный путь для `separation`, `h_separation`, `v_separation` и `margin_*` значений без полноценной theme system.

## Линейные контейнеры

- `BoxContainer` раскладывает direct `Control` children по одной оси.
- `HBoxContainer` использует горизонтальную ось.
- `VBoxContainer` использует вертикальную ось.
- `BoxContainer.Alignment` двигает всю группу в начало, центр или конец, если свободное место не распределено через `Expand`.
- `BoxContainer.AddSpacer(bool begin)` добавляет растягиваемый `Control` в начало или конец списка детей.
- Theme constant `separation` задаёт расстояние между детьми; default value равен `4`.

## GridContainer

- `GridContainer.Columns` задаёт количество колонок и должен быть больше нуля.
- Дети раскладываются row-major order.
- Ширина каждой колонки равна максимальной minimum width среди детей этой колонки.
- Высота каждой строки равна максимальной minimum height среди детей этой строки.
- Theme constants `h_separation` и `v_separation` задают расстояние между колонками и строками; default value равен `4`.

## MarginContainer

- `MarginContainer` размещает direct `Control` children внутри прямоугольника, уменьшенного на theme constants `margin_left`, `margin_top`, `margin_right`, `margin_bottom`.
- Minimum size контейнера равен максимуму minimum size детей плюс соответствующие отступы.

## CenterContainer

- `CenterContainer` размещает direct `Control` children по центру собственного `Size`.
- `UseTopLeft = true` сохраняет размер ребёнка, но делает top-left corner ребёнка якорем центрирования.

## ScrollContainer

- `ScrollContainer` наследуется от `Container` и по умолчанию включает `ClipContents`.
- `ScrollHorizontal` и `ScrollVertical` задают offset контента и clamp-ятся к доступному диапазону.
- `HorizontalScrollMode` и `VerticalScrollMode` фиксируют preview-level политику: `Disabled` запрещает смещение по оси, остальные значения оставляют ось доступной для программного scroll.
- `EnsureControlVisible(Control control)` принимает descendant control и сдвигает scroll offset так, чтобы его rectangle оказался внутри viewport area.
- Public scrollbar nodes в этой задаче не добавляются, потому что они относятся к задаче готовых controls.

## Критерии приёмки

- Есть tests для `HBoxContainer` и `VBoxContainer`, включая resize, minimum size, separation и expand/fill flags.
- Есть tests для `GridContainer`, `MarginContainer` и `CenterContainer`.
- Есть tests для `ScrollContainer`, clamp scroll offsets и `EnsureControlVisible()`.
- Публичный API имеет XML documentation и отражён в GitHub Wiki.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0068`.
Дата: 2026-06-22.

## Текущее поведение

Контейнеры Electron2D строятся поверх `Control`. Они пересчитывают layout direct child controls во время runtime frame и игнорируют дочерние nodes других типов. Пересчёт не меняет owner, parent и порядок дочерних nodes, кроме явного вызова `BoxContainer.AddSpacer()`, который добавляет новый `Control`.

`Container.FitChildInRect()` является общей операцией размещения: она сбрасывает anchors ребёнка к локальному rectangle mode и задаёт `Position`/`Size` через offsets. Это делает результат layout предсказуемым даже для controls, у которых раньше были anchors.

## Size flags

`Control` теперь хранит `SizeFlagsHorizontal`, `SizeFlagsVertical` и `SizeFlagsStretchRatio`. Контейнеры используют эти значения при распределении свободного места:

- `ShrinkBegin` оставляет control в начале выделенного слота;
- `Fill` растягивает control внутри выделенного слота;
- `Expand` получает долю свободного места;
- `ExpandFill` объединяет оба поведения;
- `ShrinkCenter` центрирует control внутри слота, если fill не используется;
- `ShrinkEnd` прижимает control к концу слота, если fill не используется.

`SizeFlagsStretchRatio` должен быть finite и больше нуля.

## Theme constants

`AddThemeConstantOverride()` и `GetThemeConstant()` поддерживают числовые theme constants, которые нужны контейнерам:

- `separation` для `BoxContainer`;
- `h_separation` и `v_separation` для `GridContainer`;
- `margin_left`, `margin_top`, `margin_right`, `margin_bottom` для `MarginContainer`.

Если override отсутствует, контейнеры используют documented default: `4` для separation и `0` для margins.

## Реализованные контейнеры

`BoxContainer` раскладывает children по одной оси. `HBoxContainer` выбирает горизонтальную ось, `VBoxContainer` - вертикальную. `Alignment` влияет только на свободное место, которое не распределено через expand children.

`GridContainer` строит строки и колонки по minimum size детей. `Columns` должен быть больше нуля.

`MarginContainer` уменьшает рабочий прямоугольник на theme margins и размещает всех direct child controls внутри него.

`CenterContainer` размещает children по центру контейнера. При `UseTopLeft = true` top-left corner ребёнка попадает в центр контейнера.

`ScrollContainer` включает `ClipContents` по умолчанию, хранит scroll offsets и размещает content со смещением. Public scrollbar nodes пока не реализованы; scroll управляется свойствами и `EnsureControlVisible()`.

## Проверки

Основная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ContainerLayoutTests" --no-restore -m:1
```

Дополнительные проверки перед закрытием задачи:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~ContainerPublicApiTests|FullyQualifiedName~CleanRuntimeBaselineTests" --no-restore -m:1
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiXmlDocs.ps1 -FailOnIssues
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Update-ApiWiki.ps1 -OutputPath .github\wiki -Check
```
