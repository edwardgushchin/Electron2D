# Ассеты reference games

Текущий baseline `0.1.0 Preview` поставляет curated subset ассетов в `data/assets/reference-games/`.

Маркер проверки: `reference-assets:manifest`.

## Что находится в репозитории

- `data/assets/reference-games/manifest.json` - authoritative manifest для verifier-а и будущего release archive.
- `data/assets/reference-games/LICENSES.md` - source metadata, author, license и upstream archive hashes.
- `data/assets/reference-games/README.md` - локальные правила использования набора.
- `data/assets/reference-games/platformer/` - графика, source tilemap files и OGG-звуки для reference platformer.
- `data/assets/reference-games/shared/` - общий TTF font и UI sprites.
- `data/assets/reference-games/ui-heavy/` - UI-графика, OGG-звуки, localization JSON и card-set JSON.

## Источники

Внешние visual/audio/font assets взяты из CC0-наборов Kenney:

- `kenney-pixel-platformer` - platformer tilesheets, sprites и source tilemap files.
- `kenney-ui-pack` - UI sprites и `kenney-future.ttf`.
- `kenney-rpg-sounds` - OGG sound effects.

Project-owned localization и card-set metadata помечены source id `electron2d` и наследуют MIT License проекта.

## Проверка

Локальная команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferenceGameAssets.ps1
```

Эта же проверка выполняется в GitHub Actions после user documentation checks. Она не скачивает файлы из сети и валидирует только содержимое репозитория.

Verifier проверяет:

- manifest schema, release и `networkRequiredDuringBuild = false`;
- license metadata для всех sources;
- наличие и hash каждого файла;
- базовые сигнатуры PNG/OGG/TTF;
- parse JSON/XML для локализации, card-set и tilemap source files;
- отсутствие `.url`, `.sfk`, cache/temp/helper files;
- required roles для `reference-platformer` и `ui-heavy-reference`.

## Текущий статус

`T-0136` закрыл asset blocker для `T-0094` и `T-0095`: оба reference examples получили локальные визуальные, звуковые, font/UI и data/localization resources.

`T-0094` реализовал `examples/reference-platformer/` как валидный проект `Electron2D.Editor` поверх этих ассетов.

`T-0095` реализовал `examples/ui-heavy-reference/` как валидный проект `Electron2D.Editor` поверх `ui-heavy/` и `shared/` ассетов. `T-0093` и `T-0102` остаются отдельными задачами: smoke/soak checks и performance gate должны проверяться поверх настоящих reference projects, а не временных fixtures.
