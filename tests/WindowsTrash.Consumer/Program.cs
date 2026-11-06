if (args is not [var oracle, var workspace]) throw new ArgumentException("Supply the native Shell oracle and controlled workspace.");
WindowsTrashTests.Run(oracle, workspace);
