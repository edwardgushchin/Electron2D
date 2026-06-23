# Script workflow в редакторе

Статус: документация реализации для `T-0046` и `T-0158`.
Дата: 2026-06-23.

## Назначение

`Electron2D.Editor` содержит внутреннюю модель script workflow для `0.1.0 Preview`: создать C# script, открыть его во встроенной модели редактора кода, изменить и сохранить текст, прикрепить script к node, собрать проект и запустить проект после successful rebuild.

Workflow не добавляет runtime compilation и не загружает пользовательские assemblies динамически. Script остаётся обычным `.cs` файлом проекта игры и компилируется обычной .NET toolchain.

Центральное рабочее пространство `Script` описано отдельно: [Script workspace редактора](../editor/script-workspace.md).

## Текущее поведение

Модель editor script workflow поддерживает:

- создание script file в `Scripts/`;
- генерацию namespace и class name из project name и имени script;
- открытие script file как code document;
- tracking несохранённых изменений;
- сохранение изменённого текста;
- attach script к node через `SceneFileDocument`;
- запуск project build;
- парсинг compiler diagnostics в file/line/column/code/message;
- запуск проекта после successful build.

Attach script меняет serialized node type на полное имя script class и сохраняет scene тем же serializer, который используют Scene Tree dock и Inspector.

Отдельная UI-модель `Script` workspace добавляет проверяемый editor surface: вкладки документов, line gutter, caret, selection, syntax token categories, search/replace, go-to-line, save/save-all, clipboard round-trip, grouped undo для AI/refactoring и обработку внешних изменений файла. Внутренняя `TextBufferEditSession` означает, что набор текста обновляет текущий `CodeDocument`, но не создаёт отдельные project operations в `OperationJournal` на каждый символ.

## Smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-workflow-smoke .temp\editor-script-workflow
```

Ожидаемый результат включает:

```text
Electron2D.Editor script workflow smoke passed
CreatedScriptExists=True
CompilerErrorCount>0
FirstCompilerErrorCode=CS1002
FixedBuildSucceeded=True
RunExitCode=0
RerunAfterRebuild=True
```

Smoke-команда создаёт временный проект, записывает невалидный script, проверяет compiler diagnostic, сохраняет исправленный script, прикрепляет его к node, собирает и запускает проект.

Дополнительная UI smoke-команда для `Script` workspace:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --script-workspace-smoke .temp\script-workspace
```

Она сохраняет state JSON, PNG screenshot и JSON analysis artifact для проверки встроенного C# editor UI, отсутствия text overflow, наличия interactive controls и соответствия workspace switcher `2D | Script | Game | Tasks`.

## Ограничения

- Это не полноценная IDE: autocomplete, debugger UI, language server и semantic refactoring не входят в текущий baseline.
- Hot Reload не является обязательным workflow. После изменения script проект пересобирается.
- Runtime не компилирует scripts из текста и не загружает их динамически.
- Текущий `Script` workspace является model-first UI surface: live desktop text input и полноценная привязка к окну подключаются следующими задачами.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkflowTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkspaceTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
