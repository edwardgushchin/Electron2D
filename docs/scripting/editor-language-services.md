# C# language services в Script workspace

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: реализовано для `T-0159`.
Обновлено: 2026-06-23.

## Назначение

`Electron2D.CSharpLanguageServices` — отдельная сборка, то есть отдельный `.NET` project с собственным compiled output, который обслуживает встроенные C# подсказки `Script` workspace. Она работает внутри процесса `Electron2D.Editor`, но не находится в runtime assembly, не зависит от UI-кода редактора и не запускает отдельный language-service process.

Сервис построен на Roslyn, официальном C# compiler и code-analysis API. Это означает, что completion, hover, diagnostics и навигация берутся из семантической модели C#-кода, а не из словаря строк.

## Live document identity

Каждый запрос и ответ содержит идентификаторы текущего документа:

- `ProjectId`;
- `DocumentId`;
- `DocumentRevision`;
- `SemanticVersion`;
- `ConfigurationHash`.

Smoke-модель принимает `CodeDocument` в памяти с unsaved text. IDE-команды работают с этим live document state напрямую: completion, signature help, hover, diagnostics, definition, references, rename, formatting и code action не требуют `WorkspaceSnapshot`.

`WorkspaceSnapshot` остаётся для reproducible build/test/run/debug workflows и пакетного анализа, где нужен стабильный снимок проекта. Для ввода символа, popup completion или hover он не создаётся.

## Поддержанные операции

Реализованный минимум для `0.1-preview`:

- completion показывает symbols из Electron2D API, локальные symbols текущего файла и members, найденные через Roslyn semantic model;
- signature help показывает overload `Vector2(float x, float y)` и активный параметр;
- hover/Quick Info возвращает display string symbol-а и XML documentation summary для documented member текущего C# document;
- live diagnostics возвращают compiler diagnostics с project-relative path, line, column, severity, code и message без ручного build;
- go to definition и find references возвращают стабильные source spans с `DocumentId` и `DocumentRevision`;
- rename symbol возвращает deterministic text edits с expected revision и не применяет их автоматически;
- document formatting возвращает изменённый текст, сформированный через C# syntax formatting;
- basic code action добавляет недостающий `using System.Collections.Generic`;
- stale result явно помечается как discarded, если ответ пришёл для более старой `DocumentRevision`;
- cancellation предыдущего запроса, diagnostics debounce и reload trigger для project/package changes фиксируются machine-readable flags.

Если semantic model нельзя построить, сервис возвращает structured diagnostic `E2D-SCRIPT-0003` вместо необработанного exception наружу.

## E2D-SCRIPT-0003

`E2D-SCRIPT-0003` означает, что C# language service не смог построить semantic model для текущего script document.

Diagnostic относится к домену `Script`, имеет severity `Error` и должен отображаться как обычная structured diagnostic запись с безопасным текстом для пользователя. Текущий smoke path создаёт этот diagnostic из управляемой failure-модели, чтобы будущие слои UI, Tooling и документационные checks могли ссылаться на стабильный код.

## Visual harness

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-language-services-smoke .temp\script-language-services
```

Создаёт:

- `.temp/script-language-services/script-language-services.state.json`;
- `.temp/script-language-services/visual/script-language-services.png`;
- `.temp/script-language-services/visual/script-language-services.analysis.json`.

PNG является обязательным screenshot artifact для visual acceptance задачи T-0159. JSON analysis фиксирует selected workspace, bounds completion popup, hover/Quick Info panel, diagnostics panel, signature state, definition target, references count, rename preview, formatting/code-action result, stale response marker, text overflow count, clickable controls и forbidden UI matches.

В текущей проверке агент открыл `script-language-services.png` и подтвердил:

- выбран `Script` workspace, внешняя IDE не открывается;
- completion popup расположен рядом с caret/current line и содержит `Sprite2D`, `Velocity` и `Position`;
- hover/Quick Info panel виден рядом с documented method и показывает XML summary;
- signature help показывает `Vector2(float x, float y)` и active parameter `1`;
- нижняя diagnostics panel показывает `CS0103`, severity `Error` и путь `Scripts/HeroController.cs`;
- справа видны `Language Services`, `Roslyn Semantic`, `Stale Discard` и `Config Hash`;
- текст не выходит за границы контейнеров, а `3D`, `AssetLib`, GDScript UI и `.gd` отсутствуют.

## Проверки

Focused test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptLanguageServicesTests"
```

Smoke-команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-language-services-smoke .temp\script-language-services
```

Документационный verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1
```
