# Project Manager редактора

Обновлено: 2026-07-01.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0079`, синхронизирована с C#-инструментом репозитория после `T-0210`.
Дата: 2026-07-01.

## Цель

`Electron2D.Editor` должен получить первый Project Manager: безопасный путь создания и открытия проектов из чистой рабочей машины. Эта задача закрывает не весь визуальный интерфейс редактора, а проверяемую основу, на которую будут опираться следующие editor-docks и workflow запуска игры.

## Контракт создания проекта

- Project Manager использует canonical template `data/templates/electron2d-empty/`.
- Создание проекта принимает имя проекта, целевую папку и renderer profile.
- Имя проекта используется как имя папки, project display name, имя `<ProjectName>.e2d` и имя `.csproj`.
- Значение namespace в template-коде нормализуется до валидного C# namespace token, если имя проекта содержит пробелы или недопустимые символы.
- `.template.config/` не копируется в созданный проект.
- `<ProjectName>.e2d` сохраняет JSON-документ формата `Electron2D.ProjectSettings`, `formatVersion: 1`, `mainScene`, display defaults, input actions и выбранный `rendererProfile`.
- `<ProjectName>.e2d` может содержать встроенные разделы `exportPresets` и `reproducibilityLock`. Эти разделы заменяют отдельные корневые файлы `export_presets.e2export.json` и `electron2d.lock.json` для новых проектов и reference games.
- `global.json` остаётся отдельным файлом в корне проекта, когда проект должен управлять выбором .NET SDK для `dotnet build` и `dotnet run`: этот файл читает .NET SDK до запуска Electron2D и редактор не может заменить его встроенным разделом `.e2d`.
- `project.e2d.json` и generic `project.e2d` не являются canonical project manifest для новых проектов и reference games; legacy-файл `project.e2d.json` может читаться только как fallback для старых проектов, если рядом нет `*.e2d`.
- Папки исходного проекта используют один регистр имён: `assets/`, `resources/`, `scenes/`, `scripts/` и `.electron2d/`.
- Корень проекта не содержит `bin/` и `obj/`; это сборочные артефакты, которые игнорируются и могут безопасно удаляться.
- Создание не перезаписывает существующую непустую папку проекта.

## Контракт открытия проекта

- Project Manager принимает путь к папке проекта или путь к `<ProjectName>.e2d`.
- Если пользователь запускает `Electron2D.Editor` с единственным аргументом `<ProjectName>.e2d`, редактор открывает этот проект, валидирует `mainScene` и передаёт результат открытия в стартовый shell. Стартовый shell не должен возвращаться к пустой раскладке: он показывает имя проекта, путь проекта, путь `<ProjectName>.e2d`, путь main scene, вкладку main scene и выбранный workspace с этой сценой.
- File-argument startup для существующего проекта не ищет repository root и не требует template directory рядом с executable. Template root нужен для создания новых проектов, но double-click/open existing project должен работать из установленного или опубликованного editor executable, где рядом нет `src/Electron2D.sln`.
- Если операционная система или установщик уже связывает расширение `.e2d` с `Electron2D.Editor.exe`, двойной клик должен работать через тот же запуск с файлом проекта: редактор получает путь `<ProjectName>.e2d` первым аргументом и открывает проект без дополнительных вспомогательных файлов в репозитории.
- Открытие валидирует project settings через тот же внутренний JSON-контракт runtime, который используется шаблоном и проверками настроек.
- Открытие проверяет, что `mainScene` непустой и существует относительно корня проекта.
- Успешно открытый проект добавляется в список последних проектов и становится `lastProjectPath`.
- Неуспешное открытие возвращает диагностический текст и не добавляет путь в recent projects.

## Recent projects

- Список последних проектов хранится в user settings file формата `Electron2D.UserSettings`.
- Project Manager нормализует пути до абсолютного вида, удаляет дубликаты и держит последний открытый проект первым.
- Для `0.1.0 Preview` достаточно хранить до 10 последних проектов.

## SDK check

- Project Manager должен проверять, что команда `dotnet --version` доступна и возвращает версию SDK.
- Результат проверки содержит булево состояние, найденную версию или диагностическое сообщение.
- Smoke-проверка должна явно выводить результат SDK check, чтобы clean-machine workflow видел проблему до попытки собрать пользовательский проект.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--project-manager-smoke <work-root> --user-data-dir <user-data-dir>
```

