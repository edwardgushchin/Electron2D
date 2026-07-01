# Формат проекта и шаблон `electron2d-empty`

Обновлено: 2026-06-27.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задачи: `T-0006`, `T-0148`.
Обновлено: 2026-06-27.

## Цель

Новый проект Electron2D должен начинаться из минимального шаблона без legacy API. После `T-0044` шаблон также фиксирует минимальную C# script model: обычный class file наследуется от `Node`, компилируется обычной .NET toolchain и получает lifecycle callbacks.

## Шаблон

Canonical template:

```text
data/templates/electron2d-empty/
```

Шаблон должен содержать:

- `.template.config/template.json`
- `Electron2D.Empty.csproj`
- `global.json`
- `electron2d.lock.json`
- `Program.cs`
- `Scripts/MainScene.cs`
- `project.e2d.json`
- `scenes/main.scene.json`
- `README.md`
- `.gitignore`
- `AGENTS.md`
- `.codex/skills/electron2d-scene/SKILL.md`
- `.codex/skills/electron2d-gameplay-code/SKILL.md`
- `.codex/skills/electron2d-resource-import/SKILL.md`
- `.codex/skills/electron2d-run-test/SKILL.md`
- `.codex/skills/electron2d-export/SKILL.md`

`TASKS.md`, `completed-tasks/` и `dev-diary/` не входят в пользовательский шаблон. Эти файлы относятся только к рабочему процессу репозитория Electron2D.

## Минимальный формат проекта

`project.e2d.json` должен быть валидным project settings документом `Electron2D.ProjectSettings` и указывать:

- `format`
- `formatVersion`
- `name`
- `version`
- `engineVersion`
- `mainScene`
- `rendererProfile`
- `physicsTicksPerSecond`
- `input.actions`
- `display`

`global.json` и `electron2d.lock.json` должны соответствовать [Reproducibility lock и `e2d doctor`](../project-system/reproducibility-lock-and-doctor.md). Шаблон фиксирует .NET SDK, версию Electron2D, target framework, package metadata, renderer profile, physics backend marker, serialization schema version, export template version и signing policy без секретных значений.

`scenes/main.scene.json` должен описывать пустую сцену без legacy component-полей.

## AI-ready project surface

Каждый новый проект, созданный через Project Manager или `e2d project create`, получает предсказуемую рабочую поверхность для coding agents без копирования приватных пользовательских правил.

### Project-local `AGENTS.md`

`AGENTS.md` создаётся в корне проекта и должен описывать:

- текущую версию Electron2D и требуемую версию .NET;
- выбранный renderer profile;
- команды `e2d validate`, `dotnet build`, `dotnet test`, `e2d run`, `e2d export` и `e2d api compare-godot <type>`;
- структуру проекта: `project.e2d.json`, `scenes/`, `Scripts/`, `.electron2d/tasks/`, `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/user/`;
- запрет редактировать `.electron2d/import-cache/` и другие generated/local-only каталоги вручную;
- правило сохранять stable UID и проверять проект через `e2d validate`;
- предупреждение не использовать внешний API вне утверждённого Electron2D 2D-профиля;
- правило подключаться к активной Editor-сессии, если она открыта;
- правило использовать `ProjectTaskManager` через Editor, Tooling или MCP, а не прямую правку task storage files;
- правило отправлять завершённую агентом работу на человеческую приёмку через `task_submit_for_acceptance`.

`AGENTS.md` не должен упоминать repository-local `TASKS.md`, `completed-tasks/` или `dev-diary/` как источник задач пользовательского проекта.

### Starter skills

Шаблон поставляет project-local skills под `.codex/skills/`. Минимальный набор:

- `electron2d-scene` — создание и изменение сцен;
- `electron2d-gameplay-code` — написание C# gameplay-кода;
- `electron2d-resource-import` — импорт ресурсов и работа с UID;
- `electron2d-run-test` — проверка через validate, build, run и scene tests;
- `electron2d-export` — подготовка export presets и проверка package contents.

