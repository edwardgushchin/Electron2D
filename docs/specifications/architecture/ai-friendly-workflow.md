# AI-friendly workflow Electron2D 0.1

Статус: целевая архитектурная спецификация.
Задача: `T-0114`.
Обновлено: 2026-06-21.

## Позиционирование

Electron2D — C#-first кроссплатформенный 2D-движок с Godot-подобным API, спроектированный для совместной разработки человеком и AI-агентами.

Ключевое обещание `0.1.0`: AI-агент может изучить проект, изменить сцену и C#-код, запустить проверку, получить структурированную диагностику, увидеть результат рендера и собрать игру без управления редактором мышью.

Это архитектурное требование, а не требование встроенного чата. AI-friendly означает, что проект и инструменты доступны через стабильные файлы, командную строку, локальный MCP-сервер и машиночитаемую документацию. Встроенная LLM, генерация игры по одному prompt, облачный аккаунт и привязка к одному AI-провайдеру в `0.1.0` не нужны.

Короткая продуктовая формулировка:

> Electron2D — 2D-движок на C# для совместной разработки человеком и AI.

Английский слоган:

> Electron2D — a C# 2D engine built for humans and AI agents.

## Обновлённая цель 0.1

Electron2D 0.1 позволяет человеку или AI-агенту создать небольшую законченную 2D-игру на C#, используя редактор либо полностью автоматизированный рабочий процесс, а затем запустить, протестировать и экспортировать один проект под Windows, Linux, macOS, Android и iOS.

Главный вертикальный срез:

```text
Описание задачи
    ↓
Создание проекта
    ↓
Создание и изменение сцен
    ↓
Написание C#-кода
    ↓
Валидация
    ↓
Запуск и автоматизированное управление
    ↓
Логи, диагностика и screenshots
    ↓
Исправление
    ↓
Тесты
    ↓
Экспорт
```

Этот цикл должен работать без GUI automation, то есть без имитации кликов мышью и без необходимости управлять `Electron2D.Editor` как визуальным приложением.

## Единое ядро инструментов

`Electron2D.Tooling` — общий слой операций над проектом. Его используют редактор, CLI, MCP-сервер, CI и будущие IDE-интеграции.

Минимальный набор сервисов:

```text
Electron2D.Tooling
├── ProjectService
├── SceneService
├── ResourceService
├── ScriptService
├── ImportService
├── BuildService
├── TestService
├── ExportService
└── DocumentationService
```

Операция «добавить `Sprite2D` в сцену» должна быть одной и той же независимо от того, вызвал её пользователь через Scene Tree dock, CLI, MCP tool, тест или будущий IDE-плагин. Редактор не должен иметь отдельную приватную модель проекта, недоступную автоматизации.

## CLI как обязательный продуктовый интерфейс

`e2d` — командный интерфейс Electron2D. Это не вспомогательный launcher, а безоконный интерфейс ко всему основному рабочему процессу.

Минимальные группы команд:

- `project create`, `project inspect`, `project validate`;
- `scene create`, `scene inspect`, `scene add-node`, `scene set`, `scene attach-script`, `scene connect`;
- `resource inspect`, `resource dependencies`;
- `import`;
- `build`;
- `run`;
- `test`;
- `export`;
- `docs search`, `docs type`, `docs member`, `docs example`;
- `api compare-godot`;
- `mcp serve`;
- `context build`;
- `doctor`.

Каждая команда должна поддерживать:

- `--help`;
- `--format text|json|jsonl`;
- `--quiet`;
- `--verbose`;
- `--dry-run`, если команда меняет проект;
- `--project <path>`.

Изменяющие команды возвращают структурированный результат с `success`, `operation`, `changedFiles`, `createdObjects` и `diagnostics`.

Ошибки должны иметь стабильные коды, например `E2D-SCENE-0004`, `E2D-RESOURCE-0012`, `E2D-SHADER-0007`, `E2D-EXPORT-ANDROID-0011`. Случайные текстовые сообщения без кода не считаются достаточной диагностикой для AI-friendly workflow.

## Текстовый формат проекта

Все семантически значимые данные проекта должны храниться в исходных текстовых файлах:

