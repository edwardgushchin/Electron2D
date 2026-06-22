# Script workflow в редакторе

Статус: документация реализации для `T-0046`.
Дата: 2026-06-22.

## Назначение

`Electron2D.Editor` содержит внутреннюю модель script workflow для `0.1.0 Preview`: создать C# script, открыть его во встроенной модели редактора кода, изменить и сохранить текст, прикрепить script к node, собрать проект и запустить проект после successful rebuild.

Workflow не добавляет runtime compilation и не загружает пользовательские assemblies динамически. Script остаётся обычным `.cs` файлом проекта игры и компилируется обычной .NET toolchain.

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

## Ограничения

- Это не полноценная IDE: autocomplete, debugger UI, language server и semantic refactoring не входят в текущий baseline.
- Hot Reload не является обязательным workflow. После изменения script проект пересобирается.
- Runtime не компилирует scripts из текста и не загружает их динамически.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorScriptWorkflowTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
