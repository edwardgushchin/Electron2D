using System.Globalization;
using System.Text;

namespace Electron2D;

public sealed partial class VisualShader
{
    private sealed record MeshEmissionBake(int Count, int Width, Dictionary<int, string> Channels, HashSet<int> Ports);
    private static MeshEmissionBake BakeMeshEmission(VisualShaderNodeParticleMeshEmitter emitter, Type stage, int id,
        HashSet<int> ports, List<VisualShaderImage> images, List<Resource> owned, HashSet<string> sourceNames)
    {
        var mesh = emitter.Mesh; var surfaces = new List<MeshSurfaceData>(); var count = 0;
        if (mesh is not null)
        {
            var surfaceCount = mesh.GetSurfaceCount();
            var indices = emitter.UseAllSurfaces ? Enumerable.Range(0, surfaceCount) : emitter.SurfaceIndex < surfaceCount ? [emitter.SurfaceIndex] : Enumerable.Empty<int>();
            foreach (var surface in indices)
            {
                var data = mesh.SurfaceGetArrays(surface); data.Validate(mesh.SurfaceGetPrimitiveType(surface));
                if (data.IsProcedural) continue;
                count = checked(count + data.Vertices.Length); surfaces.Add(data);
            }
        }
        count = Math.Max(1, count); var width = Math.Min(4096, count); var height = checked((count + width - 1) / width);
        if ((long)width * height * 16 > 64 * 1024 * 1024) throw new InvalidDataException("A baked particle mesh channel exceeds the image byte budget.");
        var channels = new Dictionary<int, string>(); var names = images.Select(i => i.Name).Concat(sourceNames).ToHashSet(StringComparer.Ordinal);
        foreach (var channel in ports.Select(p => p == 3 ? 2 : p).Distinct().Order())
        {
            var stem = "e2d_mesh_" + (int)stage + "_" + id + "_" + channel; var symbol = stem;
            for (var suffix = 2; !names.Add(symbol); suffix++) symbol = stem + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            using var image = Image.CreateEmpty(width, height, false, channel == 2 ? Image.Format.Rgba8 : Image.Format.Rgbaf);
            var at = 0;
            foreach (var surface in surfaces)
                for (var vertex = 0; vertex < surface.Vertices.Length; vertex++, at++)
                {
                    var value = channel switch
                    {
                        0 => new Color(surface.Vertices[vertex].X, surface.Vertices[vertex].Y, 0, 0),
                        1 when surface.Normals.Length != 0 => new Color(surface.Normals[vertex].X, surface.Normals[vertex].Y, 0, 0),
                        2 when surface.Colors.Length != 0 => surface.Colors[vertex],
                        4 when surface.UVs.Length != 0 => new Color(surface.UVs[vertex].X, surface.UVs[vertex].Y, 0, 0),
                        5 when surface.UV2s.Length != 0 => new Color(surface.UV2s[vertex].X, surface.UV2s[vertex].Y, 0, 0),
                        _ => new Color(0, 0, 0, 0)
                    };
                    image.SetPixel(at % width, at / width, value);
                }
            var texture = ImageTexture.CreateFromImage(image); owned.Add(texture);
            images.Add(new(stage, symbol, false, new(false, 0, 1, 1, false), texture)); channels.Add(channel, symbol);
        }
        return new(count, width, channels, ports);
    }
    private static void EmitMeshEmission(DomainCompilation domain, Type stage, int id,
        Dictionary<(int Node, int Port), string> values, StringBuilder source, HashSet<string> names)
    {
        var bake = domain.Meshes[(stage, id)]; var index = "n" + id + "vertex";
        while (!names.Add(index)) index += "_";
        source.Append("int ").Append(index).Append(" = int(").Append(domain.Symbols["e2d_random"]).Append('(').Append(domain.Symbols["e2d_seed"]).Append(") * 65535.0) % ").Append(bake.Count).AppendLine(";");
        foreach (var port in bake.Ports.Order())
        {
            var channel = port == 3 ? 2 : port; var symbol = "n" + id + "p" + port;
            while (!names.Add(symbol)) symbol += "_";
            var type = port == 2 ? "vec3" : port == 3 ? "float" : "vec2";
            var swizzle = port == 2 ? ".rgb" : port == 3 ? ".a" : ".xy";
            source.Append(type).Append(' ').Append(symbol).Append(" = texelFetch(").Append(bake.Channels[channel]).Append(", ivec2(").Append(index).Append(" % ").Append(bake.Width).Append(", ").Append(index).Append(" / ").Append(bake.Width).Append("), 0)").Append(swizzle).AppendLine(";");
            values.Add((id, port), symbol);
        }
    }
}