- project settings;
- scenes;
- resources;
- input map;
- translations;
- animation data;
- sprite frame definitions;
- themes;
- export presets без секретов.

Бинарными могут быть изображения, аудио, шрифты, импортированный cache, скомпилированные шейдеры и production asset packs.

Текстовый формат обязан иметь:

- стабильный порядок полей;
- постоянные UID;
- явные имена типов;
- явные ссылки на внешние ресурсы;
- отсутствие абсолютных путей;
- отсутствие editor-specific шума;
- небольшие локальные diff при изменении одного свойства;
- schema version;
- автоматические миграции;
- команду валидации;
- canonical formatter.

Для JSON-представлений должна публиковаться JSON Schema. Целевая версия схемы для `0.1.0` — JSON Schema Draft 2020-12.

## Машиночитаемый API manifest

Electron2D должен поставлять версионированный API manifest в JSON. Manifest нужен, чтобы AI-агенты, CLI, Inspector, Wiki, source generators и будущий language server не угадывали отличия Electron2D от Godot.

Manifest содержит:

- engine version;
- классы и наследование;
- constructors/factories;
- свойства;
- методы;
- сигналы;
- enum и flags;
- значения по умолчанию;
- nullability;
- поддерживаемые Variant-типы;
- доступность по платформам;
- требования к renderer profile;
- статус `supported`, `partial`, `experimental`;
- отличия от Godot;
- примеры использования;
- версию появления или изменения элемента.

## Карта совместимости с Godot

Godot-like API требует формальной карты отличий, а не только общей фразы в README. Для каждого публичного типа документация должна содержать блок:

```text
Compatibility with Godot
Supported:
Partial:
Not supported:
Behavioral differences:
```

Команда `e2d api compare-godot <type> --format json` должна возвращать совместимые члены, неподдерживаемые члены и поведенческие отличия. Без этого AI-агенты будут создавать убедительно выглядящий, но некомпилируемый Electron2D-код.

## MCP-сервер

MCP означает Model Context Protocol: локальный протокол, через который AI-клиент получает типизированные tools, resources и prompts. Electron2D должен предоставлять локальный, необлачный MCP-сервер, не привязанный к конкретной модели или поставщику:

```bash
e2d mcp serve
```

MCP остаётся тонким адаптером над `Electron2D.Tooling`, а не второй независимой реализацией.

Минимальные MCP resources:

- `electron2d://project/summary`;
- `electron2d://project/settings`;
- `electron2d://project/scenes`;
- `electron2d://project/resources`;
- `electron2d://project/diagnostics`;
- `electron2d://scene/{uid}`;
- `electron2d://resource/{uid}`;
- `electron2d://api/type/{name}`;
- `electron2d://api/godot-compatibility/{name}`;
- `electron2d://runtime/capabilities`;
- `electron2d://docs/topic/{name}`.

Минимальные MCP tools:

- `project_validate`, `project_build`, `project_run`, `project_test`, `project_export`;
- `scene_create`, `scene_inspect`, `scene_add_node`, `scene_remove_node`, `scene_move_node`, `scene_set_property`, `scene_attach_script`, `scene_connect_signal`;
- `resource_inspect`, `resource_import`, `resource_find_references`;
- `runtime_start`, `runtime_stop`, `runtime_step`, `runtime_inject_input`, `runtime_capture_frame`, `runtime_get_scene_tree`, `runtime_get_diagnostics`.

## Контекстный пакет проекта

`e2d context build` генерирует компактный контекст для AI-агента:

```text
.electron2d/context/
├── project-summary.json
├── api-surface.json
├── godot-differences.json
├── scene-index.json
├── resource-graph.json
├── diagnostics.json
└── conventions.md
```

Контекст включает версию движка и .NET, renderer profile, main scene, список сцен, основные узлы, пользовательские классы, Input Map, autoload/services, связи ресурсов, текущие ошибки, платформенные ограничения и команды сборки/тестирования.

Контекст не должен содержать импортированные бинарные данные, весь исходный код движка, секреты подписи, огромные логи, содержимое `.git` и неиспользуемые API.

## Project-local `AGENTS.md` и skills

Каждый новый проект должен содержать `AGENTS.md` — предсказуемое место для инструкций coding-агентам. Шаблон должен быть похож по назначению на глобальный пользовательский `AGENTS.md`, но не должен копировать приватные правила пользователя.

