# RandomNumberGenerator

Статус: реализовано для `T-0018`.
Обновлено: 2026-06-21.

## Публичный тип

`Electron2D.RandomNumberGenerator` - Godot-like генератор псевдослучайных чисел для воспроизводимых gameplay-сценариев, тестов и будущих tools.

Тип наследуется от `RefCounted` и находится в namespace `Electron2D`.

## Публичный API

Реализовано:

- `ulong Seed { get; set; }`;
- `ulong State { get; set; }`;
- `void Randomize()`;
- `uint Randi()`;
- `int RandiRange(int from, int to)`;
- `float Randf()`;
- `float RandfRange(float from, float to)`;
- `float Randfn(float mean = 0f, float deviation = 1f)`.

Unity-like aliases (`Next`, `NextFloat`, `Range`) не добавлены.

## Seed и State

`Seed` задаёт исходный seed и переинициализирует последовательность. Один и тот же `Seed` даёт одну и ту же последовательность на поддерживаемых платформах.

`State` хранит текущее внутреннее состояние. Его можно сохранить и восстановить, чтобы повторить продолжение последовательности после уже выданных значений.

`Randomize()` выбирает новый seed из системного источника случайности и применяет его через `Seed`.

## Алгоритм

`0.1.0 Preview` фиксирует PCG32 с 64-битным state, multiplier `6364136223846793005` и increment `1442695040888963407`.

Для `Seed = 42` первые значения `Randi()`:

```text
3270867926
1795671209
1924641435
1143034755
4121910957
```

Эта последовательность покрыта unit-тестом и считается частью preview-контракта.

## Range API

`RandiRange(from, to)` возвращает integer в inclusive range. Если `from > to`, границы меняются местами. Одинаковые границы возвращают это значение.

`Randf()` возвращает finite float в inclusive range `[0.0, 1.0]`.

`RandfRange(from, to)` возвращает finite float в inclusive range и также поддерживает reversed bounds.

`Randfn(mean, deviation)` использует Box-Muller transform. При `deviation = 0` метод возвращает `mean`.

## Ограничения

- `RandWeighted()` не реализован в `T-0018`: weights API должен вводиться отдельной задачей с тестами.
- Алгоритм фиксирован для `0.1.0 Preview`; изменение последовательности требует отдельной breaking-change записи.
- `State` предназначен для сохранения значения, прочитанного из `State`; для пользовательского input нужно использовать `Seed`.
