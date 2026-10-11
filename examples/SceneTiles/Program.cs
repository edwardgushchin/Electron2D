using Electron2D;
using Electron2D.Examples.SceneTiles;

if (args is ["write", var output]) { SceneTileAssets.Write(output); return 0; }
var directory = args.Length == 0 ? System.IO.Path.Combine(Environment.CurrentDirectory, "scene-assets") : args[0];
var path = System.IO.Path.Combine(directory, "scenes.e2dscene");
if (!File.Exists(path)) SceneTileAssets.Write(directory);
using var packed = ResourceLoader.Load<PackedScene>(path);
Engine.MaxFPS = 60;
var window = new Window { Size = new(288, 128), Title = "Scene tiles" };
window.AddChild(packed.Instantiate());
window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black);
return Engine.Run(window);
