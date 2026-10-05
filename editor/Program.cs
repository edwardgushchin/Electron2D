using Electron2D;
using Electron2D.Editor;

using var texture = ResourceLoader.Load<ImageTexture>(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo-stacked-dark.svg"));
Engine.MaxFPS = 60;
return Engine.Run(EditorScene.CreateWindow(texture));
