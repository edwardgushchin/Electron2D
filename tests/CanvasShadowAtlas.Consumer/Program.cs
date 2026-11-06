if (args.Length != 2) throw new ArgumentException("Expected backend profile and compiled shader folder.");
if (args[0] == "author") CanvasShadowAtlasChecks.Author(args[1]);
else CanvasShadowAtlasChecks.Run(args[0], args[1]);
