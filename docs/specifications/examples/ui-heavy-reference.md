# UI-heavy reference game 0.1.0 Preview

Статус: целевая спецификация для `T-0095`.

## Цель

`examples/ui-heavy-reference/` должен быть второй законченной приёмочной игрой `0.1.0 Preview`, а не standalone demo, fixture или набором runtime files. Проект должен открываться и проверяться тем же форматом, который использует `Electron2D.Editor`: `project.e2d.json`, `.csproj`, main scene, project settings, Input Map, export presets, ресурсы, C# scripts и `.electron2d/tasks/**`.

Игра должна быть небольшой карточной головоломкой с полным UI-loop: главное меню, игровое поле с карточками, список целей, переключение локали, touch/mouse/keyboard actions, текст, сохранение прогресса, переход между menu/game/result scenes и renderer profile `Compatibility` для Android preset.

## Project Layout

Обязательные файлы:

- `examples/ui-heavy-reference/Electron2D.UiHeavyReference.csproj`;
- `examples/ui-heavy-reference/Program.cs`;
- `examples/ui-heavy-reference/Scripts/CardPuzzleGame.cs`;
- `examples/ui-heavy-reference/project.e2d.json`;
- `examples/ui-heavy-reference/electron2d.lock.json`;
- `examples/ui-heavy-reference/global.json`;
- `examples/ui-heavy-reference/export_presets.e2export.json`;
- `examples/ui-heavy-reference/scenes/menu.scene.json`;
- `examples/ui-heavy-reference/scenes/game.scene.json`;
- `examples/ui-heavy-reference/scenes/result.scene.json`;
- `examples/ui-heavy-reference/resources/ui-heavy-reference.manifest.json`;
- `examples/ui-heavy-reference/.electron2d/tasks/board.e2tasks`;
- `examples/ui-heavy-reference/.electron2d/tasks/ui-heavy-acceptance.e2task`.

`project.e2d.json` должен:

- использовать `format = "Electron2D.ProjectSettings"` и `formatVersion = 1`;
- иметь `name = "Electron2D.UiHeavyReference"`;
- ссылаться на `scenes/menu.scene.json` как `mainScene`;
- задавать actions `accept`, `cancel`, `next_card`, `previous_card`, `switch_locale` с keyboard, pointer/touch-compatible и gamepad bindings там, где формат Input Map это поддерживает;
- задавать portrait-capable display settings с безопасной областью для mobile runtime.

`export_presets.e2export.json` должен содержать presets для `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64` и `WebAssemblyBrowser`. Android preset обязан использовать renderer profile `Compatibility`, потому что эта игра проверяет мобильный fallback-путь. Эти presets являются проверяемыми input-файлами обычного export workflow; реальные platform smoke, soak и production package checks закрываются задачами `T-0096`, `T-0093` и release gate.

## Gameplay Contract

C# script должен создавать runtime UI scene через публичный API Electron2D и явно использовать:

- `Control` root с container-based layout;
- минимум два container-типа, включая линейный container и grid/table-style container;
- `Panel`, `Label`, `Button`, `TextureButton`, `CheckBox`, `Slider`, `ProgressBar`, `TextureRect` или `NinePatchRect`;
- structured UI control для списка или вкладок, если соответствующий публичный API доступен; иначе script должен явно описать fallback через обычные `Button`/`Label` элементы и verifier должен проверять этот fallback;
- локализацию через `Translation`/`TranslationServer.Tr(...)` или public equivalent `Tr(...)`, переключение `en`/`ru` и fallback key;
- разные target resolutions: desktop landscape, mobile portrait и tablet layout checkpoints;
- touch input через `InputEventScreenTouch` или pointer-equivalent UI action;
- текстовые элементы с локализованными строками, которые не зависят от сетевых ресурсов;
- progress save под user data или verifier-supplied output path;
- scene transitions `menu -> game -> result -> menu`;
- Android compatibility renderer marker, полученный из export preset или project settings.

Проект может использовать curated assets из `data/assets/reference-games/`, но не должен ссылаться на сетевые download steps, placeholder files, ignored local workflow files, `TASKS.md`, `dev-diary/` или `completed-tasks/`.

## Verification

Нужен локальный verifier `tools/Verify-UiHeavyReference.ps1`, который:

- проверяет обязательные файлы и отсутствие запрещённых workflow files внутри примера;
- читает `project.e2d.json` через project settings loader;
- читает `export_presets.e2export.json` через export preset store;
- проверяет, что Android preset использует renderer profile `Compatibility`;
- проверяет `.electron2d/tasks/**` как first-class Editor metadata;
- проверяет, что `resources/ui-heavy-reference.manifest.json` ссылается только на существующие локальные assets;
- проверяет локализацию `en`/`ru`, game data и базовые сигнатуры PNG/OGG/TTF;
- собирает `Electron2D.UiHeavyReference.csproj`;
- запускает headless verification mode и проверяет output-маркеры UI/gameplay subsystems;
- запускает `e2d validate --project examples/ui-heavy-reference --format json` как preview validation route;
- проверяет, что package-planning/verifier steps не копируют `.electron2d/tasks/**` в WebAssembly package output.

Focused automated test должен падать до появления проекта/verifier-а и проходить после реализации. Документация реализации должна описать текущие ограничения: настоящий Tier 1 smoke/soak, performance gate и сборка обеих reference games на пяти платформах остаются в `T-0093`, `T-0096` и `T-0102`.
