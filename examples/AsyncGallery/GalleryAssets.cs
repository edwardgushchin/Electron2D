namespace Electron2D.Examples.AsyncGallery;

internal static class GalleryAssets
{
    internal static void Write(string directory)
    {
        Directory.CreateDirectory(directory); directory = System.IO.Path.GetFullPath(directory);
        using var red = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); red.Fill(Colors.Red); var redPath = System.IO.Path.Combine(directory, "red.png"); red.SavePNG(redPath);
        using var blue = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); blue.Fill(Colors.Blue); var bluePath = System.IO.Path.Combine(directory, "blue.png"); blue.SavePNG(bluePath);
        using var redTexture = ImageTexture.CreateFromImage(red); redTexture.ResourcePath = redPath;
        using var blueTexture = ImageTexture.CreateFromImage(blue); blueTexture.ResourcePath = bluePath;
        using var root = new Entity { Name = "GalleryContent" }; var left = new Sprite { Name = "Red", Texture = redTexture, Position = new(16, 24) }; var right = new Sprite { Name = "Blue", Texture = blueTexture, Position = new(48, 24) }; root.AddChild(left); left.Owner = root; root.AddChild(right); right.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root); ResourceSaver.Save(packed, System.IO.Path.Combine(directory, "gallery.e2dscene"));
    }
}
