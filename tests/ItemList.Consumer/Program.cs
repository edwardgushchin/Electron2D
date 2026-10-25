try
{
    if (args.Length > 0 && args[0] == "input") ItemListInputTests.Run();
    else if (args.Length > 0 && args[0] == "input-gpu") ItemListInputRenderingTests.Run("gpu");
    else if (args.Length > 0 && args[0] == "input-compatibility") ItemListInputRenderingTests.Run("compatibility");
    else if (args.Length > 0 && args[0] == "author") ItemListLayoutTests.Author(args[1]);
    else if (args.Length > 0 && args[0] == "read") ItemListLayoutTests.Read(args[1]);
    else { ItemListTests.Run(); if (args.Length > 0) ItemListLayoutRenderingTests.Run(args[0]); }
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
