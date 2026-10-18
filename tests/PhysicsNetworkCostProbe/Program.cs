using Electron2D.Examples.PhysicsNetwork;

if (args.SequenceEqual(new[] { "--self-test" })) { CostSampler.Test(); return 0; }
return await NetworkApp.Run(args);
