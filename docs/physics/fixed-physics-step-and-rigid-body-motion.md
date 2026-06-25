# Fixed physics timestep, basic CCD и one-way platform baseline

Обновлено: 2026-06-25.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1.0 Preview` должен иметь предсказуемый начальный цикл физики:

- `SceneTree.PhysicsFrame()` накапливает прошедшее время и запускает физику фиксированными шагами по `1/60` секунды;
- `RigidBody2D` выполняет базовое движение по `LinearVelocity` внутри физического шага;
- быстро движущийся `RigidBody2D` не должен пролетать сквозь `StaticBody2D`, если за один фиксированный шаг пересёк его AABB-границы;
- `CollisionShape2D.OneWayCollision` должен блокировать движение сверху вниз и пропускать движение снизу вверх;
- тело, поставленное в очередь удаления через `QueueFree()` во время физического шага, не должно получать следующие физические шаги в том же `PhysicsFrame()`, но само удаление происходит после завершения прохода дерева.

Задача не выбирает production physics backend, не раскрывает Box2D.NET или другие внутренние handles и не добавляет новый публичный API. Baseline строится поверх существующих Electron2D узлов и AABB-границ активных `CollisionShape2D`.

## Fixed timestep

Правила `SceneTree.PhysicsFrame(double delta)`:

- `delta` добавляется во внутренний накопитель времени;
- пока накопитель содержит минимум `1/60` секунды, запускается один физический шаг;
- в `_PhysicsProcess(double delta)` и во внутренние обработчики физики всегда передаётся `1/60`;
- остаток меньше `1/60` сохраняется до следующего вызова;
- `delta <= 0`, `NaN` и infinity не должны запускать физический шаг.

## Связь с `RuntimeHost`

`SceneTree.PhysicsFrame(double delta)` остаётся самостоятельной защитой для прямых вызовов тестов, инструментов и будущих механизмов запуска: он принимает прошедшее время, хранит свой накопитель и запускает `_PhysicsProcess(double delta)` только фиксированными шагами по `1/60` секунды.

Интерактивный `RuntimeHost` дополнительно имеет внешний планировщик кадра. Он берёт `measuredDeltaTime` из монотонных часов, ограничивает его до `MaxDeltaTime = 0.25` секунды, добавляет результат в свой накопитель и вызывает внутренний `SceneTree.PhysicsFixedStep()` столько раз, сколько помещается в накопителе и не превышает `MaxPhysicsStepsPerFrame = 5` за один отображаемый кадр. Так сохраняется один источник истины для интерактивной физики: реальный шаг остаётся `1/60` секунды внутри `SceneTree`, а внешний планировщик только решает, сколько таких шагов выполнить. В отличие от `SceneTree.PhysicsFrame(double delta)`, прямой шаг не добавляет время во внутренний накопитель дерева и не может породить дополнительный скрытый физический вызов.

Если накопитель интерактивного планировщика после пяти реальных шагов всё ещё содержит полный `SceneTree.FixedPhysicsStep`, runtime отбрасывает остаток и считает его отброшенным или ограниченным временем (`dropped/clamped time`) в диагностике. Это не меняет контракт `SceneTree.PhysicsFrame`: прямой вызывающий код всё ещё может передать `1/30` и получить два фиксированных тика (`fixed ticks`). Ограничение догоняющих шагов находится в `RuntimeHost`, потому что оно относится к интерактивному окну и защищает цикл кадров от бесконечного догоняющего выполнения после просадки.

`_PhysicsProcess(double delta)` всегда получает `SceneTree.FixedPhysicsStep`. `_Process(double delta)` в интерактивном `RuntimeHost` получает отображаемый `deltaTime`, ограниченный `MaxDeltaTime`, а не `FixedDelta`. В ограниченном автоматическом прогоне `RuntimeHost` сохраняет прежнюю детерминированность и передаёт `FixedDelta` в обновление и физику (`process/physics`) без чтения системного времени, чтобы проверяющие скрипты (`verifier`) и screenshot-тесты оставались воспроизводимыми.

## RigidBody2D motion

На каждом фиксированном шаге `RigidBody2D`:

- пропускает движение, если `Freeze == true` или `Sleeping == true`;
- двигается на `LinearVelocity * fixedDelta`;
- применяет `AngularVelocity * fixedDelta`, если `LockRotation == false`;
- проверяет путь движения через AABB sweep against `StaticBody2D`;
- при столкновении ставит позицию на ближайшую безопасную точку контакта и обнуляет компонент скорости по нормали столкновения.

Это не полноценный solver: impulses, mass, restitution, friction, contacts, exact shape narrow phase и взаимодействие rigid-rigid не входят в baseline.

## Basic CCD

Basic CCD здесь означает простую проверку пути тела за один фиксированный шаг, а не дискретную проверку только финальной позиции. Для каждого активного shape движущегося `RigidBody2D` строится AABB, затем он проверяется против AABB активных shapes у `StaticBody2D` в том же `SceneTree`.

Правила:

- self-collision исключён;
- `CollisionMask` moving body должен пересекаться с `CollisionLayer` static body;
- disabled shapes не участвуют;
- queued-for-deletion bodies не участвуют;
- выбирается ближайшее столкновение по доле пройденного пути.

## One-way platforms

Если target `CollisionShape2D.OneWayCollision == true`, baseline блокирует только движение сверху вниз:

- движение должно иметь положительный `Y`;
- нижняя грань moving body до шага должна быть не ниже верхней грани platform shape с учётом `OneWayCollisionMargin`;
- нормаль столкновения должна быть направлена вверх.

Движение снизу вверх и боковое движение через one-way shape не блокируются.

## Deferred body queue

`QueueFree()` во время физического шага помечает узел на удаление. Узел остаётся valid до конца текущего прохода дерева, чтобы текущий traversal завершился безопасно, но следующие fixed substeps внутри того же `PhysicsFrame()` должны пропускать уже queued node и его subtree. Фактическое удаление и освобождение RID выполняются после завершения `PhysicsFrame()`.

## Критерии приёмки

- Integration test подтверждает накопление `1/120 + 1/120` в один physics tick и `1/30` в два fixed ticks.
- Integration test подтверждает, что быстрый `RigidBody2D` останавливается перед `StaticBody2D`, а не оказывается внутри или за препятствием.
- Integration test подтверждает, что one-way platform блокирует падение сверху и пропускает движение снизу.
- Integration test подтверждает, что node, вызвавший `QueueFree()` на первом fixed tick, не получает второй fixed tick в том же `PhysicsFrame()`, а sibling продолжает получать все fixed ticks.
- Public API guard, MIT source header verifier и общий test runner проходят.

## Фактическое состояние, ограничения и проверки

`0.1.0 Preview` теперь выполняет физику фиксированными шагами по `1/60` секунды и имеет начальное движение `RigidBody2D` по `LinearVelocity`.

## Что реализовано

- `SceneTree.PhysicsFrame(double delta)` накапливает время и запускает столько fixed ticks по `1/60`, сколько помещается в накопителе.
- `_PhysicsProcess(double delta)` получает `1/60` на каждом fixed tick.
- Остаток времени меньше `1/60` сохраняется до следующего `PhysicsFrame()`.
- `RigidBody2D` двигается по `LinearVelocity * fixedDelta`.
- `RigidBody2D` применяет `AngularVelocity * fixedDelta`, если `LockRotation == false`.
- `Freeze == true` и `Sleeping == true` останавливают движение тела.
- Быстрое движение `RigidBody2D` проверяется через AABB sweep against `StaticBody2D`, поэтому тело останавливается перед статическим препятствием, если пересекло его за один fixed tick.
- При столкновении компонент `LinearVelocity` по нормали столкновения обнуляется.
- `CollisionShape2D.OneWayCollision` блокирует движение сверху вниз и пропускает движение снизу вверх.
- Узел, вызвавший `QueueFree()` во время fixed tick, остаётся живым до конца текущего обхода дерева, но следующие fixed ticks внутри того же `PhysicsFrame()` его уже пропускают.
- Интерактивный `RuntimeHost` использует отдельный планировщик кадра поверх `SceneTree.PhysicsFrame`: `MaxDeltaTime = 0.25` секунды, `MaxPhysicsStepsPerFrame = 5`, `_Process(deltaTime)` для отображаемого кадра и `_PhysicsProcess(SceneTree.FixedPhysicsStep)` для каждого fixed tick. `RuntimeHostOptions.FixedDelta`, отличный от `1/60`, не меняет интерактивную частоту физики и не создаёт второй накопитель с отдельным размером шага. Отброшенное из-за ограничения догоняющих шагов время попадает в runtime-диагностику.

## Правила collision checks

Baseline использует AABB-границы активных `CollisionShape2D`:

- `Shape != null`;
- `Disabled == false`;
- target body находится внутри `SceneTree`;
- target body не поставлен в очередь удаления;
- moving `RigidBody2D.CollisionMask` пересекается с target `CollisionLayer`;
- self-collision исключён.

Проверка выбирает ближайшее столкновение по пройденной доле движения. Текущий baseline проверяет `RigidBody2D` только against `StaticBody2D`.

## One-way collision

`CollisionShape2D.OneWayCollision` участвует в baseline только для static shapes. Блокировка происходит, если:

- moving body движется вниз, то есть `motion.Y > 0`;
- collision normal направлена вверх;
- нижняя грань moving body до шага находится не ниже верхней грани platform shape с учётом `OneWayCollisionMargin`.

Движение снизу вверх и боковое движение не блокируются one-way shape.

## Ограничения

Это не полноценный physics solver:

- нет impulses, mass solve, restitution и friction combine;
- нет rigid-rigid collision;
- нет точного narrow-phase по реальной форме shape;
- нет contact manifold и shape-level contacts;
- нет gravity integration;
- production kinematic solver narrow-phase остаётся отдельной задачей;
- production Box2D.NET backend пока не выбран.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~FixedPhysicsStepAndRigidBodyMotionTests"
```

Связанная проверка runtime-планировщика и fixed physics:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~RuntimeHostTests|FullyQualifiedName~FixedPhysicsStepAndRigidBodyMotionTests"
```

Последняя проверка `2026-06-25` прошла: 69/69.

Связанный physics/lifecycle набор:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~NodeSceneTreeLifecycleTests|FullyQualifiedName~DeferredCallTests|FullyQualifiedName~CSharpScriptModelTests|FullyQualifiedName~PhysicsNodeLifecycleTests|FullyQualifiedName~PhysicsMaterialStateTests|FullyQualifiedName~Area2DOverlapSignalTests|FullyQualifiedName~PhysicsDirectSpaceState2DTests|FullyQualifiedName~FixedPhysicsStepAndRigidBodyMotionTests"
```

Public API guard:

```powershell
dotnet test tests\Electron2D.Tests.Unit\Electron2D.Tests.Unit.csproj --filter "FullyQualifiedName~CleanRuntimeBaselineTests"
```