Проектный `AGENTS.md` должен описывать:

- версию Electron2D и .NET;
- renderer profile;
- команды validate/build/test/run/export;
- структуру проекта;
- запрет редактировать import cache;
- правило стабильных UID;
- правило проверки через `e2d validate`;
- предупреждение не предполагать полную совместимость с Godot;
- команду `e2d api compare-godot <type>` для спорных API.

Новый проект также получает стартовые project-local skills для создания сцены, написания gameplay-кода, импорта ресурсов, запуска тестов и подготовки экспорта.

## Автоматизированный запуск игры

AI-агент должен уметь не только собрать проект, но и проверить поведение. `e2d run` должен поддерживать headless-сценарий:

```bash
e2d run --scene scenes/main.e2scene --frames 600 --fixed-delta 0.0166667 --input tests/input/start-game.json --capture-frame 300 --output artifacts/run-001
```

Результат запуска:

```text
artifacts/run-001/
├── result.json
├── diagnostics.json
├── runtime.log.jsonl
├── frame-0300.png
├── scene-tree-final.json
└── performance.json
```

В результате должны быть screenshot, итоговое дерево узлов, необработанные исключения, failures тестов, FPS/frame time, draw calls, число физических тел, утечки ресурсов и общий результат.

## Тестовый framework для игр

Кроме обычного `dotnet test`, Electron2D 0.1 должен иметь средства для scene tests и visual tests.

Минимальные возможности:

- fixed seed;
- fixed timestep;
- advance one frame;
- advance physics frame;
- имитация Input Map;
- поиск узлов;
- чтение свойств;
- ожидание сигнала;
- screenshot;
- pixel-diff;
- timeout;
- проверка утечек ресурсов после завершения сцены.

Битовая детерминированность физики между всеми платформами не требуется, но один тест должен воспроизводиться на одной платформе при одинаковой конфигурации.

## Структурированная диагностика

Любая ошибка должна иметь структуру:

- `code`;
- `severity`;
- `category`;
- `message`;
- `file`;
- `line`;
- `column`;
- scene UID;
- node path;
- resource UID;
- related locations;
- suggested fix;
- documentation URI.

Для compiler, shader и validation diagnostics должен быть возможен вывод SARIF 2.1.0:

```bash
e2d validate --format sarif > electron2d.sarif
```

## Наблюдаемое состояние runtime

Development build должен позволять получить состояние запущенной игры:

- scene tree;
- exported properties;
- transforms;
- visibility;
- animation state;
- physics contacts;
- Input Map state;
- logs;
- frame capture;
- input injection;
- pause and step.

Пример команд:

```bash
e2d runtime tree
e2d runtime inspect /root/Main/Player
e2d runtime metrics
e2d runtime screenshot
e2d runtime pause
e2d runtime step --frames 1
```

## Безопасное изменение проекта

Изменяющие операции AI должны быть транзакционными:

```text
parse
    ↓
validate current state
    ↓
apply operation in memory
    ↓
validate resulting state
    ↓
write temporary files
    ↓
atomic replace
    ↓
return changed files and diagnostics
```

Обязательны `--dry-run`, atomic writes, automatic backup для миграций, защита от записи за пределами project root, запрет изменения import cache через scene API, список затронутых файлов, validation before commit, стабильные UID, отсутствие молчаливого удаления неизвестных свойств и audit log операций MCP.

MCP-сервер не должен автоматически подписывать Android/iOS сборки, читать keystore/certificates, публиковать игру, удалять произвольные файлы или выполнять произвольный shell без отдельного разрешения.

## Воспроизводимость

Проект должен фиксировать Electron2D version, .NET SDK range, NuGet dependencies, native package versions, asset importer versions, renderer profile, physics backend version и serialization schema version.

Минимальные файлы:

- `global.json`;
- `electron2d.lock.json`.

`e2d doctor --format json` проверяет установленный .NET SDK, версию Electron2D, native runtime, Android SDK/NDK, Xcode, export templates, Vulkan/Metal capabilities и доступность signing configuration без раскрытия секретов.

## Документация, пригодная для AI

