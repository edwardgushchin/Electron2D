using Electron2D;
using static TextureArrayTests;

internal static partial class SamplerArrayTests
{
    private sealed class CallbackTexture(Image image) : Texture
    {
        internal Action? Read;
        public override int GetWidth() => 2;
        public override int GetHeight() => 2;
        public override Image? GetImage() { Read?.Invoke(); return (Image)image.Duplicate(); }
    }
    private static void Callbacks()
    {
        var code = Module("GLSL"); using var shader = Shader.CreateFromSPIRV(code); using var material = new ShaderMaterial { Shader = shader };
        using var red = Solid(Colors.Red); using var blue = Solid(Colors.Blue); using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var source = new CallbackTexture(image); Texture?[] request = [red, source];
        source.Read = () => request[0] = blue; material.SetShaderTextureParameterArray("colorMaps", request);
        Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[0], red), "Capture callbacks cannot alter the pending array by mutating caller storage.");
        material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { red, blue });
        source.Read = () => material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { blue, red }); source.EmitChanged();
        Reject<InvalidOperationException>(() => material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { source, blue }));
        Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[0], red), "Reentrant array update rejects before mutation.");
        source.Read = () => shader.SetSPIRV(code); source.EmitChanged();
        Reject<InvalidOperationException>(() => material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { blue, source }));
        Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[0], red), "Reentrant reload retains its own result without publishing stale overrides.");
        using var first = Solid(Colors.Green); source.Read = first.Dispose; source.EmitChanged();
        Reject<ObjectDisposedException>(() => material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { first, source }));
        Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[0], red), "A later capture disposing an earlier resource preserves the complete old value.");
        var defaults = (PropertyDescriptor<Shader, Texture?[]>)shader.GetPropertyList().Single(p => p.Name == "default_texture_parameter/colorMaps");
        request[0] = red; request[1] = source; source.Read = () => request[0] = blue; source.EmitChanged(); defaults.SetValue(shader, request);
        Check(ReferenceEquals(shader.GetDefaultTextureParameter("colorMaps", 0), red), "Default arrays snapshot caller references before callbacks.");
        source.Read = () => shader.SetDefaultTextureParameter("colorMaps", blue); source.EmitChanged();
        Reject<InvalidOperationException>(() => defaults.SetValue(shader, new Texture?[] { source, blue }));
        Check(ReferenceEquals(shader.GetDefaultTextureParameter("colorMaps", 0), red), "Reentrant default mutation rejects before publication.");
        void Fail(Resource _) => throw new InvalidOperationException("after commit");
        material.Changed += Fail; Reject<InvalidOperationException>(() => material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { blue, red })); material.Changed -= Fail;
        Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[0], blue), "Changed failures propagate after committed array updates.");
        source.Read = null; source.EmitChanged(); material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { red, blue });
        Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[0], red), "Every failed validation and signal path releases the guard.");
        Console.WriteLine("Sampler callbacks: input snapshots, reentry/reload/disposal rejection and post-commit signal failures passed.");
    }
}
