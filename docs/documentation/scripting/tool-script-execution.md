# Безопасное editor-time выполнение `[Tool]` scripts

## Текущее состояние

`0.1.0 Preview` теперь содержит внутренний `ToolScriptExecutionHost` для явного выполнения callbacks у script classes, помеченных `[Tool]` и зарегистрированных в `ScriptObjectMetadataRegistry`.

Host не является публичным API. Он предназначен для будущего editor/tooling layer и тестируется как internal runtime contract.

## Execution contract

Host принимает уже созданный `Node` instance и callback kind:

- `EnterTree` вызывает `_EnterTree()`;
- `Ready` вызывает `_Ready()`;
- `Process` вызывает `_Process(delta)`;
- `PhysicsProcess` вызывает `_PhysicsProcess(delta)`;
- `ExitTree` вызывает `_ExitTree()`.

Выполнение разрешено только если:

- для script type зарегистрирована `ScriptObjectTypeMetadata`;
- metadata имеет `IsTool == true`;
- metadata имеет `IsToolExecutionSandboxed == true`.

Если metadata отсутствует или script не является tool script, callback не вызывается, а host возвращает structured result со статусом skip.

## Result model

`ToolScriptExecutionResult` возвращает:

- `Status`;
- `Executed`;
- `Callback`;
- `ScriptType`;
- `Exception`.

Статусы:

- `Executed`;
- `MissingMetadata`;
- `NotToolScript`;
- `NotSandboxed`;
- `ExceptionIsolated`.

User exception не пробрасывается наружу из host. Exception сохраняется в result, callback считается неуспешным, а следующий explicit host call может продолжить работу.

## Runtime/editor separation

`SceneTree` не вызывает `ToolScriptExecutionHost`. Обычный runtime traversal не менялся: node lifecycle продолжает идти через `SceneTree`.

Editor-time выполнение запускается только явным вызовом host. Host не добавляет node в tree, не меняет traversal order и не запускает script только по наличию attribute без registered metadata.

## Dynamic load policy

Host не поддерживает dynamic assembly load:

- `SupportsDynamicAssemblyLoad == false`;
- public methods host не принимают `Assembly`, `AssemblyLoadContext`, `byte[]` или path parameter;
- host не компилирует source code и не загружает assemblies.

Это сохраняет AOT/export paths fail-closed: будущий editor/tooling layer должен передать уже созданный managed object и registered typed metadata.

## Проверки

- `ToolScriptExecutionTests.HostExecutesOnlyRegisteredSandboxedToolScripts`
- `ToolScriptExecutionTests.HostIsolatesCallbackExceptionsAndCanContinue`
- `ToolScriptExecutionTests.HostDoesNotExposeDynamicAssemblyLoading`