Smoke-режим должен:

- создать новый проект из template в `<work-root>`;
- открыть созданный проект;
- проверить выбранный renderer profile;
- сохранить recent projects в `<user-data-dir>`;
- выполнить SDK check;
- вернуть exit code `0`;
- вывести machine-readable строки с путём проекта, именем проекта, renderer profile, состоянием SDK и количеством recent projects.

Editor executable также должен поддерживать проверочный аргумент:

```text
--open-project-smoke <ProjectName>.e2d --user-data-dir <user-data-dir>
```

Smoke-режим открытия должен:

- открыть переданный `<ProjectName>.e2d` тем же кодом, что используется для обычного запуска по двойному клику;
- вывести `ProjectSettingsPath`, `ProjectPath`, `MainScenePath`, `MainSceneLoaded=True`;
- завершиться без постоянного event loop, чтобы automated test мог проверить загрузку main scene без ручного закрытия окна.

Editor executable также должен поддерживать bounded-проверку стартового окна с открытым проектом:

```text
--open-project-window-smoke <ProjectName>.e2d <work-root> --user-data-dir <user-data-dir>
```

Smoke-режим стартового окна должен:

- открыть переданный `<ProjectName>.e2d` тем же кодом, что используется для обычного запуска по двойному клику;
- построить стартовый shell из результата открытия проекта, а не из пустой default layout;
- вывести `ProjectName`, `ProjectPath`, `ProjectSettingsPath`, `MainScenePath`, `ProjectLoaded=True`, `SelectedWorkspace`, `DocumentTabs` и `GameDocuments`;
- создать bounded window smoke artifacts и завершиться без ручного закрытия окна.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --project-manager-smoke ...` и подтверждает создание, открытие, recent projects, renderer profile и SDK check.
- Integration test подтверждает, что template/reference projects используют `<ProjectName>.e2d`, не создают `project.e2d.json`, `electron2d.lock.json` и `export_presets.e2export.json` в корне, а `--open-project-smoke <ProjectName>.e2d` загружает main scene.
- Integration test подтверждает, что `--open-project-window-smoke <ProjectName>.e2d ...` создаёт bounded стартовое окно с загруженным project-bound shell state: имя проекта, пути проекта и main scene, вкладка main scene и документ `res://scenes/main.scene.json`.
- Integration test подтверждает, что reference project не содержит `bin/`, `obj/` и папок исходников с разным регистром вроде `Scripts/`.
- Integration test подтверждает, что запуск с файлом проекта не зависит от удалённого корневого `tools/`-помощника; установочная регистрация расширения остаётся внешним шагом установщика, а не частью рабочего пути репозитория.
- Документация clean-machine workflow описывает команду smoke-проверки и ожидаемый результат.
- `dotnet run --project eng/Electron2D.Build -- verify licenses` проходит.
- `dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0079`, `T-0148` и рабочего пути репозитория после `T-0210`.
Дата: 2026-07-01.

## Назначение

Project Manager в `Electron2D.Editor` отвечает за первый рабочий путь проекта: создать agent-native проект из стандартного шаблона, открыть существующий проект, запомнить последние открытые проекты, выбрать renderer profile и заранее проверить доступность .NET SDK.

Это внутренняя часть editor executable. Она не добавляет новые публичные типы в runtime assembly `Electron2D`.

## Создание проекта

Project Manager создаёт проект через общий `ProjectTemplateCreator` из `Electron2D.ProjectSystem`, используя `data/templates/electron2d-empty/`:

- копирует `Electron2D.Empty.csproj`, `Program.cs`, `Scripts/MainScene.cs`, `project.e2d.json`, `scenes/main.scene.json`, `README.md`, `AGENTS.md`, `.gitignore`, `.codex/skills/` и `.electron2d/tasks/`;
- пропускает `.template.config/`, потому что это metadata для `dotnet new`, а не часть созданного проекта;
- переименовывает `.csproj` под имя проекта;
- переименовывает `project.e2d.json` в `<ProjectName>.e2d`;
- переименовывает `Scripts/` в lower-case `scripts/` и обновляет scene/script references;
- обновляет `<ProjectName>.e2d`: `name`, `rendererProfile`, embedded `exportPresets` и embedded `reproducibilityLock`;
- нормализует namespace sample-кода, чтобы проект с пользовательским именем продолжал собираться.
- создаёт или переписывает project-local `AGENTS.md`, starter skills и начальную доску `ProjectTaskManager`;
- пытается выполнить `git init` в корне нового проекта.

