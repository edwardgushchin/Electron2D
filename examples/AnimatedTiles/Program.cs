using Electron2D;
using Electron2D.Examples.AnimatedTiles;

if (args is ["write", var output]) { AnimatedTileAssets.Write(output); return; }
var directory = args.Length == 0 ? System.IO.Path.Combine(Environment.CurrentDirectory, "animated-tile-assets") : args[0];
if (!File.Exists(System.IO.Path.Combine(directory, "animated-tiles.e2dscene"))) AnimatedTileAssets.Write(directory);
Engine.MaxFPS = 60; var window = new Window { Size = new(80, 32), Title = "Animated tiles" }; window.AddChild(new AnimatedTileScene(System.IO.Path.Combine(directory, "animated-tiles.e2dscene"))); window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black); Engine.Run(window);
