# Reference Game Assets

Этот каталог содержит локальный, проверяемый набор ассетов для reference game `0.1.0 Preview`.

Набор не является временной заглушкой: файлы выбраны из опубликованных CC0-наборов Kenney и дополнены небольшими метаданными и ресурсами проекта Electron2D. Сборка, тесты и выпускной архив не должны скачивать эти ассеты из сети.

## Состав

- `platformer/graphics/` - tilesheets, персонаж, terrain sprite и background tiles для Platformer.
- `platformer/levels/` - source tilemap/tileset files для стартового уровня.
- `platformer/audio/` - OGG-звуки движения, шага и взаимодействия.
- `shared/fonts/` - TTF-шрифт для reference project.
- `shared/ui/` - UI sprites, пригодные для pause menu, HUD и basic controls.

## Проверка

Команда:

```text
dotnet run --project eng/Electron2D.Build -- verify reference-game-assets
```

Проверка читает `data/assets/reference-games/manifest.json`, проверяет наличие всех файлов, SHA-256, размер, разрешённые расширения, локальность путей, наличие сведений о лицензиях и обязательные роли для `platformer`.

## Правила использования

- Не добавлять `.url`, `.sfk`, файлы кэша или файлы, требующие сетевого скачивания во время сборки.
- Каждый новый файл должен быть добавлен в `manifest.json` с `source`, `roles`, `games`, `bytes` и `sha256`.
- Каждый внешний источник должен иметь автора, лицензию и source URL в `manifest.json` и `LICENSES.md`.
- Если reference game требует нового публичного API или нового ассета, соответствующая задача остаётся заблокированной до реализации API и добавления ассета в этот manifest.
