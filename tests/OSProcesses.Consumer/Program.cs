if (args.Length > 1 && args[0] == "--os-process-child") OSProcessTests.Child(args);
else OSProcessTests.Run();

internal static class TextureArrayTests
{
    internal static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    internal static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
