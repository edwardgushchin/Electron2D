namespace Electron2D;

internal static unsafe partial class NativeTextBreak
{
    internal static int[] AccessibilityBoundaries(string text, int[] offsets, int kind, bool ends, string language)
    {
        lock (Gate)
        {
            Initialize(); var iterator = Open(kind, language.Length == 0 ? TranslationServer.GetToolLocale() : language);
            fixed (char* data = text)
            {
                try
                {
                    Check(Set(iterator, data, text.Length)); var previous = First(iterator); var result = new List<int> { 0 };
                    for (var next = Next(iterator); next >= 0; next = Next(iterator))
                    {
                        if (kind != 1 || Status(iterator) >= 100) result.Add(ScalarIndex(offsets, ends ? next : previous));
                        previous = next;
                    }
                    result.Add(offsets.Length - 1); return result.Distinct().Order().ToArray();
                }
                finally { Close(iterator); }
            }
        }
    }
}
