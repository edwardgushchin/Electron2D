namespace Electron2D;

/// <summary>
/// Сортировка команд спрайтов по стабильному ключу (<see cref="SpriteCommand.StableKey"/>).
/// </summary>
internal static class SpriteCommandSorter
{
    #region Public API

    /// <summary>
    /// Сортирует команды по <see cref="SpriteCommand.StableKey"/> по возрастанию.
    /// </summary>
    /// <param name="commands">Срез команд для сортировки (in-place, без аллокаций).</param>
    public static void Sort(Span<SpriteCommand> commands)
    {
        // Быстрая in-place сортировка по 64-битному ключу.
        // Для типичных 2D очередей этого достаточно. При желании можно заменить на introsort,
        // но текущая реализация минимальна и без аллокаций.
        if (commands.Length <= 1)
            return;

        QuickSort(commands, 0, commands.Length - 1);
    }

    #endregion

    #region Private helpers

    private static void QuickSort(Span<SpriteCommand> commands, int lo, int hi)
    {
        // Итеративная форма с элиминацией хвостовой рекурсии:
        // рекурсивно сортируем только меньший поддиапазон, чтобы ограничить глубину стека.
        while (lo < hi)
        {
            var i = lo;
            var j = hi;
            var pivotKey = commands[lo + ((hi - lo) >> 1)].StableKey;

            while (i <= j)
            {
                while (commands[i].StableKey < pivotKey) i++;
                while (commands[j].StableKey > pivotKey) j--;

                if (i > j)
                    continue;

                // Swap без аллокаций (ValueTuple-свап JIT обычно оптимизирует хорошо).
                (commands[i], commands[j]) = (commands[j], commands[i]);
                i++;
                j--;
            }

            // Сортируем меньший диапазон рекурсивно, больший — продолжаем в цикле.
            if ((j - lo) < (hi - i))
            {
                if (lo < j)
                    QuickSort(commands, lo, j);

                lo = i;
            }
            else
            {
                if (i < hi)
                    QuickSort(commands, i, hi);

                hi = j;
            }
        }
    }

    #endregion
}
