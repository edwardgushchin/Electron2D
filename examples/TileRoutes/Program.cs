using Electron2D;
using Electron2D.Examples.TileRoutes;

if (args is ["write", var output]) { TileRouteAssets.Write(output); return; }
var directory = args.Length == 0 ? System.IO.Path.Combine(Environment.CurrentDirectory, "tile-assets") : args[0];
if (!File.Exists(System.IO.Path.Combine(directory, "tiles.e2dscene"))) TileRouteAssets.Write(directory);
Engine.MaxFPS = 60; var window = new Window { Size = new(64, 64), Title = "Tile routes" }; window.AddChild(new TileRouteScene(System.IO.Path.Combine(directory, "tiles.e2dscene"))); window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black); Engine.Run(window);
