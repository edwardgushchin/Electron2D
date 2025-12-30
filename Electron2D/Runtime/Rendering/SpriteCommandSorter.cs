namespace Electron2D;

internal static class SpriteCommandSorter
{
    public static void Sort(Span<SpriteCommand> span)
    {
        // Небольшой, без-аллоцирующий quicksort по StableKey.
        // Для типичных 2D очередей это достаточно. При желании — заменить на introsort.
        QuickSort(span, 0, span.Length - 1);
    }

    private static void QuickSort(Span<SpriteCommand> a, int lo, int hi)
    {
        while (lo < hi)
        {
            int i = lo, j = hi;
            var pivot = a[lo + ((hi - lo) >> 1)].StableKey;

            while (i <= j)
            {
                while (a[i].StableKey < pivot) i++;
                while (a[j].StableKey > pivot) j--;

                if (i > j) continue;
                (a[i], a[j]) = (a[j], a[i]);
                i++; j--;
            }

            // Tail recursion elimination: сортируем меньший диапазон рекурсивно.
            if (j - lo < hi - i)
            {
                if (lo < j) QuickSort(a, lo, j);
                lo = i;
            }
            else
            {
                if (i < hi) QuickSort(a, i, hi);
                hi = j;
            }
        }
    }
}