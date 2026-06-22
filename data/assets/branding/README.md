# Брендовые ассеты Electron2D

Этот каталог содержит бренд-пак Electron2D, импортированный из `electron2d_complete_brand_pack_v5.zip` и нормализованный в рабочей копии: прозрачные PNG/SVG-версии логотипа обрезаны по видимой области без лишнего прозрачного поля.

## Состав

- `logo/` - основные горизонтальные логотипы в SVG и PNG.
- `readme/` - версии логотипа для README на светлой и тёмной теме.
- `icon/` - `.ico`, исходный SVG и PNG-иконки от 16 до 1024 px.
- `social/` - плотные прозрачные logo variants в светлом и тёмном вариантах; исходный большой canvas social preview не сохраняется после нормализации.
- `mark/` - отдельный знак Electron2D.
- `docs/` - краткий brand guide и исходный README snippet из бренд-пака.
- `preview/` - preview-лист набора.

## Использование в репозитории

- `README.md` использует SVG из `readme/`.
- NuGet metadata runtime package использует `icon/electron2d_windows_icon_128.png` как `PackageIcon`.
- `Electron2D.Editor` использует `icon/electron2d.ico` как иконку executable.

Исходный архив проверен локально перед импортом:

```text
SHA256: 5C740EDF8DD1214E6DD2C8C87DABC11CB3EAD4B40064F1595A7C9A559DF18E42
```

Нормализация затрагивает `logo/`, `readme/`, `social/` и прозрачный `mark/`. Фиксированные Windows icon assets в `icon/` и preview-лист в `preview/` не обрезаются, потому что их canvas size является частью назначения файла.
