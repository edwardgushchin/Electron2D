# Безопасное editor-time выполнение `[Tool]` scripts

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
- Implementation documentation добавлена в `docs/documentation/scripting/`.
- Public documentation в затронутых scripting docs не использует запрещённые публичные формулировки.
