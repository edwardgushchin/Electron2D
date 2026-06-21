# Control layout core

Статус: документация реализации для `T-0067`.
Дата: 2026-06-22.

## Текущее поведение

`Control` хранит layout через anchors и offsets. Вычисленный `Position` и `Size` остаются доступными для пользовательского кода, но фактическая геометрия берётся из четырёх anchors и четырёх offsets. Для root-level controls база расчёта - `Viewport.Size`; для дочерних controls база расчёта - `Size` родительского `Control`.

`GetRect()` возвращает локальный прямоугольник control. `GetGlobalRect()` добавляет позиции родительских controls и возвращает прямоугольник в координатах root viewport.

## Minimum size

`CustomMinimumSize` и `_GetMinimumSize()` образуют combined minimum size. `SetSize()` и `ResetSize()` не оставляют control меньше этого значения. Если requested size меньше minimum size, `GrowHorizontal` и `GrowVertical` определяют, какая сторона будет двигаться:

- `Begin` сохраняет конец прямоугольника;
- `End` сохраняет начало прямоугольника;
- `Both` распределяет расширение между началом и концом.

## Clipping

`ClipContents` уже влияет на hit-test мыши и touch: дочерние controls не получают GUI input за пределами clipping-прямоугольника родителя. Рендерный scissor будет подключаться в renderer/backend задачах отдельно.

## Focus navigation

`FocusNext`, `FocusPrevious` и directional focus neighbors хранят `NodePath` до целевого control. `FindNextValidFocus()` и `FindPrevValidFocus()` сначала пытаются использовать явный path, затем переходят к fallback-порядку обхода UI-дерева текущего viewport.

В focus navigation участвуют только controls, которые находятся в tree, видимы и имеют `FocusMode` не равный `None`.

## Проверки

Основная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ControlLayoutCoreTests" --no-restore -m:1
```

Дополнительные проверки перед закрытием задачи:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-PublicApiXmlDocs.ps1 -FailOnIssues
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Update-ApiWiki.ps1 -OutputPath .github\wiki -Check
```
