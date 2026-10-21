using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class ResourceLoader
{
    private sealed class LocalizedResource(string path)
    {
        internal readonly string Path = path;
        internal bool Active = true;
    }
    private readonly object _localeMapGate = new();
    private ulong _localeMapVersion;
    private ResourceLocaleMap _localeMap = new([]);
    private readonly ConditionalWeakTable<Resource, LocalizedResource> _localizedResources = new();
    private int _hasLocalizedResources;
    private int _localeReloadDepth;

    internal static ResourceLocaleMap LocaleMap()
    {
        lock (Runtime._localeMapGate)
        {
            var version = ProjectSettings.Service.VersionCore;
            if (version != Runtime._localeMapVersion)
            {
                var records = ProjectSettings.GetWithOverride(ProjectSettings.LocaleResourceRemaps);
                if (!Runtime._localeMap.Matches(records)) Runtime._localeMap = new(records);
                Runtime._localeMapVersion = version;
            }
            return Runtime._localeMap;
        }
    }
    internal static void TrackLocalizedResource(Resource resource, string path)
    {
        lock (LoadGate)
        {
            Runtime._localizedResources.AddOrUpdate(resource, new(path));
            Volatile.Write(ref Runtime._hasLocalizedResources, 1);
        }
    }
    internal static long LocalizedSourceUID(string path)
    {
        try
        {
            var id = ResourceArchive.ReadUID(path);
            if (id != ResourceUID.InvalidID) return id;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // The selected variant can load without a readable original archive header.
        }
        return ResourceUID.FindIDForPath(path);
    }
    private static bool LocalizedAlive(Resource resource, string path)
    {
        try { return !resource.IsDisposed && resource.ResourcePath == path; }
        catch (ObjectDisposedException) { return false; }
    }
    private static Resource LoadLocalized(string path, CacheMode mode, Type type, out bool owned)
    {
        lock (LoadGate)
        {
            var cached = Resource.GetRegisteredPath(path);
            if (mode == CacheMode.Reuse && cached is not null)
            {
                if (!type.IsInstanceOfType(cached)) throw new InvalidOperationException("Cached resource does not match requested type.");
                TrackLocalizedResource(cached, path); owned = false; return cached;
            }
            var graph = new ResourceLoadGraph(LoaderSnapshot(), false, default, AbortOnMissingResources);
            try
            {
                graph.Prepare(path, mode, type); var result = graph.Commit(); owned = !ReferenceEquals(cached, result); return result;
            }
            catch (Exception error)
            {
                try { graph.DisposeUnused(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                throw;
            }
        }
    }
    internal static void ReloadTranslationRemaps()
    {
        var initial = LocaleMap();
        if (initial.IsEmpty && Volatile.Read(ref Runtime._hasLocalizedResources) == 0) return;
        lock (LoadGate)
        {
            if (Runtime._localeReloadDepth >= 32) throw new InvalidOperationException("Localized resource reload nesting exceeds 32 changes.");
            Runtime._localeReloadDepth++;
            try
            {
                List<Exception>? errors = null;
                for (var pass = 0; ; pass++)
                {
                    var map = LocaleMap(); var locale = TranslationServer.Culture.Name;
                    foreach (var path in map.Paths) if (Resource.GetRegisteredPath(path) is { } cached) TrackLocalizedResource(cached, path);
                    var snapshot = Runtime._localizedResources.ToArray();
                    foreach (var pair in snapshot)
                    {
                        if (!ReferenceEquals(map, LocaleMap()) || locale != TranslationServer.Culture.Name) break;
                        var resource = pair.Key; var path = pair.Value.Path;
                        if (!LocalizedAlive(resource, path)) { Runtime._localizedResources.Remove(resource); continue; }
                        if (!map.Contains(path) && !pair.Value.Active) continue;
                        var graph = new ResourceLoadGraph(LoaderSnapshot(), false, default, AbortOnMissingResources, map, locale);
                        try
                        {
                            graph.Prepare(path, CacheMode.IgnoreDeep, resource.GetType());
                            if (!graph.LocaleIsCurrent) { graph.DisposeUnused(); break; }
                            using var prepared = graph.Commit();
                            if (LocalizedAlive(resource, path)) resource.CopyFromResource(prepared);
                            pair.Value.Active = map.Contains(path);
                        }
                        catch (Exception error)
                        {
                            try { graph.DisposeUnused(); } catch (Exception cleanup) { error = new AggregateException(error, cleanup); }
                            (errors ??= []).Add(error);
                        }
                    }
                    foreach (var pair in Runtime._localizedResources.ToArray()) if (!LocalizedAlive(pair.Key, pair.Value.Path)) Runtime._localizedResources.Remove(pair.Key);
                    Volatile.Write(ref Runtime._hasLocalizedResources, Runtime._localizedResources.Any(pair => pair.Value.Active) ? 1 : 0);
                    if (ReferenceEquals(map, LocaleMap()) && locale == TranslationServer.Culture.Name) break;
                    if (pass == 31) { (errors ??= []).Add(new InvalidOperationException("Localized resource reload did not settle after 32 locale changes.")); break; }
                }
                if (errors is not null) throw new AggregateException("One or more localized resources could not be refreshed.", errors);
            }
            finally { Runtime._localeReloadDepth--; }
        }
    }
}
