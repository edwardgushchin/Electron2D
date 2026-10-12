using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class RDShaderFileTests
{
    private static RDShaderSPIRV Compute(string name)
    {
        using var input = typeof(RDShaderFileTests).Assembly.GetManifestResourceStream("Electron2D.Compute." + name + ".spv")!; using var stream = new MemoryStream(); input.CopyTo(stream); return new() { BytecodeCompute = stream.ToArray() };
    }
    internal static void Run()
    {
        Contract(); Storage(); LocalScenes(); Warm(); Fresh();
        Console.WriteLine("Shader versions: exact borrowed identities, scalar ordering, diagnostics, failure/reentry, graph copies/storage/fresh process and zero warmed lookup/replacement passed.");
    }
    private static void Contract()
    {
        using var file = new RDShaderFile(); using var a = new RDShaderSPIRV(); using var b = new RDShaderSPIRV(); Check(file.BaseError == "" && file.GetSPIRV() is null && file.GetVersionList().Length == 0, "Empty defaults and missing versions.");
        var changes = 0; file.Changed += _ => changes++; file.SetBytecode(a); file.SetBytecode(a); file.BaseError = "parse diagnostic"; file.BaseError = "parse diagnostic"; Check(changes == 4 && ReferenceEquals(file.GetSPIRV(), a), "Equal assignments notify and diagnostic does not hide modules.");
        foreach (var name in new[] { "z", "A", "a", "\uE000", "\U00010000" }) file.SetBytecode(a, name); Check(file.GetVersionList().SequenceEqual(new[] { "", "A", "a", "z", "\uE000", "\U00010000" }), "Scalar lexical order differs from UTF16 surrogate ordering."); var names = file.GetVersionList(); names[0] = "broken"; Check(file.GetVersionList()[0] == "", "Version lists copy containers.");
        file.SetBytecode(b); Check(ReferenceEquals(file.GetSPIRV(), b) && !a.IsDisposed && file.GetSPIRV("Z") is null, "Replacement borrows modules and names are case sensitive.");
        Reject<ArgumentNullException>(() => file.SetBytecode(null!)); Reject<ArgumentNullException>(() => file.GetSPIRV(null!)); foreach (var invalid in new[] { "bad\0name", "\uD800", "\uDC00" }) { Reject<ArgumentException>(() => file.SetBytecode(a, invalid)); Reject<ArgumentException>(() => file.GetSPIRV(invalid)); }
        Action<Resource> failed = _ => throw new InvalidOperationException("Changed fixture"); file.Changed += failed; Reject<InvalidOperationException>(() => file.SetBytecode(a, "committed")); file.Changed -= failed; Check(ReferenceEquals(file.GetSPIRV("committed"), a), "Callback failure follows committed edit.");
        Action<Resource>? reentrant = null; reentrant = _ => { file.Changed -= reentrant; file.SetBytecode(b, "nested"); }; file.Changed += reentrant; file.SetBytecode(a, "outer"); Check(ReferenceEquals(file.GetSPIRV("nested"), b), "Callbacks may edit after commitment without storage lock dispatch.");
        using var dead = new RDShaderSPIRV(); dead.Dispose(); Reject<ObjectDisposedException>(() => file.SetBytecode(dead, "dead")); file.SetBytecode(a, "borrowed"); a.Dispose(); Reject<ObjectDisposedException>(() => file.GetSPIRV("borrowed")); Check(file.GetVersionList().Contains("borrowed"), "Disposed borrowed entry remains replaceable by name."); file.Dispose(); Check(!b.IsDisposed, "File disposal releases references without closing borrowed modules."); Reject<ObjectDisposedException>(() => file.GetVersionList());
    }
    private static void Storage()
    {
        using var a = new RDShaderSPIRV { BytecodeCompute = [1, 2, 3], CompileErrorCompute = "stage error" }; using var b = new RDShaderSPIRV { BytecodeVertex = [4], CompileErrorFragment = "fragment error" }; using var file = new RDShaderFile { BaseError = "base error" }; file.SetBytecode(a); file.SetBytecode(a, "alias"); file.SetBytecode(b, "graphics");
        using var shallow = (RDShaderFile)file.Duplicate(); using var deep = (RDShaderFile)file.Duplicate(true); Check(ReferenceEquals(shallow.GetSPIRV(), a) && !ReferenceEquals(deep.GetSPIRV(), a) && ReferenceEquals(deep.GetSPIRV(), deep.GetSPIRV("alias")) && deep.BaseError == "base error", "Shallow borrowing/deep alias preservation and diagnostics."); deep.GetSPIRV()!.BytecodeCompute = [9]; Check(a.BytecodeCompute[0] == 1, "Deep bytecode copies are independent."); using var copied = new RDShaderFile(); copied.CopyFromResource(file); Check(ReferenceEquals(copied.GetSPIRV("graphics"), b), "CopyFrom preserves target identity and borrowed child graph.");
        var names = (PropertyDescriptor<RDShaderFile, string[]>)file.GetPropertyList().Single(p => p.Name == "_version_names"); var modules = (PropertyDescriptor<RDShaderFile, RDShaderSPIRV[]>)file.GetPropertyList().Single(p => p.Name == "_version_modules"); Reject<InvalidDataException>(() => names.SetValue(copied, ["x", "x"])); names.SetValue(copied, ["x"]); Reject<InvalidDataException>(() => modules.SetValue(copied, [])); Check(ReferenceEquals(copied.GetSPIRV(), a), "Incomplete stored pairs preserve prior versions."); modules.SetValue(copied, [b]); Check(copied.GetSPIRV() is null && ReferenceEquals(copied.GetSPIRV("x"), b), "Stored restore replaces the full map atomically.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-shader-versions-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try { ResourceSaver.Save(file, path); using var loaded = ResourceLoader.Load<RDShaderFile>(path, ResourceLoader.CacheMode.Ignore); Check(ReferenceEquals(loaded.GetSPIRV(), loaded.GetSPIRV("alias")) && loaded.GetSPIRV()!.CompileErrorCompute == "stage error" && loaded.GetSPIRV("graphics")!.CompileErrorFragment == "fragment error" && loaded.BaseError == "base error", "Portable versions preserve aliases, all stage diagnostics and base error."); } finally { File.Delete(path); }
    }
    private sealed class FileNode : Node
    {
        internal RDShaderFile? File;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateFileNode;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([new PropertyDescriptor<FileNode, RDShaderFile?>("File", n => n.File, (n, v) => n.File = v, _ => null, stored: true)]);
    }
    private static FileNode CreateFileNode() => new();
    private static void LocalScenes()
    {
        ResourceFileTypes.RegisterNode("tests.ShaderFileNode", CreateFileNode);
        using var module = new RDShaderSPIRV { ResourceLocalToScene = true }; using var file = new RDShaderFile { ResourceLocalToScene = true }; file.SetBytecode(module); file.SetBytecode(module, "alias"); using var root = new FileNode { Name = "Root", File = file }; var child = new FileNode { Name = "Child", File = file }; root.AddChild(child); child.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root); using var one = (FileNode)scene.Instantiate(); using var two = (FileNode)scene.Instantiate(); Check(!ReferenceEquals(file, one.File) && !ReferenceEquals(one.File, two.File) && ReferenceEquals(one.File, ((FileNode)one.GetNode("Child")).File), "Local version collections retain aliases inside independent scene copies."); Check(!ReferenceEquals(module, one.File!.GetSPIRV()) && ReferenceEquals(one.File.GetSPIRV(), one.File.GetSPIRV("alias")) && !ReferenceEquals(one.File.GetSPIRV(), two.File!.GetSPIRV()), "Local modules retain aliases and independent instance ownership.");
    }
    private static void Warm()
    {
        using var file = new RDShaderFile(); using var a = new RDShaderSPIRV(); using var b = new RDShaderSPIRV(); file.SetBytecode(a, "selected");
        void Step(int i) { file.SetBytecode(i % 2 == 0 ? a : b, "selected"); file.BaseError = i % 2 == 0 ? "" : "diagnostic"; Check(file.GetSPIRV("selected") is not null && file.GetSPIRV("missing") is null, "Prepared selection."); }
        for (var i = 0; i < 24; i++) Step(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Step(i); Check(before == GC.GetAllocatedBytesForCurrentThread(), "Prepared version replacement/lookups/diagnostic writes allocate zero managed bytes."); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { Check(file.GetSPIRV("selected") is not null && file.BaseError == "diagnostic", "Prepared idle queries."); }
        Check(before == GC.GetAllocatedBytesForCurrentThread(), "Prepared idle queries allocate zero managed bytes.");
    }
    private static void Fresh(bool native = false)
    {
        using var a = Compute("LocalCompute.comp"); using var b = Compute("LocalComputeHLSL.comp"); using var file = new RDShaderFile(); file.SetBytecode(a); file.SetBytecode(a, "alias"); file.SetBytecode(b, "HLSL"); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-shader-versions-fresh-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try { ResourceSaver.Save(file, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(RDShaderFileTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_SHADER_FILE"); start.Environment.Remove("ELECTRON2D_TEST_SHADER_FILE_NATIVE"); start.Environment["ELECTRON2D_TEST_SHADER_FILE_CHILD"] = path; if (native) start.Environment["ELECTRON2D_TEST_SHADER_FILE_DEVICE_CHILD"] = "1"; using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Shader versions child."); } Check(child.ExitCode == 0 && stdout.GetAwaiter().GetResult().Contains("Fresh shader versions passed"), stderr.GetAwaiter().GetResult()); } finally { File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        using var file = ResourceLoader.Load<RDShaderFile>(path, ResourceLoader.CacheMode.Ignore); Check(ReferenceEquals(file.GetSPIRV(), file.GetSPIRV("alias")) && file.GetSPIRV("HLSL")!.BytecodeCompute.Length > 0, "Fresh file retains named compiled modules and aliases."); if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_SHADER_FILE_DEVICE_CHILD") == "1") Device(file); Console.WriteLine("Fresh shader versions passed.");
    }
    internal static void RunNative()
    {
        Run(); Fresh(true); using var a = Compute("LocalCompute.comp"); using var b = Compute("LocalComputeHLSL.comp"); using var file = new RDShaderFile(); file.SetBytecode(a); file.SetBytecode(b, "HLSL"); Device(file); Graphics(); Console.WriteLine("Saved shader version selection executes GLSL/HLSL compute and graphics bytecode through native device pipelines.");
    }
    private static void Graphics()
    {
        using var module = RenderingDeviceGraphicsTests.Load("DeviceProcedural"); using var file = new RDShaderFile(); file.SetBytecode(module, "graphics"); file.SetBytecode(module, "alias");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-shader-graphics-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(file, path); using var saved = ResourceLoader.Load<RDShaderFile>(path, ResourceLoader.CacheMode.Ignore); using var device = RenderingServer.CreateLocalRenderingDevice();
            using var format = new RDTextureFormat { Width = 16, Height = 16, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView();
            var output = device.TextureCreate(format, view); var target = device.FramebufferCreate([output]); var shader = device.ShaderCreateFromSPIRV(saved.GetSPIRV("graphics")!);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
            var list = device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var pixels = device.TextureGetData(output, 0); var center = (8 * 16 + 8) * 4;
            Check(pixels[center] == 255 && pixels[center + 1] == 0 && pixels[center + 2] == 0 && pixels[center + 3] is >= 127 and <= 128, "Saved graphics version creates a real vertex/fragment pipeline and target pixels.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SHADER_FILE_OUTPUT") is { } directory) { Directory.CreateDirectory(directory); using var image = Image.CreateFromData(16, 16, false, Image.Format.Rgba8, pixels); image.SavePNG(System.IO.Path.Combine(directory, "graphics.png")); }
            Check(ReferenceEquals(saved.GetSPIRV("graphics"), saved.GetSPIRV("alias")), "Saved graphics aliases retain selected module identity."); device.FreeRID(shader); device.FreeRID(output);
        }
        finally { File.Delete(path); }
    }
    private static void Device(RDShaderFile file)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice(); var values = Enumerable.Range(0, 64).Select(i => (float)i).ToArray(); var output = device.StorageBufferCreate(256, MemoryMarshal.AsBytes(values.AsSpan())); var uniform = device.UniformBufferCreate(16); Span<float> settings = stackalloc float[4]; settings[0] = 2; settings[1] = 64; settings[2] = 3; device.BufferUpdate(uniform, 0, 16, MemoryMarshal.AsBytes(settings));
        using var data = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.StorageBuffer }; data.AddID(output); using var parameters = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.UniformBuffer }; parameters.AddID(uniform);
        var shaders = new RID[2]; var pipelines = new RID[2]; var sets = new RID[2]; var paramsSets = new RID[2]; var names = new[] { "", "HLSL" };
        for (var i = 0; i < 2; i++) { shaders[i] = device.ShaderCreateFromSPIRV(file.GetSPIRV(names[i])!); pipelines[i] = device.ComputePipelineCreate(shaders[i]); sets[i] = device.UniformSetCreate([data], shaders[i], 3); paramsSets[i] = device.UniformSetCreate([parameters], shaders[i], 2); }
        void Step(int i) { var index = i % 2; var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipelines[index]); device.ComputeListBindUniformSet(list, sets[index], 3); device.ComputeListBindUniformSet(list, paramsSets[index], 2); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, MemoryMarshal.AsBytes(values.AsSpan())); }
        for (var i = 0; i < 2; i++) { device.BufferUpdate(output, 0, 256, MemoryMarshal.AsBytes(Enumerable.Range(0, 64).Select(x => (float)x).ToArray().AsSpan())); Step(i); Check(values[63] == 129, "Selected versions execute exact GPU results."); }
        settings[0] = 1; settings[2] = 0; device.BufferUpdate(uniform, 0, 16, MemoryMarshal.AsBytes(settings)); for (var i = 0; i < 24; i++) Step(i); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Step(i); Check(before == GC.GetAllocatedBytesForCurrentThread(), "Prepared selected pipelines dispatch/read back without managed allocation."); foreach (var shader in shaders) device.FreeRID(shader); device.FreeRID(output); device.FreeRID(uniform);
    }
}
