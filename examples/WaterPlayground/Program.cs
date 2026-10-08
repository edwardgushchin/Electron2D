using Electron2D;
using Electron2D.Examples.WaterPlayground;

WaterWindow.ConfigurePresentation();
using var regular = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf")) };
Engine.MaxFPS = 144;
ProjectSettings.Set(ProjectSettings.RenderingMethod, args.Contains("--compatibility") ? "compatibility" : "gpu");
using var window = new WaterWindow(regular, !args.Contains("--cpu"));
return Engine.Run(window);
