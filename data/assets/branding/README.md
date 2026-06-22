# Брендовые ассеты Electron2D

Этот каталог содержит полный бренд-пак Electron2D, импортированный из `electron2d_complete_brand_pack_v2.zip`.

## Состав

- `logo/` - основные горизонтальные логотипы в SVG и PNG.
- `readme/` - версии логотипа для README на светлой и тёмной теме.
- `icon/` - `.ico`, исходный SVG и PNG-иконки от 16 до 1024 px.
- `social/` - GitHub social preview в светлом и тёмном вариантах.
- `mark/` - отдельный знак Electron2D.
- `docs/` - краткий brand guide и исходный README snippet из бренд-пака.
- `preview/` - preview-лист набора.

## Использование в репозитории

- `README.md` использует SVG из `readme/`.
- NuGet metadata runtime package использует `icon/electron2d_windows_icon_128.png` как `PackageIcon`.
- `Electron2D.Editor` использует `icon/electron2d.ico` как иконку executable.

Исходный архив проверен локально перед импортом:

```text
SHA256: EDEE673C1E8936D0C49A12D37BC4A0352BE22C40776F23E91E4E40173CAF049C
```
