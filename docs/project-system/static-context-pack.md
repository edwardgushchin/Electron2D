# Статический context pack проекта

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0120`.
Обновлено: 2026-06-23.
Связанные документы: [Agent-native cross-platform 2D game engine workflow Electron2D 0.1](../architecture/agent-native-workflow.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md); [Live ProjectWorkspace](live-project-workspace.md); [ProjectTaskManager, TaskActivity и task storage](project-task-manager.md).

## Назначение

`e2d context build` создаёт компактный статический snapshot проекта для автономного AI-агента, CI или клиента без постоянного подключения к открытому Editor. Snapshot - это сохранённый на диск снимок выбранных сведений о проекте на момент выполнения команды. Он может устареть сразу после изменения scene, resource, settings, script или task documents и не заменяет живые MCP resources активной Editor-сессии.

Команда является read-only по отношению к исходным игровым документам: она не открывает `ProjectWorkspace` на запись, не создаёт undo group, не меняет `.electron2d/tasks/*.e2task`, scene/resource files, scripts, import cache или task activity. Единственные создаваемые файлы находятся в `.electron2d/context/`.

## CLI contract

Команда:

```powershell
e2d context build --project <path> --format json
```

Правила:

- `--project` указывает project root; если флаг отсутствует, используется текущая директория;
- `--format json` возвращает общий CLI envelope с `command = "context build"`, `route = "none"`, `data.mode = "context.build"`, `data.outputPath`, `data.files[]`, `data.totalBytes` и `data.snapshotWarning`;
- `--format text` выводит короткую человеческую сводку и пишет те же файлы;
- `jsonl`, `sarif` и `markdown` не поддерживаются для `context build`;
- `route = "none"` означает, что команда не подключается к active Editor и не создаёт headless `ProjectWorkspace`;
- failures используют `E2D-CLI-0001` для неизвестного subcommand и `E2D-CLI-0002` для неверных аргументов, невалидного project root или ошибок чтения безопасных project files.

## Output layout

Команда полностью пересоздаёт `.electron2d/context/` и записывает:

```text
.electron2d/context/
├── context-manifest.json
├── project-summary.json
├── api-surface.json
├── godot-differences.json
├── scene-index.json
├── resource-graph.json
├── diagnostics.json
└── conventions.md
```

`context-manifest.json` содержит `schemaVersion`, `generatedAtUtc`, `projectRoot`, список файлов context pack, суммарный размер и предупреждение о том, что snapshot может устареть относительно активной Editor-сессии.

`project-summary.json` содержит:

- project name, version, engine version, .NET version;
- `mainScene`, `rendererProfile`, display settings и physics tick rate;
- Input Map action names, deadzones и краткие signatures событий ввода без platform-specific raw state;
- custom script classes, найденные в project-local `.cs` files;
- список рекомендуемых check commands для агента.

`api-surface.json` содержит компактную сводку tracked API manifest: путь manifest-а, число типов, число supported/partial/out-of-profile строк и короткий список типов `0.1.0 Preview`. Файл не включает полный исходный код движка.

`godot-differences.json` фиксирует только machine-readable статус выбранного API-подмножества, команду проверки `e2d api compare-godot <type>` и ссылку на локальную API compatibility документацию. Он не является руководством по обходу границ profile contract.

`scene-index.json` содержит список `*.scene.json`, root/node counts, имена и типы узлов, persistent groups, external resource references и diagnostics parsing failures.

`resource-graph.json` содержит project-local `.e2res` files, resource type, uid, external references и references из scenes к resources. Импортированные бинарные данные и содержимое cache artifacts не включаются.

`diagnostics.json` содержит summary проверок context build, skipped paths по категориям, parse diagnostics и security diagnostics. Значения секретов не попадают в diagnostics; если secret-like ключ найден, выводится только redacted path/category.

`conventions.md` содержит краткие правила для AI-агента: context pack является snapshot, generated/local-only working directories нельзя редактировать вручную, canonical tasks лежат в `.electron2d/tasks/`, а перед изменениями нужно читать актуальные project instructions.

## Security and size policy

Context pack не должен включать:

- `.git/`;
- `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/session/`, `.electron2d/user/`, `.electron2d/export-smoke/`;
- `bin/`, `obj/`, `node_modules/`, generated paths и текущий `.electron2d/context/`;
- local workflow files `TASKS.md`, `data/completed-tasks/`, `data/dev-diary/`, `CHANGELOG*`, `RELEASE-NOTES*`;
- imported binary data, images, audio, archives, executables, symbols, certificates, keystores и private key files;
- huge logs и любые text files больше лимита чтения context builder;
- значения secret-like keys: `secret`, `password`, `token`, `apiKey`, `api_key`, `privateKey`, `private_key`, `keystorePassword`, `certificatePassword`.

Для маленького проекта context pack должен оставаться компактным: tests должны проверять верхнюю границу суммарного размера и отсутствие secret values в каждом output file. Если файл исключён, context pack может сохранить только project-relative path, category и причину исключения.

## Acceptance criteria

- `e2d context build --format json` создаёт весь output layout в `.electron2d/context/` и возвращает successful stable CLI envelope с `route = "none"`.
- `project-summary.json`, `scene-index.json` и `resource-graph.json` содержат engine/.NET version, renderer profile, main scene, scene/resource index, Input Map, custom classes, diagnostics и check commands.
- Документация ясно говорит, что context pack - snapshot и может устареть относительно активной Editor-сессии.
- Context pack исключает `.git`, секреты, импортированные бинарные данные, huge logs, local workflow Markdown и полный исходный код движка.
- Automated tests покрывают состав output layout, компактный размер и отсутствие secret values.

## Фактическое состояние, ограничения и проверки

Обновлено: 2026-06-23.
Связанные спецификации: [Статический context pack проекта](static-context-pack.md); [`e2d` CLI для headless, CI и active Editor routing](../cli/e2d-cli.md).

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
