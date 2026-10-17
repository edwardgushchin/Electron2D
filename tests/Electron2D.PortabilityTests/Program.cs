if (args.Length == 3 && args[0] == "--deferred-scene") DeferredSceneContract.Run(args[1], args[2]);
else ContractChecks.Run();
