# Ассеты reference games 0.1.0 Preview

`T-0136` закрывает блокер по отсутствующим реальным ассетам для приёмочных игр `0.1.0 Preview`.

Эта спецификация не закрывает сами reference games. `T-0094` и `T-0095` должны создать полноценные валидные проекты `Electron2D.Editor`: с `project.e2d.json`, `.csproj`, main scene, project settings, Input Map, export presets, импортируемыми resources, scripts и `.electron2d/tasks/**` metadata. Успешная проверка ассетов означает только готовность исходных файлов ресурсов; она не заменяет Editor project validation, открытие проекта в Editor, save/reopen cycle, run/debug/export workflow и platform packaging checks.

## Цель

Reference platformer и UI-heavy reference game должны получать локально поставляемые файлы ассетов с понятной лицензией, проверяемым manifest и без сетевого скачивания во время build, test или release archive packaging.

Набор ассетов не должен состоять из placeholder-изображений, синтетических звуков, test-only resources или временных файлов из исходных архивов.

## Контракт набора

Маркер проверки: `reference-assets:manifest`.

Поставляемый набор должен находиться в `data/assets/reference-games/` и включать:

- `manifest.json` - machine-readable список файлов, sources, roles, games, sizes и SHA-256.
- `LICENSES.md` - человекочитаемые license metadata по каждому внешнему source.
- `README.md` - правила использования и локальная команда проверки.
- ассеты для `reference-platformer`: tileset/characters/background sprites, отдельные sprites, source level files, OGG-звуки движения/шага/взаимодействия, общий TTF font и UI sprites.
- ассеты для `ui-heavy-reference`: UI sprites, OGG-звуки взаимодействия/награды, общий TTF font, localization JSON и game data JSON.

## Источники и лицензии

Внешние источники должны иметь:

- stable `source id`;
- author;
- license id;
- license URL;
- source URL;
- hash upstream archive, если набор подготовлен из архива.

Для `0.1.0 Preview` допускаются только источники, которые можно положить в public repository и release archive без runtime/download условий. Первый baseline использует CC0-наборы Kenney и project-owned metadata/resources под MIT.

## Проверяемость

Verifier `tools\Verify-ReferenceGameAssets.ps1` должен:

- читать `data/assets/reference-games/manifest.json`;
- проверять `networkRequiredDuringBuild = false`;
- запрещать remote asset paths;
- проверять, что все shipped files находятся внутри `data/assets/reference-games/`;
- проверять size и SHA-256 каждого asset file;
- проверять базовые сигнатуры PNG, OGG и TTF;
- проверять JSON/XML parse для localization, game data и source tilemap files;
- запрещать `.url`, `.sfk`, cache/temp/helper files;
- проверять, что каждое обязательное role присутствует для `reference-platformer` и `ui-heavy-reference`;
- падать, если в asset directory есть файл, не описанный в manifest, кроме `manifest.json`, `README.md` и `LICENSES.md`.

## Acceptance

- `T-0094` и `T-0095` можно разблокировать только после green verifier и фактического появления required roles для обеих игр.
- `T-0093` и `T-0102` остаются blocked, пока сами reference games не реализованы поверх этих ассетов.
- Любое изменение asset file требует обновить `bytes`, `sha256`, роли и license metadata в manifest.