Создание проекта не перезаписывает существующую непустую папку.

Если `git` недоступен, файлы проекта остаются созданными, а create result получает warning diagnostic `E2D-PROJECT-0003`. На машине с доступным `git` после создания существует каталог `.git/`.

Project Manager не создаёт `TASKS.md`, `completed-tasks/` или `dev-diary/` в пользовательском проекте. Canonical task storage находится в `.electron2d/tasks/`.

## Открытие проекта

Открыть можно папку проекта или конкретный named `.e2d` file, например `Platformer.e2d`. Project Manager читает JSON через тот же внутренний формат настроек, который использует runtime для project settings. Legacy `project.e2d.json` поддерживается только как fallback для старых проектов, если рядом нет named `.e2d`. Если файл повреждён, содержит неизвестный формат или ссылается на отсутствующую main scene, открытие завершается диагностикой и не меняет список последних проектов.

После успешного открытия путь проекта сохраняется первым в recent projects, а `lastProjectPath` получает тот же путь.

## Запуск по файлу проекта

Windows double-click по `.e2d` работает через обычный file-argument path: file association запускает `Electron2D.Editor.exe "<ProjectName>.e2d"`. Редактор принимает этот единственный аргумент, открывает проект через Project Manager и передаёт результат открытия в стартовую оболочку редактора.

В стартовой оболочке должны быть доступны признаки загруженного проекта: имя проекта, абсолютный путь корня проекта, путь `<ProjectName>.e2d`, путь main scene, вкладка `main.scene.json` и game document `res://scenes/main.scene.json`. Это важно: одного обновления recent projects недостаточно, потому что пользователь после двойного клика должен сразу попасть в редактор с загруженной сценой.

Открытие существующего `.e2d` не требует наличия исходного репозитория рядом с executable. Редактор не ищет `src/Electron2D.sln` и template directory в этом path: template нужен для создания нового проекта, но не для double-click по уже существующему проекту.

Обычный Windows запуск редактора собирается как GUI application, поэтому double-click не должен показывать отдельное console window. Если запуск завершается ошибкой, диагностический текст должен идти в stderr для smoke/CI и в будущий editor error surface, но не через кратко вспыхивающую консоль для пользователя.

Локальная регистрация расширения сейчас не поставляется отдельным вспомогательным файлом репозитория. После миграции автоматизации на C#-инструмент проверяемый контракт в репозитории ограничен тем, что `Electron2D.Editor.exe "<ProjectName>.e2d"` открывает проект через обычный путь Project Manager. Регистрация расширения в установленной системе относится к будущему слою установщика и не должна требовать корневой каталог `tools/` в исходном репозитории.

Bounded-проверка без ручного закрытия окна:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --open-project-window-smoke examples\platformer\Platformer.e2d .temp\editor-open-project-window --user-data-dir .temp\editor-open-project-window-user
```

Ожидаемый результат содержит:

```text
Electron2D.Editor open project window smoke passed
ProjectName=Platformer
ProjectLoaded=True
SelectedWorkspace=2D
DocumentTabs=main.scene.json
GameDocuments=res://scenes/main.scene.json
```

## Clean-machine smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --project-manager-smoke .temp\editor-project-manager --user-data-dir .temp\editor-project-manager-user
```

Ожидаемый результат:

```text
Electron2D.Editor project manager smoke passed
ProjectName=ProjectManagerSmoke
RendererProfile=Compatibility
SdkAvailable=True
RecentProjects=1
```

Smoke-режим также выводит абсолютные пути `ProjectPath`, `ProjectSettingsPath`, `MainScenePath` и `UserSettingsPath`, чтобы тесты и CI могли проверить файлы без ручной подготовки окружения.

## Ограничения

- В этой задаче Project Manager реализован как внутренняя логика редактора и smoke-команда. Полноценный экран выбора проектов в интерактивном окне добавляется следующими editor-задачами.
- Project Manager не собирает пользовательский проект; он только проверяет доступность .NET SDK до будущих build/run workflow.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectManagerTests"
```

Полные проверки:

```powershell
dotnet run --project eng/Electron2D.Build -- verify licenses
dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600
dotnet build src\Electron2D.sln -c Release
```
