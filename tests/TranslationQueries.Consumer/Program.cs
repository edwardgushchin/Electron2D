if (args.Length == 0 || args[0] == "managed") TranslationQueryChecks.Run();
else TranslationQueryChecks.RunNative(args[0], args.Length > 1 ? args[1] : null);
