# Fixed physics timestep, basic CCD и one-way platform baseline

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