Каждый `SKILL.md` должен иметь YAML front matter с `name` и `description`, быть самодостаточным и не содержать абсолютных путей, секретов или ссылок на локальный workflow репозитория Electron2D.

### Project tasks storage

Новый проект получает начальную доску `.electron2d/tasks/board.e2tasks` и стартовую задачу `.electron2d/tasks/welcome.e2task`. Эти документы являются `EditorMetadata`: они доступны Editor, Tooling, CLI и MCP, но не являются игровыми ресурсами и не попадают в runtime snapshot или production package contents.

Стартовая задача должна быть в статусе `Backlog`, содержать acceptance criteria для первого запуска проекта и использовать canonical formatter `ProjectTaskSerializer`.

### `.gitignore` и git init

Template `.gitignore` не должен игнорировать `.electron2d/` целиком. Обязательные ignored paths:

```gitignore
.electron2d/import-cache/
.electron2d/workspaces/
.electron2d/context/
.electron2d/session/
.electron2d/user/
```

`.electron2d/tasks/` должен быть явно отслеживаемым по умолчанию: в `.gitignore` не должно быть правила, которое скрывает `.electron2d/tasks/`.

Создание проекта пытается выполнить `git init` в корне нового проекта. Если `git` недоступен или команда завершается ошибкой, создание проекта не откатывает файлы шаблона, но возвращает structured warning с понятным сообщением. Если `git` доступен, после создания должен существовать каталог `.git`.

### CLI `project create`

`e2d project create <name> --output <projects-root> [--renderer-profile Standard|Compatibility|Automatic] --format json` создаёт тот же AI-ready project surface, что и Project Manager.

JSON-результат должен содержать:

- `projectName`;
- `projectPath`;
- `projectSettingsPath`;
- `mainScenePath`;
- `rendererProfile`;
- `gitInitialized`;
- `taskBoardPath`;
- `starterSkillCount`;
- `agentInstructionsPath`.

Команда не должна создавать `TASKS.md`, `completed-tasks/` или `dev-diary/`.

## Верификация

```bash
dotnet run --project eng/Electron2D.Build -- verify project-template
```

Verifier должен:

1. подтвердить наличие обязательных файлов шаблона;
2. проверить JSON-форму `project.e2d.json`;
3. проверить JSON-форму `.electron2d/tasks/board.e2tasks` и `.electron2d/tasks/welcome.e2task`;
4. подтвердить наличие `AGENTS.md`, `.gitignore`, пяти стартовых skills и `.electron2d/tasks/`;
5. подтвердить отсутствие `TASKS.md`, `completed-tasks/` и `dev-diary/`;
6. подтвердить, что `.gitignore` не исключает `.electron2d/tasks/`;
7. вернуть структурированные JSON-диагностики.

Эта C#-команда является manifest/shape check, то есть проверкой состава и формы файлов. Для `T-0214` именно она является целевой поверхностью проверки формы шаблона и стартовых manifest-файлов. Полный pack/restore/build/run сценарий остаётся миграционным долгом для будущего C#-маршрута и не входит в текущую поддержанную поверхность проверки `T-0214`.

## Editor run override

Шаблон должен понимать переменную процесса `ELECTRON2D_CURRENT_SCENE`. Если она задана, проект запускает указанный scene file как относительный путь внутри проекта и не меняет `project.e2d.json`.

## Фактическое состояние, ограничения и проверки

Статус: реализованный AI-ready шаблон.
Задачи: `T-0006`, `T-0148`.
Обновлено: 2026-06-27.

## Где находится шаблон

```text
data/templates/electron2d-empty/
```

Шаблон содержит .NET template metadata, минимальный `.csproj`, `Program.cs`, `Scripts/MainScene.cs`, `project.e2d.json`, `global.json`, `electron2d.lock.json`, пустую сцену `scenes/main.scene.json`, `AGENTS.md`, `.gitignore`, стартовые project-local skills и начальные task documents в `.electron2d/tasks/`.

