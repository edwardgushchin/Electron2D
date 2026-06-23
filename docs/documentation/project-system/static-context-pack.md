# Статический context pack проекта

Обновлено: 2026-06-23.
Связанные спецификации: [Статический context pack проекта](../../specifications/project-system/static-context-pack.md); [`e2d` CLI для headless, CI и active Editor routing](../../specifications/cli/e2d-cli.md).

## Текущее поведение

`e2d context build` создаёт `.electron2d/context/` как компактный snapshot проекта для автономного агента, CI или клиента без постоянного подключения к открытому Editor. Snapshot - это сохранённый на диск снимок выбранных сведений на момент выполнения команды; после изменения settings, scene, resource, script или task documents его нужно пересобрать.

Команда не подключается к active Editor, не открывает `ProjectWorkspace`, не создаёт undo group и не меняет исходные игровые документы. В общем JSON envelope возвращается `command = "context build"`, `route = "none"`, `data.mode = "context.build"`, `data.outputPath = ".electron2d/context"`, список созданных файлов и суммарный размер.

## Output layout

Команда пересоздаёт generated directory `.electron2d/context/` и пишет:

- `context-manifest.json` - время генерации, список файлов, суммарный размер и предупреждение о snapshot.
- `project-summary.json` - project name/version, engine/.NET version, `mainScene`, `rendererProfile`, display settings, physics tick rate, Input Map, custom C# classes и recommended check commands.
- `api-surface.json` - компактная сводка tracked API manifest без исходного кода движка.
- `godot-differences.json` - machine-readable указатель на проверку выбранного API-подмножества через `e2d api compare-godot <type>`.
- `scene-index.json` - список `*.scene.json`, node names/types/groups и scene external resource references.
- `resource-graph.json` - `.e2res` files, resource type/uid и references из scenes к resources.
- `diagnostics.json` - summary context build, категории пропущенных файлов и parse/security diagnostics без значений секретов.
- `conventions.md` - краткие правила для агента: snapshot нужно пересобирать, generated working directories нельзя редактировать вручную, canonical tasks лежат в `.electron2d/tasks/`.

## Исключения и безопасность

Context pack не копирует source text scripts, binary payloads, импортированные cache artifacts, большие logs, workflow Markdown, `.git`, build outputs и секреты. Для custom C# classes сохраняются только type name, base type и project-relative path. Для skipped data сохраняются категории и счётчики, а не содержимое и не secret values.

Текущие automated checks проверяют, что маленький проект получает полный layout, размер context pack остаётся меньше 64 KiB, а output files не содержат secret values, `.git`, local workflow paths, import cache references, huge log names или binary asset names.
