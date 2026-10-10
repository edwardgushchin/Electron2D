using Electron2D;
using Electron2D.Examples.AsyncGallery;

if (args is ["write", var output]) { GalleryAssets.Write(output); return; }
var directory = args.Length == 0 ? System.IO.Path.Combine(Environment.CurrentDirectory, "gallery-assets") : args[0];
if (!File.Exists(System.IO.Path.Combine(directory, "gallery.e2dscene"))) GalleryAssets.Write(directory);
ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); Engine.MaxFPS = 60;
var window = new Window { Size = new(64, 64), Title = "Async gallery" }; window.AddChild(new Gallery(System.IO.Path.Combine(directory, "gallery.e2dscene"))); window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black); Engine.Run(window);
