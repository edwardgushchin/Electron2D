using System.Reflection;
using Electron2D;

internal static partial class ScriptTests
{
    private static MethodMetadataCode CreateMetadataCode() => new();
    internal static void MethodMetadata()
    {
        Script.RegisterAbstract<MethodMetadataBase>("MethodMetadata.Base", MethodMetadataBase.SourcePath);
        Script.RegisterObject("MethodMetadata.Code", MethodMetadataCode.SourcePath, CreateMetadataCode);
        using var source = ResourceLoader.Load<Script>(MethodMetadataCode.SourcePath);
        var methods = source.GetScriptMethodList();
        MethodInfo Find(string name, Type owner) => methods.Single(m => m.Name == name && m.DeclaringType == owner);
        var normal = Find(nameof(MethodMetadataCode.Normal), typeof(MethodMetadataCode));
        Check(!normal.IsStatic && !normal.IsVirtual && !normal.IsAbstract && normal.ReturnType == typeof(int), "ordinary typed signature");
        Check(normal.GetParameters()[0].HasDefaultValue && (int)normal.GetParameters()[0].DefaultValue! == 7, "optional argument metadata");
        Check(Find(nameof(MethodMetadataCode.Static), typeof(MethodMetadataCode)).IsStatic, "static declaration");
        var optional = Find(nameof(MethodMetadataBase.Optional), typeof(MethodMetadataBase));
        var required = Find(nameof(MethodMetadataBase.Required), typeof(MethodMetadataBase));
        var implementation = Find(nameof(MethodMetadataCode.Required), typeof(MethodMetadataCode));
        Check(optional.IsVirtual && !optional.IsAbstract && !optional.IsFinal, "optional overridable declaration");
        Check(required.IsVirtual && required.IsAbstract && !required.IsFinal, "required abstract declaration");
        Check(implementation.IsVirtual && !implementation.IsAbstract && implementation.IsFinal && implementation.GetBaseDefinition() == required, "sealed implementation retains required base provenance without remaining overridable");
        var variadic = Find(nameof(MethodMetadataCode.Many), typeof(MethodMetadataCode));
        var array = Find(nameof(MethodMetadataCode.Array), typeof(MethodMetadataCode));
        Check(variadic.GetParameters()[0].IsDefined(typeof(ParamArrayAttribute)) && !array.GetParameters()[0].IsDefined(typeof(ParamArrayAttribute)), "typed params differs from fixed array");
        Check((variadic.CallingConvention & CallingConventions.VarArgs) == 0, "C# params is not CLR unmanaged varargs");
        Check(methods.Any(m => m.IsPrivate && m.Name == "Hidden") && methods.All(m => !m.IsSpecialName && m.DeclaringType!.Assembly == typeof(MethodMetadataCode).Assembly), "private project declarations included; accessors and engine base declarations excluded");
        using var instance = source.New<MethodMetadataCode>();
        Check(instance.Normal() == 7 && MethodMetadataCode.Static() == 3 && instance.Optional() == 2 && instance.Required() == 4 && instance.Many() == 0 && instance.Many(1, 2, 3) == 6, "compiled calls preserve applicable method behavior");
        using var copy = (Script)source.Duplicate();
        methods[0] = null!;
        Check(source.GetScriptMethodList().All(m => m is not null) && copy.GetScriptMethodList().All(m => m is not null), "independent list and duplicate wrapper snapshots");
        source.SourceCode += "\n// metadata remains compiled";
        source.Dispose();
        Reject<ObjectDisposedException>(() => source.GetScriptMethodList());
        Check(copy.GetScriptMethodList().Contains(required) && instance.Required() == 4 && normal.ReturnType == typeof(int), "catalog and acquired metadata outlive disposed source wrapper");
        Reject<InvalidOperationException>(() => Script.RegisterObject("MethodMetadata.Code", MethodMetadataCode.SourcePath, CreateMetadataCode));
        using var unbound = new Script();
        Check(unbound.GetScriptMethodList().Length == 0, "unbound snapshot");
        Console.WriteLine("Script method metadata: signatures, static/virtual/abstract/sealed, params/fixed array, compiled calls, snapshots and registration/lifetime passed.");
    }
}

internal sealed class MethodMetadataCode : MethodMetadataBase
{
    internal static new readonly string SourcePath = Source();
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
    public int Value { get; set; }
    public int Normal(int value = 7) => value;
    public static int Static() => 3;
    public sealed override int Required() => 4;
    public int Many(params int[] values) => values.Sum();
    public int Array(int[] values) => values.Sum();
    private int Hidden() => Value;
}
