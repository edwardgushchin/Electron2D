namespace Electron2D;

public sealed partial class RenderingServer
{
    private RID _testTextureRID, _whiteTextureRID;

    private RID BuiltinTexture(ref RID cached, bool white)
    {
        EnsureOwner();
        if (cached.IsValid()) return cached;
        EnsureTextureChange();
        var size = white ? 4 : 256;
        var bytes = new byte[size * size * 3];
        if (white) bytes.AsSpan().Fill(255);
        else
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var offset = (y * size + x) * 3;
                    var shade = (byte)(255 - (x + y) / 2);
                    var grid = x % 32 < 2 || y % 32 < 2;
                    bytes[offset] = grid ? (byte)y : shade;
                    bytes[offset + 1] = grid ? shade : (byte)x;
                    bytes[offset + 2] = grid ? (byte)x : (byte)y;
                }
        using var image = Image.CreateFromData(size, size, false, Image.Format.Rgb8, bytes);
        return cached = Texture2DCreateCore(image);
    }

    /// <summary>Returns the current renderer's lazily created 256-by-256 RGB8 test-pattern texture.</summary>
    /// <returns>A cached texture RID usable by ordinary canvas and shader operations.</returns>
    /// <remarks>Creation is cold allocation-capable work. Later calls retain the same identity and any ordinary
    /// texture updates or replacement applied to it. FreeRID retires it; the next call creates the original pattern
    /// with a fresh identity. Shutdown releases it with other renderer-owned textures.</remarks>
    /// <exception cref="InvalidOperationException">No active renderer exists, the caller is off-owner, or creation occurs during submission/shutdown.</exception>
    public static RID GetTestTexture() { var service = RequireService(); return service.BuiltinTexture(ref service._testTextureRID, false); }

    /// <summary>Returns the current renderer's lazily created 4-by-4 opaque white RGB8 texture.</summary>
    /// <returns>A cached texture RID usable by ordinary canvas and shader operations.</returns>
    /// <remarks>Creation is cold allocation-capable work. Ordinary updates/replacement retain the cached identity.
    /// FreeRID permits fresh creation; renderer shutdown releases the texture.</remarks>
    /// <exception cref="InvalidOperationException">No active renderer exists, the caller is off-owner, or creation occurs during submission/shutdown.</exception>
    public static RID GetWhiteTexture() { var service = RequireService(); return service.BuiltinTexture(ref service._whiteTextureRID, true); }
}
