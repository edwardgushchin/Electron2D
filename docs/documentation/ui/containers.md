# UI containers

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
