# Ассеты reference games 0.1.0 Preview

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

`T-0136` закрывает блокер по отсутствующим реальным ассетам для приёмочного Platformer-проекта `0.1.0 Preview`.

Эта спецификация не закрывает сам reference game. `T-0094` должен создать полноценный валидный проект `Electron2D.Editor`: с `.e2d`, `.csproj`, main scene, project settings, Input Map, export presets, импортируемыми resources, scripts и `.electron2d/tasks/**` metadata. Успешная проверка ассетов означает только готовность исходных файлов ресурсов; она не заменяет Editor project validation, открытие проекта в Editor, save/reopen cycle, run/debug/export workflow и platform packaging checks.

## Цель

Platformer должен получать локально поставляемые файлы ассетов с понятной лицензией, проверяемым manifest и без сетевого скачивания во время build, test или release archive packaging. Отдельные ассеты второго reference project больше не входят в активный preview/release contract.

Набор ассетов не должен состоять из placeholder-изображений, синтетических звуков, test-only resources или временных файлов из исходных архивов.

## Контракт набора

Маркер проверки: `reference-assets:manifest`.

Поставляемый набор должен находиться в `data/assets/reference-games/` и включать:

- `manifest.json` - machine-readable список файлов, sources, roles, games, sizes и SHA-256.
- `LICENSES.md` - человекочитаемые license metadata по каждому внешнему source.
- `README.md` - правила использования и локальная команда проверки.
- ассеты для `platformer`: tileset/characters/background sprites, отдельные sprites, source level files, OGG-звуки движения/шага/взаимодействия, общий TTF font и UI sprites.

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
- проверять JSON/XML parse для source tilemap files;
- запрещать `.url`, `.sfk`, cache/temp/helper files;
- проверять, что каждое обязательное role присутствует для `platformer`;
- падать, если в asset directory есть файл, не описанный в manifest, кроме `manifest.json`, `README.md` и `LICENSES.md`.

## Acceptance

- `T-0094` можно разблокировать только после green verifier и фактического появления required roles для `platformer`.
- `T-0093` и `T-0102` остаются blocked, пока reference game не реализован поверх этих ассетов.
- Любое изменение asset file требует обновить `bytes`, `sha256`, роли и license metadata в manifest.

## Фактическое состояние, ограничения и проверки

Текущий baseline `0.1.0 Preview` поставляет curated subset ассетов в `data/assets/reference-games/`.

Маркер проверки: `reference-assets:manifest`.

## Что находится в репозитории

- `data/assets/reference-games/manifest.json` - authoritative manifest для verifier-а и будущего release archive.
- `data/assets/reference-games/LICENSES.md` - source metadata, author, license и upstream archive hashes.
- `data/assets/reference-games/README.md` - локальные правила использования набора.
- `data/assets/reference-games/platformer/` - графика, source tilemap files и OGG-звуки для Platformer.
- `data/assets/reference-games/shared/` - общий TTF font и UI sprites.

## Источники

Внешние visual/audio/font assets взяты из CC0-наборов Kenney:

- `kenney-pixel-platformer` - platformer tilesheets, sprites и source tilemap files.
- `kenney-ui-pack` - UI sprites и `kenney-future.ttf`.
- `kenney-rpg-sounds` - OGG sound effects.

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
- parse JSON/XML для tilemap source files;
- отсутствие `.url`, `.sfk`, cache/temp/helper files;
- required roles для `platformer`.

## Текущий статус

`T-0136` закрыл asset blocker для `T-0094`: Platformer получил локальные визуальные, звуковые и font/UI resources.

`T-0094` реализовал `examples/platformer/` как валидный проект `Electron2D.Editor` поверх этих ассетов.

`T-0211` удаляет второй reference project и его отдельный asset subset из активного preview/release contract. `T-0093` и `T-0102` остаются отдельными задачами: smoke/soak checks и performance gate должны проверяться поверх настоящего reference project, а не временных fixtures.
