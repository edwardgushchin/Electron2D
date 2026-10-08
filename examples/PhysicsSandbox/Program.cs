using Electron2D;
using Electron2D.Examples.PhysicsSandbox;

using var regular = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf")) };
Engine.MaxFPS = 60;
ProjectSettings.Set(ProjectSettings.RenderingMethod, args.Contains("--compatibility") ? "compatibility" : "gpu");
using var window = new WaterWindow(regular, !args.Contains("--cpu"));
return Engine.Run(window);
