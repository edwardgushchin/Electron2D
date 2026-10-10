# Procedural noise

Run `dotnet run --project examples/ProceduralNoise -- write DIRECTORY` to save the complete noise/gradient/texture graph and scene. Run the example with that directory to reload two independent scene-local patterns. After one second, the left pattern regenerates on a worker while the right stays unchanged.

The first image is generated in the scene's deferred queue. Later reads retain completed pixels until the owner publishes a replacement. `Changed` marks completed image publication. Without a running scene loop, `GetImage` generates synchronously for file and library hosts. Custom noise samplers used in a scene must support background execution and coordinated state; sources remain borrowed.
