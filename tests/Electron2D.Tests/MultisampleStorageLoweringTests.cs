using System.Reflection;
using System.Runtime.CompilerServices;
using Electron2D;

internal static class MultisampleStorageLoweringTests
{
    internal static void Run(string directory)
    {
        var remap = typeof(RenderingDevice).GetMethod("Remap", BindingFlags.Static | BindingFlags.NonPublic)!;
        var output = System.IO.Path.Combine(directory, "native"); Directory.CreateDirectory(output);
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(directory, "*.spv"))
        {
            var stage = Enum.Parse<RenderingDevice.ShaderStage>(System.IO.Path.GetFileNameWithoutExtension(file).Split('.')[^1]);
            var result = (ITuple)remap.Invoke(null, [File.ReadAllBytes(file), stage])!;
            File.WriteAllBytes(System.IO.Path.Combine(output, System.IO.Path.GetFileName(file)), (byte[])result[0]!); count++;
        }
        if (count == 0) throw new InvalidOperationException("No shader fixtures were lowered.");
        Console.WriteLine($"Multisample native shader lowering PASS {count}");
    }
}
