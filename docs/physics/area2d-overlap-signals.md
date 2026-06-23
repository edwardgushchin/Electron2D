# Area2D sensors и overlap signals baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

## Цель

`0.1.0 Preview` должен иметь Electron2D baseline для `Area2D` sensors: область должна находить пересечения с телами и другими областями, хранить текущие overlap lists и эмитить вход/выход через стандартную систему `Object.Connect()`/`EmitSignal()`.

Задача не реализует полноценный solver и не раскрывает backend-типы. Текущий результат нужен как проверяемый managed baseline для editor/runtime gameplay logic, пока production physics backend ещё подключается отдельными задачами.

## Публичный API

`Area2D` сохраняет существующие properties:

- `Monitoring: bool`;
- `Monitorable: bool`;
- `Priority: int`;
- `CollisionLayer`;
- `CollisionMask`.

Добавляются Electron2D methods:

- `GetOverlappingBodies(): Node2D[]`;
- `GetOverlappingAreas(): Area2D[]`;
- `HasOverlappingBodies(): bool`;
- `HasOverlappingAreas(): bool`;
- `OverlapsBody(Node2D body): bool`;
- `OverlapsArea(Area2D area): bool`.

`Area2D` регистрирует built-in signal names:

- `body_entered`, argument: `Node2D body`;
- `body_exited`, argument: `Node2D body`;
- `area_entered`, argument: `Area2D area`;
- `area_exited`, argument: `Area2D area`.

.NET events и backend-specific callbacks не добавляются, чтобы не расширять публичную модель за пределы Electron2D API.

## Overlap baseline

Для `0.1.0 Preview` managed baseline использует AABB-пересечение активных `CollisionShape2D`:

- shape считается активным, если у `CollisionShape2D.Shape` есть concrete `Shape2D` и `Disabled == false`;
- transform берётся из `CollisionShape2D.GlobalTransform`;
- rectangle, circle, capsule, segment, convex polygon и concave segments сводятся к world-space `Rect2`;
- объект без активных shapes не участвует в overlap.

Фильтр слоёв применяет Electron2D правило scanning object:

- `Area2D.CollisionMask` должен пересекаться с `CollisionLayer` target object;
- `Monitoring == false` отключает collection и signals текущей области;
- `Monitorable == false` отключает обнаружение этой области другими monitoring areas;
- `Area2D` не пересекается сама с собой.

## Порядок событий

На каждом `SceneTree.PhysicsFrame()` область сравнивает текущие пересечения с предыдущим снимком:

1. `body_entered` в порядке возрастания `Node.GetInstanceId()`;
2. `body_exited` в порядке возрастания `Node.GetInstanceId()`;
3. `area_entered` в порядке возрастания `Node.GetInstanceId()`;
4. `area_exited` в порядке возрастания `Node.GetInstanceId()`.

Список overlap helpers отражает текущий снимок после обработки frame. Invalid/freed nodes не возвращаются и не получают exit-сигнал после удаления, чтобы signal payload не ссылался на уже освобождённый объект.

## Deferred removal

Если callback `body_entered` или `area_entered` вызывает `QueueFree()` для overlapping node, удаление выполняется через существующую deferred queue `SceneTree`. Объект остаётся валидным внутри callback, удаляется после завершения traversal и не ломает следующий physics frame.

## Не входит в задачу

- точный narrow-phase solver;
- contact manifolds;
- `body_shape_entered`, `body_shape_exited`, `area_shape_entered`, `area_shape_exited`;
- `CharacterBody2D` kinematic solver;
- raycast, point query и shape query;
- Box2D.NET production binding;
- rollback/deterministic physics.

## Критерии приёмки

- `Area2D` имеет Electron2D signal names и overlap helper methods.
- Enter/exit ordering проверен integration tests.
- `Monitoring`, `Monitorable`, collision layer/mask filters проверены.
- `QueueFree()` внутри overlap signal callback не ломает traversal и следующий physics frame.
- Public API guard, GitHub Wiki API source, документация реализации и release-facing документы синхронизированы.
- Focused tests, `Verify-ApiCompatibility.ps1`, `Verify-SourceLicenseHeaders.ps1` и общий test runner проходят.

## Фактическое состояние, ограничения и проверки

`Area2D` теперь имеет начальный sensor baseline для `0.1.0 Preview`: область на physics frame находит пересечения с телами и другими областями, хранит текущие overlap snapshots и эмитит стандартные signal names через `Object.Connect()`.

## Что реализовано

- Built-in signals регистрируются при создании `Area2D`:
  - `body_entered`;
  - `body_exited`;
  - `area_entered`;
  - `area_exited`.
- `GetOverlappingBodies()` возвращает текущие valid body nodes как `Node2D[]`.
- `GetOverlappingAreas()` возвращает текущие valid area nodes как `Area2D[]`.
- `HasOverlappingBodies()` и `HasOverlappingAreas()` проверяют наличие текущих пересечений.
- `OverlapsBody(Node2D body)` и `OverlapsArea(Area2D area)` проверяют конкретный объект.
- `Monitoring == false` очищает текущий snapshot и отключает collection/signals этой области.
- `Monitorable == false` не даёт другим areas обнаружить эту область.
- `CollisionMask` monitoring area должен пересекаться с `CollisionLayer` target object.

## Overlap model

Текущий managed baseline использует AABB-пересечение active `CollisionShape2D`:

- `CollisionShape2D.Disabled == true` исключает shape из проверки;
- `CollisionShape2D.Shape == null` не участвует в overlap;
- rectangle, circle, capsule, segment, convex polygon и concave segments сводятся к world-space `Rect2`;
- `CollisionShape2D.GlobalTransform` учитывает transform shape node и родителей `Node2D`;
- collision object без active shapes не участвует в overlap.

Это не production narrow-phase solver. Baseline нужен, чтобы gameplay-код, editor previews и будущий backend уже использовали стабильный публичный API.

## Порядок сигналов

На каждом `SceneTree.PhysicsFrame()` изменения эмитятся в стабильном порядке:

1. `body_entered`;
2. `body_exited`;
3. `area_entered`;
4. `area_exited`.

Внутри каждой группы nodes сортируются по `GetInstanceId()`. Это даёт повторяемые integration tests и предсказуемое поведение для простых сцен.

## Deferred removal

Если callback overlap-сигнала вызывает `QueueFree()` для body/area, удаление остаётся deferred через `SceneTree`. Объект валиден внутри callback, удаляется после текущего traversal и не ломает следующий physics frame.

Freed nodes не возвращаются из overlap helper methods и не получают exit-сигнал после фактического освобождения, чтобы signal payload не ссылался на уже освобождённый объект.

## Что ещё не реализовано

- точная narrow-phase collision detection;
- contact manifolds;
- shape-level signals `body_shape_entered`, `body_shape_exited`, `area_shape_entered`, `area_shape_exited`;
- точные shape-level query narrow-phase results;
- production continuous collision detection;
- production Box2D.NET backend.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~Area2DOverlapSignalTests"
```

Compatibility verifier:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ApiCompatibility.ps1
```
