# CharacterBody2D kinematic solver baseline

## Цель

`0.1.0 Preview` должен предоставить `CharacterBody2D` для управляемых персонажей и платформенных контроллеров. API должен следовать выбранной форме API Godot 4: `CharacterBody2D` наследуется от `PhysicsBody2D`, хранит `Velocity`, а `MoveAndSlide()` двигает тело на основе этой скорости. `MoveAndCollide()` доступен на `PhysicsBody2D` и возвращает `KinematicCollision2D`.

Источник формы API: официальные документы Godot описывают `CharacterBody2D` как тело для персонажей, которое не двигается физикой само, но двигается скриптом через `move_and_collide()` и `move_and_slide()`; `move_and_slide()` использует свойство `velocity`, а `move_and_collide()` возвращает `KinematicCollision2D` с данными столкновения.

## Публичный API

Добавляются публичные типы:

- `CharacterBody2D : PhysicsBody2D`;
- `KinematicCollision2D : RefCounted`.

`PhysicsBody2D` получает:

- `MoveAndCollide(Vector2 motion, bool testOnly = false, float safeMargin = 0.08f, bool recoveryAsCollision = false): KinematicCollision2D?`.

`CharacterBody2D` получает свойства:

- `Velocity: Vector2`;
- `UpDirection: Vector2`, default `Vector2.Up`;
- `FloorSnapLength: float`, default `1.0`;
- `FloorMaxAngle: float`, default `Mathf.Pi / 4`;
- `FloorStopOnSlope: bool`, default `true`;
- `FloorConstantSpeed: bool`, default `false`;
- `FloorBlockOnWall: bool`, default `true`;
- `SlideOnCeiling: bool`, default `true`;
- `MaxSlides: int`, default `4`;
- `MotionMode: MotionModeEnum`, default `Grounded`;
- `SafeMargin: float`, default `0.08`;
- `WallMinSlideAngle: float`, default `Mathf.Pi / 12`;
- `PlatformFloorLayers: uint`, default `uint.MaxValue`;
- `PlatformWallLayers: uint`, default `0`;
- `PlatformOnLeave: PlatformOnLeaveEnum`, default `AddVelocity`.

`CharacterBody2D` получает методы:

- `MoveAndSlide(): bool`;
- `ApplyFloorSnap(): void`;
- `GetFloorAngle(Vector2? upDirection = null): float`;
- `GetFloorNormal(): Vector2`;
- `GetLastMotion(): Vector2`;
- `GetLastSlideCollision(): KinematicCollision2D?`;
- `GetPlatformVelocity(): Vector2`;
- `GetPositionDelta(): Vector2`;
- `GetRealVelocity(): Vector2`;
- `GetSlideCollision(int slideIdx): KinematicCollision2D?`;
- `GetSlideCollisionCount(): int`;
- `GetWallNormal(): Vector2`;
- `IsOnCeiling(): bool`;
- `IsOnCeilingOnly(): bool`;
- `IsOnFloor(): bool`;
- `IsOnFloorOnly(): bool`;
- `IsOnWall(): bool`;
- `IsOnWallOnly(): bool`.

`KinematicCollision2D` получает методы:

- `GetAngle(Vector2? upDirection = null): float`;
- `GetCollider(): Object?`;
- `GetColliderId(): long`;
- `GetColliderRid(): Rid`;
- `GetColliderShape(): Object?`;
- `GetColliderShapeIndex(): int`;
- `GetColliderVelocity(): Vector2`;
- `GetDepth(): float`;
- `GetLocalShape(): Object?`;
- `GetNormal(): Vector2`;
- `GetPosition(): Vector2`;
- `GetRemainder(): Vector2`;
- `GetTravel(): Vector2`.

## Solver baseline

Текущий solver остаётся managed AABB baseline:

- участвуют active `CollisionShape2D` с `Shape != null` и `Disabled == false`;
- moving body проверяется against `StaticBody2D`;
- `CollisionMask` moving body должен пересекаться с target `CollisionLayer`;
- `safeMargin` расширяет target bounds перед sweep;
- `testOnly == true` возвращает would-be collision без изменения позиции;
- `MoveAndCollide()` останавливает body на ближайшем collision point и возвращает `KinematicCollision2D`;
- `MoveAndSlide()` выполняет до `MaxSlides` итераций, скользит остаточным движением вдоль collision normal и обновляет `Velocity`;
- `FloorMaxAngle` и `UpDirection` классифицируют floor/wall/ceiling;
- `FloorSnapLength` делает дополнительную проверку вдоль направления, противоположного `UpDirection`, если body не движется вверх;
- `StaticBody2D.ConstantLinearVelocity` записывается как platform velocity для floor collisions, если `StaticBody2D.CollisionLayer` попадает в `PlatformFloorLayers`.

Для slope baseline `SegmentShape2D` использует нормаль сегмента, выбранную против направления движения. Остальные shapes используют AABB normal.

## Не входит в задачу

- production Box2D.NET solver;
- rigid-rigid и character-character push;
- точный narrow-phase по всем shape типам;
- полноценный floor stop/constant speed на сложных slopes;
- `AnimatableBody2D`;
- collision exceptions и `TestMove()`.

## Критерии приёмки

- `MoveAndCollide()` останавливает `CharacterBody2D` перед `StaticBody2D`, возвращает collider, RID, normal, travel и remainder.
- `MoveAndCollide(testOnly: true)` возвращает collision без изменения позиции.
- `MoveAndSlide()` обновляет floor/wall/ceiling state, `Velocity`, slide collisions, last motion, position delta и real velocity.
- `FloorSnapLength` удерживает character на floor при горизонтальном движении.
- `PlatformFloorLayers` и `StaticBody2D.ConstantLinearVelocity` попадают в `GetPlatformVelocity()`.
- Slope floor case покрыт integration test через `SegmentShape2D`.
- Public API guard, GitHub Wiki API source, implementation docs, release-facing документы и full test runner синхронизированы.
