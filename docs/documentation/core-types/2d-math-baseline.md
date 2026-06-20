# 2D math baseline

Статус: реализовано для `T-0017`.
Обновлено: 2026-06-21.

## Публичные типы

В `Electron2D` добавлен первый Godot-like набор 2D math типов:

- `Vector2`;
- `Vector2I`;
- `Rect2`;
- `Rect2I`;
- `Transform2D`;
- `Color`;
- `Mathf`.

Эти типы являются базой для будущих `Node2D`, rendering, physics, scene serialization и `Variant`. Они не добавляют 3D API и не возвращают Unity-like aliases.

## `Vector2`

`Vector2` хранит `float X` и `float Y`.

Реализовано:

- constants `Zero`, `One`, `Inf`, `Left`, `Right`, `Up`, `Down`;
- arithmetic operators для vector/vector и vector/scalar операций;
- exact equality и `IsEqualApprox()`;
- `Length()`, `LengthSquared()`, `Normalized()`, `IsNormalized()`;
- `Dot()`, `Cross()`, `Angle()`, `AngleTo()`, `DistanceTo()`, `DistanceSquaredTo()`, `DirectionTo()`;
- `Lerp()`, `Rotated()`, `Abs()`, `Floor()`, `Ceil()`, `Round()`, `Sign()`, `Min()`, `Max()`, `Clamp()`;
- `IsZeroApprox()`, `IsFinite()`;
- invariant `ToString()`.

`Vector2.Zero.Normalized()` возвращает `Vector2.Zero`.

## `Vector2I`

`Vector2I` хранит `int X` и `int Y`.

Реализовано:

- constants `Zero`, `One`, `Left`, `Right`, `Up`, `Down`;
- integer arithmetic operators, включая `%`;
- exact equality;
- `Length()`, `LengthSquared()`, `Aspect()`;
- `Abs()`, `Sign()`, `Min()`, `Max()`, `Clamp()`;
- implicit conversion в `Vector2`;
- explicit conversion из `Vector2` с усечением к `int`;
- invariant `ToString()`.

## `Rect2` и `Rect2I`

`Rect2` хранит `Vector2 Position` и `Vector2 Size`.
`Rect2I` хранит `Vector2I Position` и `Vector2I Size`.

Реализовано:

- constructors по position/size и x/y/width/height;
- `End` getter/setter;
- `GetArea()`, `GetCenter()`, `HasArea()`;
- `Abs()` для нормализации отрицательного размера;
- `HasPoint()`, `Encloses()`, `Intersects()`, `Intersection()`, `Merge()`, `Expand()`, `Grow()`;
- exact equality;
- `Rect2.IsEqualApprox()`;
- invariant `ToString()`.

Создание прямоугольника с отрицательным размером не запрещено. Для геометрических операций с таким прямоугольником нужно явно вызвать `Abs()`.

## `Transform2D`

`Transform2D` хранит basis axes `Vector2 X`, `Vector2 Y` и translation `Vector2 Origin`.

Реализовано:

- constants `Identity`, `FlipX`, `FlipY`;
- constructors по axes/origin, rotation/origin и шести float-компонентам;
- `Xform()`, `BasisXform()`;
- `Determinant()`, `AffineInverse()`, `Inverse()`;
- `Translated()`, `Scaled()`, `Rotated()`;
- transform/vector, transform/rect и transform/transform operators;
- exact equality, `IsEqualApprox()`, `IsFinite()`;
- invariant `ToString()`.

`AffineInverse()` и `Inverse()` бросают `InvalidOperationException`, если basis не обратим.

## `Color`

`Color` хранит `float R`, `G`, `B`, `A`.

Реализовано:

- constructor `Color(float r, float g, float b, float a = 1f)`;
- constants `Black`, `White`, `Transparent`;
- component-wise arithmetic operators и scalar operators;
- exact equality и `IsEqualApprox()`;
- `Lerp()`, `Clamp()`, `Lightened()`, `Darkened()`;
- `ToHtml(bool includeAlpha = true)`;
- `FromHtml(string html)`;
- invariant `ToString()`.

`FromHtml()` принимает `RRGGBB`, `RRGGBBAA`, `#RRGGBB` и `#RRGGBBAA`. Другие форматы дают `FormatException`.

## `Mathf`

`Mathf` содержит базовые Godot-like helpers:

- constants `Pi`, `Tau`, `E`, `Epsilon`;
- `IsEqualApprox()`, `IsZeroApprox()`, `IsFinite()`;
- `Clamp()`, `Lerp()`, `InverseLerp()`, `MoveToward()`;
- `DegToRad()`, `RadToDeg()`;
- `PosMod()`, `Snapped()`;
- `FloorToInt()`, `CeilToInt()`, `RoundToInt()`.

## Ограничения

- `Variant` integration не входит в `T-0017`.
- `StringName` и `Rid` реализуются отдельными задачами.
- 3D math-типы не входят в `0.1.0 Preview` math baseline.
- Полный XML documentation gate остаётся задачей `T-0106`.

## Размещение файлов

Публичный namespace всех runtime-типов остаётся `Electron2D`, но исходные файлы разнесены по доменным папкам:

- `src/Electron2D/Core/Math/` - math value types и helpers;
- `src/Electron2D/Core/ObjectModel/` - базовая object/resource/callable модель;
- `src/Electron2D/Core/SceneTree/` - node tree, scene resources и внутренние механизмы обхода.
