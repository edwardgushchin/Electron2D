using Electron2D;
using Electron2D.Examples.WorkerRoutes;

ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
ProjectSettings.Set(ProjectSettings.WorkerPoolLowPriorityThreadRatio, 1);
ProjectSettings.Set(ProjectSettings.RenderingMethod, args is ["compatibility"] ? "compatibility" : "gpu");
ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
Engine.MaxFPS = 60;
var window = new Window { Title = "Worker routes", Size = new(64, 64) };
window.AddChild(new RoutePlanner());
window.Ready += _ => RenderingServer.SetDefaultClearColor(Colors.Black);
Engine.Run(window);