`project.e2d.json` уже использует текущий project settings формат: `Electron2D.ProjectSettings`, `formatVersion: 1`, имя проекта, версию проекта, engine version, main scene, renderer profile, physics tick rate, пустой `input.actions` и display/window defaults.

`global.json` фиксирует .NET SDK `10.0.101` с `rollForward: latestFeature`. `electron2d.lock.json` фиксирует Electron2D package version, target framework, package metadata, renderer profile, physics backend marker, serialization schema version, export template version и signing policy `referencesOnly`.

Проверка этих файлов описана в [Reproducibility lock и e2d doctor](../project-system/reproducibility-lock-and-doctor.md).

## AI-ready файлы

`AGENTS.md` в пользовательском проекте описывает версию Electron2D, .NET SDK, выбранный renderer profile, команды `e2d validate`, `dotnet build`, `dotnet test`, `e2d run`, `e2d export`, `e2d api compare-godot <type>`, структуру проекта, правила stable UID и запрет ручной правки generated/local-only каталогов.

Шаблон не создаёт `TASKS.md`, `completed-tasks/` или `dev-diary/`. Для пользовательских задач используется `ProjectTaskManager`: начальная доска хранится в `.electron2d/tasks/board.e2tasks`, стартовая задача — в `.electron2d/tasks/welcome.e2task`.

Project-local skills находятся в `.codex/skills/`:

- `electron2d-scene`;
- `electron2d-gameplay-code`;
- `electron2d-resource-import`;
- `electron2d-run-test`;
- `electron2d-export`.

`.gitignore` игнорирует generated/local-only каталоги `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/` и `.electron2d/user/`, но не игнорирует `.electron2d/tasks/`.

## Создание проекта

Project Manager и `e2d project create` используют общий `ProjectTemplateCreator` из `Electron2D.ProjectSystem`. Он копирует template, удаляет `.template.config/` из созданного проекта, переименовывает `.csproj`, обновляет `project.e2d.json` и `electron2d.lock.json`, переписывает namespace sample-кода, создаёт AI-ready файлы и пытается выполнить `git init`.

Если `git init` успешен, в проекте появляется `.git/`. Если `git` недоступен или завершается ошибкой, проектные файлы остаются созданными, а результат содержит warning diagnostic `E2D-PROJECT-0003`.

CLI форма:

```bash
e2d project create MyGame --output .\projects --renderer-profile Compatibility --format json
```

JSON-результат содержит `projectName`, `projectPath`, `projectSettingsPath`, `mainScenePath`, `rendererProfile`, `gitInitialized`, `taskBoardPath`, `starterSkillCount` и `agentInstructionsPath`.

## Как проверять

```bash
dotnet run --project eng/Electron2D.Build -- verify project-template
```

Команда выполняет C# manifest/shape check для `data/templates/electron2d-empty`: проверяет обязательные файлы, JSON-форму project settings и стартовых задач, `AGENTS.md`, `.gitignore`, пять starter skills, `.electron2d/tasks/board.e2tasks`, `.electron2d/tasks/welcome.e2task`, отсутствие `TASKS.md`, `completed-tasks/` и `dev-diary/`, а также то, что `.gitignore` не скрывает `.electron2d/tasks/`.

Полный pack/restore/build/run verifier должен быть перенесён в C#-инструмент перед тем, как его можно будет объявить текущим gate. Именно полный verifier должен проверять output запуска созданного проекта:

```text
Electron2D empty scene loaded: scenes/main.scene.json
Electron2D C# script lifecycle: _EnterTree,_Ready
Electron2D C# script services: tree=True,text=True
```

При запуске из редактора template также понимает переменную процесса `ELECTRON2D_CURRENT_SCENE`. Это относительный project path для запуска выбранной scene без изменения `project.e2d.json`.

## Script sample

`Scripts/MainScene.cs` наследуется от `Electron2D.Node`, переопределяет `_EnterTree()` и `_Ready()`, а в `_Ready()` обращается к `GetTree()` и `RenderingServer`. Это минимальный baseline C# scripting без runtime compilation и без dynamic assembly load.
