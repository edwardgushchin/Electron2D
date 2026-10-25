using Electron2D.Editor;

internal static class ProjectionTests
{
    internal static void Run()
    {
        Check("a\r\nb\rc\nd", "a\nb\nc\nd", "a\nbX\nc\nd", "a\r\nbX\rc\nd");
        Check("a\r\nb", "a\nb", "a\nx\nb", "a\r\nx\r\nb");
        Check("🙂\r\nend", "🙂\nend", "🙃\nend", "🙃\r\nend");
        Check("a\rb", "a\nb", "a\nb\nc", "a\rb\rc");
        Check("", "", "x\ny", "x\ny");
        Check("a\r\nb", "a\nb", "", "");
        Console.WriteLine("PASS 6 source projection edge cases.");
    }
    private static void Check(string original, string baseline, string edited, string expected)
    {
        if (SourceTextProjection.Apply(original, baseline, edited) != expected) throw new InvalidOperationException("Source projection changed an untouched range or line-ending policy.");
    }
}
