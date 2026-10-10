using Electron2D;
using Electron2D.Examples.IterationRoutes;

if (args is ["write", var output]) { IterationAssets.Write(output); return; }
var directory = args.Length == 0 ? System.IO.Path.Combine(Environment.CurrentDirectory, "iteration-assets") : args[0];
if (!File.Exists(System.IO.Path.Combine(directory, "room.e2dscene"))) IterationAssets.Write(directory);
Engine.MaxFPS = 60; var window = new Window { Size = new(64, 64), Title = "Background routes" }; window.AddChild(new IterationScene(directory)); window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black); Engine.Run(window);
