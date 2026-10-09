namespace Electron2D;

internal static class PhysicsReplayCopy
{
    internal static void Require(bool value)
    { if (!value) throw new InvalidOperationException("Physics replay requires the captured object identities and authored configuration."); }
    internal static void Buffer<T>(ReadOnlySpan<T> source, ref T[] destination)
    { if (destination.Length < source.Length) Array.Resize(ref destination, source.Length); source.CopyTo(destination); }
    internal static void List<T>(List<T> source, List<T> destination)
    { destination.Clear(); destination.EnsureCapacity(source.Count); foreach (var item in source) destination.Add(item); }
    internal static void Map<K, V>(Dictionary<K, V> source, Dictionary<K, V> destination) where K : notnull
    { destination.Clear(); destination.EnsureCapacity(source.Count); foreach (var item in source) destination.Add(item.Key, item.Value); }
}
