namespace Electron2D;

internal static partial class CanvasRasterScale
{
    private static readonly List<WeakReference<TextServer>> Providers = [];
    private static int _notifyingProviders;
    private static void EnsureProviderNotificationsIdle()
    {
        if (_notifyingProviders != 0) throw new InvalidOperationException("Raster scale references cannot change during provider notification.");
    }
    private readonly record struct ProviderNotification(TextServer Provider, TextServerManager.ConsumerReservation Reservation);
    private static ProviderNotification[] BeginProviderNotifications()
    {
        var providers = new List<ProviderNotification>();
        try
        {
            for (var i = Providers.Count - 1; i >= 0; i--)
                if (!Providers[i].TryGetTarget(out var provider)) Providers.RemoveAt(i);
                else providers.Add(new(provider, TextServerManager.ReserveConsumer(provider)));
            var result = providers.ToArray(); _notifyingProviders++; return result;
        }
        catch { for (var i = providers.Count - 1; i >= 0; i--) providers[i].Reservation.Dispose(); throw; }
    }
    private static void EndProviderNotifications(ProviderNotification[]? providers = null)
    {
        lock (ScaleGate)
        { if (providers is not null) for (var i = providers.Length - 1; i >= 0; i--) providers[i].Reservation.Dispose(); _notifyingProviders--; }
    }
    private static void NotifyProvider(TextServer provider, int key, bool reference, bool retiring, ref List<Exception>? errors)
    {
        try { provider.NotifyRasterScale(key / 64f, reference, retiring); }
        catch (Exception error) { Node.CollectException(ref errors, error); }
    }
    internal static void TrackProvider(TextServer provider)
    {
        if (provider is not TextServerExtension) return;
        KeyValuePair<int, ScaleLevel>[] levels;
        using var consumer = TextServerManager.BeginConsumerRead(provider);
        lock (ScaleGate)
        {
            foreach (var weak in Providers) if (weak.TryGetTarget(out var existing) && ReferenceEquals(existing, provider)) return;
            levels = _scales.ToArray(); Providers.Add(new(provider)); _notifyingProviders++;
        }
        List<Exception>? errors = null;
        try
        {
            foreach (var level in levels)
                for (var i = 0; i < level.Value.Count; i++) NotifyProvider(provider, level.Key, true, false, ref errors);
        }
        finally { EndProviderNotifications(); }
        Node.ThrowCollected("Text raster scale subscription committed with callback failures.", errors);
    }
    internal static void ForgetProvider(TextServer provider)
    {
        KeyValuePair<int, ScaleLevel>[] levels;
        lock (ScaleGate)
        {
            var index = Providers.FindIndex(weak => weak.TryGetTarget(out var existing) && ReferenceEquals(existing, provider));
            if (index < 0) return;
            levels = _scales.ToArray(); Providers.RemoveAt(index); _notifyingProviders++;
        }
        List<Exception>? errors = null;
        try
        {
            foreach (var level in levels)
                for (var i = 0; i < level.Value.Count; i++) NotifyProvider(provider, level.Key, false, true, ref errors);
        }
        finally { EndProviderNotifications(); }
        Node.ThrowCollected("Text raster scale retirement committed with callback failures.", errors);
    }
}
