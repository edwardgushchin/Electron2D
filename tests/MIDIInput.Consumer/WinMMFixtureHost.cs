using System.Reflection;
using System.Runtime.Loader;

internal sealed class WinMMFixtureHost(string fixturePath, string consumerPath) : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver = new(consumerPath);

    internal static int Run(string fixturePath)
    {
        var consumerPath = typeof(WinMMFixtureHost).Assembly.Location;
        var fixture = Path.GetFullPath(fixturePath);
        var context = new WinMMFixtureHost(fixture, consumerPath);
        var consumer = context.LoadFromAssemblyPath(consumerPath);
        return consumer.EntryPoint!.Invoke(null, [new[] { "winmm-fixture-child", fixture }]) is int code ? code : 0;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName) =>
        unmanagedDllName is "winmm.dll" or "cfgmgr32.dll" ? LoadUnmanagedDllFromPath(fixturePath) : 0;
}
