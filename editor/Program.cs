using Electron2D;
using Electron2D.Editor;

using var texture = ResourceLoader.Load<ImageTexture>(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo-stacked-dark.svg"));
var markFile = args.Contains("--outline") ? "mark-dark-outlined.svg" : "mark-dark.svg";
using var mark = ResourceLoader.Load<ImageTexture>(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", markFile));
using var wordmark = new AtlasTexture { Atlas = texture, Region = new(24, 160, 440, 68), FilterClip = true };
using var font = new FontFile();
font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
Engine.MaxFPS = 60;
return Engine.Run(EditorScene.CreateWindow(mark, wordmark, font));
