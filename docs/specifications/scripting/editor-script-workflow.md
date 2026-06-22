# Script workflow в редакторе

Статус: целевая спецификация для `T-0046`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` должен дать минимальный встроенный workflow для C# scripts без обязательного внешнего IDE: создать script file, открыть его во встроенной модели редактора кода, изменить и сохранить текст, прикрепить script к узлу сцены, собрать проект, показать ошибки компиляции и запустить проект после успешной пересборки.

Эта задача не добавляет runtime C# compilation, dynamic assembly load, Hot Reload или полноценную IDE. Пользовательский script остаётся обычным C# source file в проекте игры и компилируется обычной .NET toolchain вместе с проектом.

## Встроенный редактор кода

Минимальная модель встроенного редактора кода должна поддерживать:

- создание script file из шаблона;
- открытие существующего script file;
- замену текста документа;
- признак несохранённых изменений;
- сохранение текста на диск;
- повторное открытие сохранённого текста.

В `0.1.0 Preview` не требуются language server, autocomplete, semantic refactoring, debugger UI или полноценная вкладочная IDE. Важно, чтобы editor workflow был достаточен для создания, исправления и сохранения C# code без выхода во внешний инструмент.

## Attach к узлу

Attach script к node должен работать с текущим scene serialization:

- editor получает `SceneFileDocument`;
- находит node по id;
- меняет serialized node type на полное имя script class;
- сохраняет scene file тем же `SceneFileTextSerializer`;
- после сохранения scene round-trip остаётся стабильным.

Если node не найден или scene file повреждён, операция должна завершаться диагностикой, а не молча менять другой файл.

## Build и compiler diagnostics

Editor build workflow должен запускать project build через .NET toolchain и возвращать структурированный результат:

- exit code;
- полный stdout/stderr;
- список diagnostics с severity, code, file, line, column и message;
- признак success/failure.

Compiler errors должны быть actionable: editor должен знать файл, строку, колонку, код ошибки и текст сообщения. Это нужно, чтобы будущий визуальный dock мог показать ошибку и перейти к строке в embedded code editor.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--script-workflow-smoke <work-root>
```

Smoke-режим должен:

- создать временный проект;
- создать `PlayerController.cs` через editor script workflow;
- открыть script во встроенной модели редактора кода;
- сохранить невалидный C# текст и подтвердить compiler error;
- сохранить исправленный C# текст;
- прикрепить script к node в scene file;
- собрать проект;
- запустить проект после successful rebuild;
- вывести machine-readable строки: `ProjectPath`, `ScriptPath`, `CreatedScriptExists`, `OpenedScript`, `DirtyBeforeSave`, `DirtyAfterSave`, `AttachedNodeType`, `CompilerErrorCount`, `FirstCompilerErrorCode`, `FirstCompilerErrorLine`, `FirstCompilerErrorColumn`, `FixedBuildSucceeded`, `RunExitCode`, `RunOutputContainsMessage`, `SceneRoundTripStable`, `RerunAfterRebuild`;
- вернуть exit code `0`, если все инварианты выполнены.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --script-workflow-smoke ...`.
- Тест подтверждает создание, открытие, редактирование и сохранение C# script через встроенную модель.
- Тест подтверждает attach script к node в saved scene file.
- Тест подтверждает, что compiler error содержит file, line, column, code и message.
- Тест подтверждает successful rebuild после исправления script.
- Тест подтверждает запуск проекта после rebuild.
- Документация реализации описывает workflow и ограничения.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-UiPublicApiGate.ps1 -WikiPath .github\wiki` проходит перед editor work.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.
