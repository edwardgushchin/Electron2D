using Electron2D;
using Electron2D.Examples;
using IOPath = System.IO.Path;

// Resolve bundled assets relative to the executable, so the working directory does not matter.
var assets = IOPath.Combine(AppContext.BaseDirectory, "Assets");
using var character = ResourceLoader.Load<ImageTexture>(IOPath.Combine(assets, "mark-dark.svg"));
using var font = new FontFile();
font.LoadDynamicFont(IOPath.Combine(assets, "IBMPlexSans-Regular.ttf"));

// Engine.Run owns the scene. Keep its borrowed texture and font alive until the host returns.
var window = CharacterMovementScene.CreateWindow(character, font);
Engine.MaxFPS = 60;
return Engine.Run(window);
