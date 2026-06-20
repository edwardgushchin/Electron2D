# 2D math baseline

Статус: целевая спецификация.
Задача: `T-0017`.
Обновлено: 2026-06-21.

## Цель

Ввести закрытый 2D math baseline для `0.1.0 Preview`: `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Transform2D`, `Color` и `Mathf`. Эти типы нужны будущим `Node2D`, rendering/physics API, сериализации ресурсов и `Variant`.

Контракт сверяется с официальной документацией Godot по встроенным 2D-типам:

- `Vector2` и `Vector2I`: координаты, направления, длина, расстояние и компонентные операции.
- `Rect2` и `Rect2I`: прямоугольник через `Position`/`Size`, `End`, площадь, пересечения и нормализация отрицательного размера через `Abs()`.
- `Transform2D`: две оси basis и `Origin`, identity/flip constants и преобразование точек.
- `Color`: RGBA-компоненты, приближённое сравнение, interpolation и HTML-представление.
- `Mathf`: базовые числовые helpers, нужные math-типам.

## Публичный API

Все новые типы находятся в namespace `Electron2D`.

Публичный API должен использовать C#-стиль имён Godot-подобного API: `X`, `Y`, `Position`, `Size`, `Origin`, `Length()`, `LengthSquared()`, `Normalized()`, `Dot()`, `Cross()`, `DistanceTo()`, `Lerp()`, `IsEqualApprox()`, `HasPoint()`, `Intersects()`, `Xform()`, `ToHtml()` и аналогичные PascalCase-члены.

Запрещено добавлять Unity-like имена (`Magnitude`, `SqrMagnitude`, `normalized`, `Bounds`, `Matrix2D`, `Quaternion`) и любые 3D-типы в рамках этой задачи.

## `Vector2`

`Vector2` - value type с компонентами `float X` и `float Y`.

Минимальный контракт:

- constants: `Zero`, `One`, `Inf`, `Left`, `Right`, `Up`, `Down`;
- arithmetic operators: `+`, `-`, unary `-`, component-wise `*`/`/`, scalar `*`/`/`;
- exact equality operators и `Equals()` для value semantics;
- `Length()`, `LengthSquared()`, `Normalized()`, `IsNormalized()`;
- `Dot()`, `Cross()`, `Angle()`, `AngleTo()`, `DistanceTo()`, `DistanceSquaredTo()`, `DirectionTo()`;
- `Lerp()`, `Rotated()`, `Abs()`, `Floor()`, `Ceil()`, `Round()`, `Sign()`, `Min()`, `Max()`, `Clamp()`;
- `IsEqualApprox()`, `IsZeroApprox()`, `IsFinite()`;
- `ToString()` использует invariant culture, чтобы вывод не зависел от локали ОС.

`Normalized()` для zero vector возвращает `Vector2.Zero`.

## `Vector2I`

`Vector2I` - value type с компонентами `int X` и `int Y`.

Минимальный контракт:

- constants: `Zero`, `One`, `Left`, `Right`, `Up`, `Down`;
- arithmetic operators: `+`, `-`, unary `-`, component-wise `*`/`/`, scalar `*`/`/`, remainder `%`;
- exact equality operators и `Equals()`;
- `Length()`, `LengthSquared()`, `Aspect()`;
- `Abs()`, `Sign()`, `Min()`, `Max()`, `Clamp()`;
- явное преобразование из `Vector2` с усечением к `int`;
- неявное преобразование в `Vector2`;
- `ToString()` использует invariant culture.

## `Rect2` и `Rect2I`

`Rect2` использует `Vector2 Position` и `Vector2 Size`.
`Rect2I` использует `Vector2I Position` и `Vector2I Size`.

Минимальный контракт для обоих типов:

- constructors по `position/size` и по `x/y/width/height`;
- `End` возвращает `Position + Size`; setter меняет `Size`;
- `GetArea()`, `GetCenter()`, `HasArea()`;
- `Abs()` возвращает прямоугольник с неотрицательным размером;
- `HasPoint()`, `Encloses()`, `Intersects()`, `Intersection()`, `Merge()`, `Expand()`, `Grow()`;
- exact equality operators и `IsEqualApprox()` для `Rect2`;
- `ToString()` использует invariant culture.

Отрицательный размер не запрещён при создании, но большинство геометрических методов ожидают нормальный прямоугольник. Для нормализации вызывается `Abs()`.

## `Transform2D`

`Transform2D` - value type с basis-осями `Vector2 X`, `Vector2 Y` и translation `Vector2 Origin`.

Минимальный контракт:

- constants: `Identity`, `FlipX`, `FlipY`;
- constructors по `xAxis/yAxis/origin`, по `rotation/origin`, по шести float-компонентам;
- `Xform(Vector2)`, `BasisXform(Vector2)`;
- `Determinant()`, `AffineInverse()`, `Inverse()`;
- `Translated()`, `Scaled()`, `Rotated()`;
- transform/vector и transform/transform operators;
- exact equality operators, `IsEqualApprox()`, `IsFinite()`;
- `ToString()` использует invariant culture.

## `Color`

`Color` - value type с компонентами `float R`, `G`, `B`, `A`.

Минимальный контракт:

- constructor `Color(float r, float g, float b, float a = 1f)`;
- constants: `Black`, `White`, `Transparent`;
- component-wise arithmetic operators и scalar `*`/`/`;
- exact equality operators и `IsEqualApprox()`;
- `Lerp()`, `Clamp()`, `Lightened()`, `Darkened()`;
- `ToHtml(bool includeAlpha = true)` и `FromHtml(string html)`;
- `ToString()` использует invariant culture.

`FromHtml()` принимает `RRGGBB`, `RRGGBBAA`, `#RRGGBB` и `#RRGGBBAA`; остальные форматы дают `FormatException`.

## `Mathf`

`Mathf` - static class для базовых Godot-like math helpers:

- constants: `Pi`, `Tau`, `E`, `Epsilon`;
- `IsEqualApprox()`, `IsZeroApprox()`, `IsFinite()`;
- `Clamp()`, `Lerp()`, `InverseLerp()`, `MoveToward()`;
- `DegToRad()`, `RadToDeg()`;
- `PosMod()`, `Snapped()`;
- `FloorToInt()`, `CeilToInt()`, `RoundToInt()`.

## Acceptance tests

- `Vector2` покрыт арифметикой, длиной, нормализацией, dot/cross, расстоянием, rotation, interpolation, invariant formatting и zero-vector edge case.
- `Vector2I` покрыт integer arithmetic, length/aspect, component helpers и conversions.
- `Rect2` и `Rect2I` покрыты `End`, area/center, `Abs()`, point containment, intersections, merge и grow.
- `Transform2D` покрыт identity, translation, rotation, scale, inverse и transform composition.
- `Color` и `Mathf` покрыты approximate equality, clamp, interpolation, angle conversion, positive modulo, snapping, HTML formatting/parsing и invalid HTML error.
- Public API compatibility table и runtime baseline test обновлены под новые типы.
