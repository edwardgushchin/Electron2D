using Electron2D;
using Electron2D.Examples.PhysicsSandbox;

using var regular = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf")) };
using var semibold = new FontFile { Data = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-SemiBold.ttf")) };
Engine.MaxFPS = 60;
ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
using var window = new SandboxWindow(regular, semibold);
return Engine.Run(window);
