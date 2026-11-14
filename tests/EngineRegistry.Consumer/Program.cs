if (args.Length == 0)
    EngineRegistryChecks.Run();
else
    EngineRegistryChecks.RunWindowed(args.Single());
