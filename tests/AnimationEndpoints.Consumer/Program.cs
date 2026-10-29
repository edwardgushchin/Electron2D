if (args.Length == 0) AnimationEndpointTests.Run();
else if (args is ["sparse"]) AnimationEndpointTests.Sparse();
else if (args is ["audio-linear"]) AnimationEndpointTests.RunAudio(Electron2D.SpriteFrames.LoopMode.Linear);
else if (args is ["audio-pingpong"]) AnimationEndpointTests.RunAudio(Electron2D.SpriteFrames.LoopMode.PingPong);
else if (args is ["audio-none"]) AnimationEndpointTests.RunAudio(Electron2D.SpriteFrames.LoopMode.None);
else throw new ArgumentException("Supply sparse, audio-linear, audio-pingpong or audio-none, or no arguments for endpoint vectors.");
