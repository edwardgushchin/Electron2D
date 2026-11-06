if (args.Length is < 2 or > 3) throw new ArgumentException("Native inspector, receipt directory and optional live TLS host are required.");
SystemCAChecks.Run(args[0], args[1], args.Length == 3 ? args[2] : null);
