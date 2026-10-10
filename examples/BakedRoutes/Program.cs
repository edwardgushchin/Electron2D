using Electron2D;
using Electron2D.Examples.BakedRoutes;

if (args is ["write", var output]) { RouteAssets.Write(output); return; }
var directory = args.Length == 0 ? System.IO.Path.Combine(Environment.CurrentDirectory, "route-assets") : args[0];
if (!File.Exists(System.IO.Path.Combine(directory, "room.e2dscene"))) RouteAssets.Write(directory);
ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); Engine.MaxFPS = 60;
var window = new Window { Size = new(64, 64), Title = "Baked routes" }; window.AddChild(new RouteScene(System.IO.Path.Combine(directory, "room.e2dscene"))); window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black); Engine.Run(window);
