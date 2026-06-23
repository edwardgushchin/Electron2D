# Reference platformer 0.1.0 Preview

Статус: целевая спецификация для `T-0094`.

## Цель

`examples/reference-platformer/` должен быть законченной приёмочной игрой `0.1.0 Preview`, а не standalone demo, fixture или набором runtime files. Проект должен открываться и проверяться тем же форматом, который использует `Electron2D.Editor`: `project.e2d.json`, `.csproj`, main scene, project settings, Input Map, export presets, ресурсы, C# scripts и `.electron2d/tasks/**`.

Игра проверяет небольшой 2D platformer loop: игрок двигается по tilemap-уровню, прыгает на one-way platform, камера следует за игроком, анимация меняет кадры, звук запускается на игровых событиях, gamepad/keyboard/touch input приводят к одному action flow, pause menu останавливает gameplay, а progress save фиксирует checkpoint и собранные предметы.

## Project Layout

Обязательные файлы:

- `examples/reference-platformer/Electron2D.ReferencePlatformer.csproj`;
- `examples/reference-platformer/Program.cs`;
- `examples/reference-platformer/Scripts/PlatformerGame.cs`;
- `examples/reference-platformer/project.e2d.json`;
- `examples/reference-platformer/electron2d.lock.json`;
- `examples/reference-platformer/global.json`;
- `examples/reference-platformer/export_presets.e2export.json`;
- `examples/reference-platformer/scenes/main.scene.json`;
- `examples/reference-platformer/resources/reference-platformer.manifest.json`;
- `examples/reference-platformer/.electron2d/tasks/board.e2tasks`;
- `examples/reference-platformer/.electron2d/tasks/reference-platformer-acceptance.e2task`.

`project.e2d.json` должен:

- использовать `format = "Electron2D.ProjectSettings"` и `formatVersion = 1`;
- иметь `name = "Electron2D.ReferencePlatformer"`;
- ссылаться на `scenes/main.scene.json` как `mainScene`;
- задавать actions `move_left`, `move_right`, `jump`, `pause` с keyboard и gamepad bindings;
- задавать landscape display settings.

`export_presets.e2export.json` должен содержать presets для `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64` и `WebAssemblyBrowser`. Эти presets являются проверяемыми input-файлами обычного export workflow; реальные platform smoke, soak и production package checks закрываются задачами `T-0096`, `T-0093` и release gate.

## Gameplay Contract

C# script должен создавать runtime scene через публичный API Electron2D и явно использовать:

- `TileSet`, `TileSetAtlasSource` и `TileMapLayer` с collision polygons;
- at least one one-way tile collision polygon;
- `CharacterBody2D.MoveAndSlide()` для player movement;
- `Camera2D.MakeCurrent()` и player-follow state;
- `SpriteFrames` и `AnimatedSprite2D` для idle/walk animation;
- `AudioStreamPlayer` или `AudioStreamPlayer2D` для jump/checkpoint feedback;
- `Input.GetVector(...)`, `Input.IsActionJustPressed(...)`, gamepad bindings из Input Map и touch events через `InputEventScreenTouch`/`InputEventScreenDrag`;
- pause state и отдельный pause menu node;
- deterministic save progress file under user data или verifier-supplied output path.

Проект может использовать curated assets из `data/assets/reference-games/`, но не должен ссылаться на сетевые download steps, placeholder files, ignored local workflow files, `TASKS.md`, `dev-diary/` или `completed-tasks/`.

## Verification

Нужен локальный verifier `tools/Verify-ReferencePlatformer.ps1`, который:

- проверяет обязательные файлы и отсутствие запрещённых workflow files внутри примера;
- читает `project.e2d.json` через project settings loader;
- читает `export_presets.e2export.json` через export preset store;
- проверяет `.electron2d/tasks/**` как first-class Editor metadata;
- проверяет, что `resources/reference-platformer.manifest.json` ссылается только на существующие локальные assets;
- собирает `Electron2D.ReferencePlatformer.csproj`;
- запускает headless verification mode и проверяет output-маркеры gameplay subsystems;
- запускает `e2d validate --project examples/reference-platformer --format json` как preview validation route;
- проверяет, что package-planning/verifier steps не копируют `.electron2d/tasks/**` в runtime asset list.

Focused automated test должен падать до появления проекта/verifier-а и проходить после реализации. Документация реализации должна описать текущие ограничения: настоящий Tier 1 smoke/soak, performance gate и сборка обеих reference games на пяти платформах остаются в `T-0093`, `T-0096` и `T-0102`.
