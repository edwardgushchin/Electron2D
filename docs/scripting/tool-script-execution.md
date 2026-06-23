# Безопасное editor-time выполнение `[Tool]` scripts

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1.0 Preview` должен иметь проверяемый внутренний механизм, который позволяет tooling/editor-слою явно выполнить callback script class, помеченного `[Tool]`, без смешивания этого пути с обычным runtime traversal и без динамической загрузки пользовательских assemblies.

Эта задача не реализует GUI редактора, hot reload, compile pipeline, external IDE workflow, загрузку assemblies по path, source generation или полноценный sandbox process. Она фиксирует минимальный execution contract, на который будущий editor host сможет опереться.

## Контракт выполнения

- Tool execution host является internal runtime/tooling механизмом и не добавляет новый публичный API.
- Host принимает уже созданный `Node` instance и использует только явно зарегистрированную `ScriptObjectTypeMetadata`.
- Host не принимает file path, project path, assembly path, raw source code или assembly bytes.
- Host не загружает assemblies и не вызывает runtime compilation.
- Script без зарегистрированной metadata не выполняется.
- Script с metadata, где `IsTool == false`, не выполняется.
- Script с metadata, где `IsToolExecutionSandboxed == false`, не выполняется.
- `[Tool]` script выполняется только через явный вызов host; обычный `SceneTree` runtime traversal не включает editor-time host автоматически.

## Поддержанные callbacks

Минимальный host должен уметь выполнить:

- `_EnterTree()`;
- `_Ready()`;
- `_Process(double delta)`;
- `_PhysicsProcess(double delta)`;
- `_ExitTree()`.

`delta` должен быть finite и неотрицательным для process callbacks.

## Result model

Host возвращает structured result, а не пробрасывает user exception наружу.

Минимальные статусы:

- `Executed` - callback выполнен без exception;
- `MissingMetadata` - metadata для типа не зарегистрирована;
- `NotToolScript` - metadata зарегистрирована, но script не помечен как tool;
- `NotSandboxed` - metadata не разрешает sandboxed tool execution;
- `ExceptionIsolated` - callback выбросил exception, exception сохранён в result.

Result должен содержать:

- status;
- executed/skipped flag;
- callback name;
- script type;
- exception, если она была изолирована.

## Runtime/editor separation

Editor-time execution host не должен:

- вызываться из `SceneTree.ProcessFrame()` или `SceneTree.PhysicsFrame()`;
- менять node tree traversal order;
- автоматически добавлять node в `SceneTree`;
- выполнять callbacks для любого script class только из-за наличия `[Tool]` attribute без registered metadata.

Runtime остаётся обычным path: если пользователь добавляет script node в `SceneTree`, lifecycle callbacks выполняются стандартным traversal как раньше. Editor-time host - отдельный явный путь для tooling.

## Platform safety

Для iOS и других AOT/export paths baseline должен быть fail-closed:

- нет обязательной dynamic assembly load;
- нет обязательной runtime compilation;
- нет API, который принимает path/source/bytes и запускает их как script;
- host работает только с уже созданным managed object и registered typed metadata.

## Acceptance Criteria

- Specification описывает editor/runtime separation, exception isolation и запрет dynamic load path.
- Integration tests подтверждают, что host выполняет только registered tool metadata.
- Integration tests подтверждают, что non-tool и missing metadata scripts не выполняются.
- Integration tests подтверждают, что exception из callback возвращается как result и не ломает следующий запуск host.
- Integration tests подтверждают, что host не предоставляет dynamic assembly loading capability.
- Implementation documentation добавлена в `docs/scripting/`.
- Public documentation в затронутых scripting docs не использует запрещённые публичные формулировки.

## Фактическое состояние, ограничения и проверки

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
