namespace Electron2D;

public abstract partial class Font
{
    [ThreadStatic] private static int _callbackGroupDepth;
    internal readonly struct GroupRead(TextServerManager.ConsumerRead provider, bool callbacks) : IDisposable
    {
        public void Dispose() { if (callbacks) _callbackGroupDepth--; provider.Dispose(); }
    }
    internal static GroupRead BeginReadGroup(List<Font> fonts, Font? root = null)
    {
        TextServer? owner = null; var callbacks = false;
        if (root is not null) Include(root);
        foreach (var font in fonts) if (!ReferenceEquals(font, root)) Include(font);
        var provider = TextServerManager.BeginConsumerRead(owner);
        callbacks |= provider.Provider.UsesCallbackConsumers;
        if (callbacks) _callbackGroupDepth++;
        return new(provider, callbacks);
        void Include(Font font)
        {
            var candidate = font.PrepareCallbackGraph();
            if (candidate is not null)
            {
                if (owner is not null && !ReferenceEquals(owner, candidate)) throw new InvalidOperationException("A text operation cannot combine opaque fonts from different implementations.");
                owner = candidate; callbacks = true;
            }
            callbacks |= !font._engineType;
        }
    }
}
