if (args.Length != 2) throw new ArgumentException("Expected independent Shell observer and private output folder.");
FileManagerChecks.Run(args[0], args[1]);
