using Electron2D;
using Electron2D.Examples.ProceduralNoise;

if (args is ["write", var output]) { NoiseAssets.Write(output); return; }
var directory = args.Length == 0 ? System.IO.Path.Combine(Environment.CurrentDirectory, "noise-assets") : args[0];
if (!File.Exists(System.IO.Path.Combine(directory, "pattern.e2dscene"))) NoiseAssets.Write(directory);
Engine.MaxFPS = 60; var window = new Window { Size = new(64, 32), Title = "Procedural noise", CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var scene = new NoiseScene(directory); window.AddChild(scene);
var frames = 0; window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () => { if (++frames == 60) scene.Stage(); }; }; Engine.Run(window);
