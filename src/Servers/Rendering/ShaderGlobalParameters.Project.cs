using Kind = Electron2D.RenderingServer.GlobalShaderParameterType;

namespace Electron2D;

internal sealed partial class ShaderGlobalParameters
{
    private readonly Dictionary<string, Resource> _projectImages = new(StringComparer.Ordinal);
    internal static ShaderGlobalParameters PrepareProject(ProjectSettingsRegistry settings)
    {
        var apply = settings.CaptureShaderGlobals(); var prepared = new ShaderGlobalParameters();
        try { foreach (var entry in apply) entry(prepared); return prepared; }
        catch (Exception error)
        {
            try { prepared.RetireProjectImages(); }
            catch (Exception cleanup) { throw new AggregateException("Project shader preparation and cleanup failed.", error, cleanup); }
            throw;
        }
    }
    internal void AddProjectImage(string name, Kind kind, string path)
    {
        ValidateName(name);
        if (kind is not (Kind.Sampler2D or Kind.Sampler2DArray)) throw new InvalidDataException("An image path requires an ordinary or array sampled-image role.");
        Resource? image = null;
        if (path.Length != 0 && !_projectImages.TryGetValue(path, out image))
        {
            image = kind == Kind.Sampler2D
                ? ResourceLoader.Load<Texture>(path, ResourceLoader.CacheMode.Ignore)
                : ResourceLoader.Load<TextureLayered>(path, ResourceLoader.CacheMode.Ignore);
            _projectImages.Add(path, image);
        }
        AddTexture(name, image, kind == Kind.Sampler2DArray);
    }
    internal void RetireProjectImages()
    {
        List<Exception>? errors = null;
        foreach (var image in _projectImages.Values)
            try { image.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _projectImages.Clear();
        if (errors is not null) throw new AggregateException("Project shader image cleanup failed.", errors);
    }
}
