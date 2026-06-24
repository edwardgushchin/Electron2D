# Reference Game Assets

Этот каталог содержит локальный, проверяемый набор ассетов для reference game `0.1.0 Preview`.

Набор не является временной заглушкой: файлы выбраны из опубликованных CC0-наборов Kenney и дополнены небольшими metadata/resources проекта Electron2D. Build, test и release archive не должны скачивать эти ассеты из сети.

## Состав

- `platformer/graphics/` - tilesheets, персонаж, terrain sprite и background tiles для Platformer.
- `platformer/levels/` - source tilemap/tileset files для стартового уровня.
- `platformer/audio/` - OGG-звуки движения, шага и взаимодействия.
- `shared/fonts/` - TTF-шрифт для reference project.
- `shared/ui/` - UI sprites, пригодные для pause menu, HUD и basic controls.

## Проверка

Команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferenceGameAssets.ps1
```

Verifier читает `data/assets/reference-games/manifest.json`, проверяет наличие всех файлов, SHA-256, размер, разрешённые расширения, локальность путей, наличие license metadata и обязательные роли для `platformer`.

## Правила использования

- Не добавлять `.url`, `.sfk`, cache files или файлы, требующие сетевого скачивания во время build.
- Каждый новый файл должен быть добавлен в `manifest.json` с `source`, `roles`, `games`, `bytes` и `sha256`.
- Каждый внешний источник должен иметь автора, лицензию и source URL в `manifest.json` и `LICENSES.md`.
- Если reference game требует нового публичного API или нового ассета, соответствующая задача остаётся blocked до реализации API и добавления ассета в этот manifest.
