# RandomNumberGenerator

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация.
Задача: `T-0018`.
Обновлено: 2026-06-21.

## Цель

Ввести Electron2D `RandomNumberGenerator` для `0.1-preview`: отдельный объект генератора псевдослучайных чисел с воспроизводимым `Seed`, сохраняемым `State`, integer/float range API и нормальным распределением.

Контракт сверяется с официальной документацией Godot `RandomNumberGenerator`: класс имеет `seed`, `state`, `randf()`, `randf_range()`, `randfn()`, `randi()`, `randi_range()` и `randomize()`, а Godot указывает PCG32 как текущий алгоритм, но предупреждает, что сам алгоритм является implementation detail. В Electron2D `0.1-preview` алгоритм фиксируется как часть preview-контракта, потому что release gate требует воспроизводимые deterministic tests.

Источник Electron2D поверхности: [Godot RandomNumberGenerator](https://docs.godotengine.org/en/stable/classes/class_randomnumbergenerator.html).

## Публичный API

Тип находится в namespace `Electron2D` и наследуется от `RefCounted`.

Публичная поверхность:

```csharp
public class RandomNumberGenerator : RefCounted
{
    public ulong Seed { get; set; }
    public ulong State { get; set; }

    public void Randomize();
    public uint Randi();
    public int RandiRange(int from, int to);
    public float Randf();
    public float RandfRange(float from, float to);
    public float Randfn(float mean = 0f, float deviation = 1f);
}
```

Запрещено добавлять Unity-like aliases (`Next`, `NextFloat`, `Range`, `Random`) и публичные свойства, которых нет в согласованной Electron2D поверхности.

`RandWeighted()` не входит в `T-0018`: weights API должен вводиться отдельной задачей с тестами.

## Seed и State

- `Seed` задаёт исходный seed и переинициализирует внутренний state.
- Один и тот же `Seed` обязан давать одну и ту же последовательность на всех поддерживаемых платформах.
- `State` возвращает текущий внутренний state.
- Сохранённый `State` можно восстановить, чтобы повторить продолжение последовательности.
- `State` не предназначен для произвольных внешних значений; для внешнего input нужно использовать `Seed`.
- `Randomize()` выбирает новый seed из системного источника случайности и применяет его через `Seed`.

## Алгоритм

`0.1-preview` использует PCG32 с 64-битным state, multiplier `6364136223846793005` и increment `1442695040888963407`.

Seed initialization:

1. `state = 0`;
2. выполнить один PCG step;
3. `state += seed`;
4. выполнить второй PCG step.

Для `Seed = 42` первые значения `Randi()` обязаны быть:

```text
3270867926
1795671209
1924641435
1143034755
4121910957
```

## Range API

- `RandiRange(from, to)` возвращает integer в inclusive range.
- Если `from > to`, границы меняются местами.
- `RandiRange(x, x)` возвращает `x`.
- Диапазон должен работать для отрицательных границ и полного `int` range.
- `Randf()` возвращает finite float в inclusive range `[0.0, 1.0]`.
- `RandfRange(from, to)` возвращает finite float в inclusive range; при `from > to` границы меняются местами.
- `RandfRange(x, x)` возвращает `x`.

## Normal distribution

`Randfn(mean = 0f, deviation = 1f)` использует Box-Muller transform поверх текущей последовательности `Randf()`.

- Возврат должен быть finite.
- Один и тот же `Seed` должен давать один и тот же `Randfn()` результат.
- `deviation = 0` возвращает `mean`.

## Acceptance tests

- `Seed` replay фиксирует известную PCG32 последовательность.
- Сохранение и восстановление `State` повторяет continuation.
- `RandiRange()` и `RandfRange()` покрыты обычными, reversed и single-value границами.
- `Randf()` и `Randfn()` детерминированы для одинакового `Seed`.
- Public API compatibility table и runtime baseline test обновлены под новый тип.

## Фактическое состояние, ограничения и проверки

Статус: реализовано для `T-0018`.
Обновлено: 2026-06-21.

## Публичный тип

`Electron2D.RandomNumberGenerator` - Electron2D генератор псевдослучайных чисел для воспроизводимых gameplay-сценариев, тестов и будущих tools.

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

`0.1-preview` фиксирует PCG32 с 64-битным state, multiplier `6364136223846793005` и increment `1442695040888963407`.

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
- Алгоритм фиксирован для `0.1-preview`; изменение последовательности требует отдельной breaking-change записи.
- `State` предназначен для сохранения значения, прочитанного из `State`; для пользовательского input нужно использовать `Seed`.