Wiki остаётся, но одной Wiki недостаточно. Документация должна поставляться вместе с конкретной версией движка в трёх представлениях:

| Представление | Для кого |
| --- | --- |
| Wiki/HTML | Человек |
| XML documentation | C# IDE и compiler tooling |
| JSON API manifest | AI, CLI, Inspector, генераторы |

CLI должен уметь искать локальную документацию:

```bash
e2d docs search "move and slide"
e2d docs type CharacterBody2D --format json
e2d docs member CharacterBody2D.MoveAndSlide
e2d docs example "platformer movement"
```

Документация каждого публичного API должна содержать назначение, сигнатуру, lifecycle restrictions, thread affinity, ownership/disposal, пример, ошибки, платформенные ограничения, renderer restrictions и отличия от Godot.

## Что AI-friendly не означает

Первая версия не обязана иметь:

- встроенное окно ChatGPT;
- собственную LLM;
- генерацию ассетов;
- генерацию игры по одному prompt;
- облачный аккаунт;
- автономную публикацию игры;
- API, привязанный к одному поставщику;
- обучение модели на пользовательском проекте;
- визуального агента, кликающего по Editor;
- возможность произвольно выполнять shell-команды через движок.

Правильная зависимость:

```text
AI assistant
      ↓
CLI / MCP
      ↓
Electron2D.Tooling
      ↓
Project model
```

Неправильная зависимость:

```text
Electron2D.Editor
      ↓
OpenAI/Anthropic/Gemini integration, зашитая напрямую в редактор
```

## Приоритеты 0.1

Critical для 0.1:

- текстовые сцены и ресурсы;
- стабильные UID;
- полный CLI workflow, то есть основной рабочий процесс через командную строку;
- headless validation/build/test/export;
- structured diagnostics;
- API manifest;
- карта отличий от Godot;
- runtime input injection;
- frame capture;
- scene tests;
- локальная документация.

High для 0.1:

- `AGENTS.md` template;
- MCP-сервер;
- context pack;
- runtime scene inspection;
- visual regression tests.

Можно отложить: встроенный редактор C#, visual shader editor, сложный `AnimationTree`, skeletal animation, расширенный particle editor, полноценный profiler UI, plugin marketplace, сложная dock-система, встроенный AI-chat и автоматическая публикация в магазины.

## Критерий приёмки AI-friendly

Нужен отдельный benchmark. Тестовый агент получает установленный Electron2D, пустую директорию, документацию Electron2D, CLI/MCP, текстовое описание задачи, без доступа к исходному коду движка, без ручной помощи и без управления Editor мышью.

Агент должен выполнить пять заданий:

1. Создать проект с main scene и экспортируемым свойством.
2. Изменить сцену: добавить `Sprite2D`, `Camera2D`, `CollisionShape2D`, назначить ресурсы и сохранить сцену.
3. Реализовать механику движения персонажа через Input Map.
4. Получить структурированную диагностику отсутствующего ресурса или неверного свойства и исправить её.
5. Запустить scene test, получить screenshot и экспортировать desktop build.

Условия успеха:

- агент не редактировал generated cache;
- агент не использовал недоступный Godot API;
- проект открывается в Editor;
- сцены проходят round-trip;
- тесты проходят;
- сборка запускается;
- все изменения находятся в ожидаемых source-файлах;
- задача выполнена через документированный публичный интерфейс.

Целевой показатель первой версии: не менее 80% эталонных задач успешно выполняются минимум двумя разными AI-агентами без специальных скрытых инструкций.

## Источники

- [Godot command line tutorial](https://docs.godotengine.org/en/latest/tutorials/editor/command_line_tutorial.html) - headless запуск, импорт и экспорт как устоявшийся паттерн игровых инструментов.
- [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12) - целевая версия схем для JSON-представлений.
- [Model Context Protocol Tools](https://modelcontextprotocol.io/specification/draft/server/tools) - типизированные tools/resources/prompts для локальной AI-интеграции.
- [AGENTS.md convention](https://agents.md/) - предсказуемый файл инструкций для coding agents.
- [SARIF 2.1.0](https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html) - формат структурированного вывода результатов анализа.
- [global.json overview](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) - фиксация .NET SDK для воспроизводимой сборки.
