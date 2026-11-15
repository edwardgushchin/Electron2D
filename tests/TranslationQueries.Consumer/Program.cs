if (args.Length == 0 || args[0] == "managed") TranslationQueryChecks.Run();
else if (args[0] == "initial-culture") TranslationQueryChecks.RunInitialCulture();
else TranslationQueryChecks.RunNative(args[0], args.Length > 1 ? args[1] : null);
