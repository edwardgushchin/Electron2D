using System.Buffers.Binary;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class StencilTexturesTests
{
    private static void Failures(RenderingDevice device, string profile)
    {
        if (profile is "bridge" or "format")
        {
            Check(!device.TextureIsFormatSupportedForUsage(DataFormat.S8UINT, Roles | Transfers), "Stencil-only formats refuse missing native support.");
            Reject<NotSupportedException>(() => Image(device, DataFormat.S8UINT, Roles | Transfers));
        }
        else if (profile.StartsWith("resolve-", StringComparison.Ordinal))
        {
            var supported = profile == "resolve-only";
            Check(device.TextureIsFormatSupportedForUsage(DataFormat.S8UINT, TextureUsageBits.DepthResolveAttachment) == supported, "Stencil resolve has an independent enabled bit.");
            Check(device.TextureIsFormatSupportedForUsage(DataFormat.D32SFLOAT, TextureUsageBits.DepthResolveAttachment) != supported, "Depth resolve keeps its independent enabled bit.");
            var image = Image(device, DataFormat.S8UINT, Roles | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo, TextureSamples.Samples4); var target = device.FramebufferCreate([image]);
            device.DrawListBegin(target, DrawFlags.ClearStencil, clearStencilValue: 209); device.DrawListEnd();
            if (supported)
            {
                var destination = Image(device, DataFormat.S8UINT, TextureUsageBits.DepthResolveAttachment | Transfers);
                device.TextureResolveMultisample(image, destination); Check(device.TextureGetData(destination, 0).All(v => v == 209), "Stencil-only resolve executes when depth resolve is disabled."); device.FreeRID(destination);
            }
            else Reject<NotSupportedException>(() => Image(device, DataFormat.S8UINT, TextureUsageBits.DepthResolveAttachment | Transfers));
            device.FreeRID(target); device.FreeRID(image);
        }
        else
        {
            Environment.SetEnvironmentVariable("ELECTRON2D_STENCIL_FAULT", "pass");
            Reject<InvalidOperationException>(() => Image(device, DataFormat.S8UINT, Roles));
            Environment.SetEnvironmentVariable("ELECTRON2D_STENCIL_FAULT", null);
            var image = Image(device, DataFormat.S8UINT, Roles | Transfers); var target = device.FramebufferCreate([image]);
            var shader = Shader(device, "operations", ShaderLanguage.GLSL, "#version 450\nvoid main(){}");
            using var raster = new RDPipelineRasterizationState(); using var blend = new RDPipelineColorBlendState();
            Environment.SetEnvironmentVariable("ELECTRON2D_STENCIL_FAULT", "pass");
            Reject<InvalidOperationException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend));
            Environment.SetEnvironmentVariable("ELECTRON2D_STENCIL_FAULT", null);
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
            // Initialization used no colors; this mixed attachment key forces a fresh native render pass.
            var color = Image(device, DataFormat.R8UINT, TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom); var mixed = device.FramebufferCreate([color, image]);
            Environment.SetEnvironmentVariable("ELECTRON2D_STENCIL_FAULT", "pass"); Reject<InvalidOperationException>(() => device.DrawListBegin(mixed));
            Environment.SetEnvironmentVariable("ELECTRON2D_STENCIL_FAULT", null);
            device.DrawListBegin(mixed, DrawFlags.ClearColor0 | DrawFlags.ClearStencil, [new Color()], clearStencilValue: 169); device.DrawListEnd();
            Check(device.TextureGetData(image, 0).All(v => v == 169), "Native allocation/pipeline/pass failures release failed state and permit retry.");
            foreach (var rid in new[] { mixed, color, pipeline, target, image, shader }) device.FreeRID(rid);
        }
        var ordinary = Image(device, DataFormat.R8UINT, TextureUsageBits.ColorAttachment | Transfers); var framebuffer = device.FramebufferCreate([ordinary]);
        device.DrawListBegin(framebuffer, DrawFlags.ClearColor0, [new Color(BitConverter.Int32BitsToSingle(37), 0, 0, 0)]); device.DrawListEnd(); Check(device.TextureGetData(ordinary, 0).All(v => v == 37), "Ordinary R8 color targets survive stencil refusal/failure.");
        var bytes = new byte[64]; for (var i = 0; i < 16; i++) BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), .25f);
        var depth = Image(device, DataFormat.D32SFLOAT, TextureUsageBits.DepthStencilAttachment | Transfers, data: [bytes]); Check(device.TextureGetData(depth, 0).SequenceEqual(bytes), "Ordinary native depth transfers survive stencil refusal/failure.");
        foreach (var rid in new[] { framebuffer, ordinary, depth }) device.FreeRID(rid);
        Console.WriteLine($"Stencil {profile} control PASS {_checks}");
    }
}
