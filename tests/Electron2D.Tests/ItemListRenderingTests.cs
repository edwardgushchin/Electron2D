using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyItemListRendering(string backend)
    {
        var window = new Window { Size = new(150, 130) };
        var list = new ItemList { Position = new(10, 10), Size = new(100, 80) };
        for (var i = 0; i < 5; i++) list.AddItem("AAA");
        list.Select(0);
        window.AddChild(list);
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = server.Readback();
                frames++;
                var selected = image.GetPixel(90, 20);
                var unselected = image.GetPixel(90, 45);
                var outside = image.GetPixel(90, 95);
                Check(selected.R > unselected.R + .1f && list.GetVScrollBar().Visible,
                    $"The {backend} item list draws selected rows and a real overflow bar; selected={selected}, unselected={unselected}.");
                Check(outside.R < .02f && outside.G < .02f && outside.B < .02f,
                    $"The {backend} item list clips overflowing own content to its bounds; outside={outside}.");
                window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Released(window);
        Check(frames == 1, $"The {backend} item-list frame completed.");
        Console.WriteLine($"ItemList text/selection and vertical bar rendered on {backend}.");
    }
}
