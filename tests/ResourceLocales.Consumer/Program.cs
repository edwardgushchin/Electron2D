if (args is ["optimized", var operation, var directory])
    OptimizedCatalogChecks.Run(operation, directory);
else
    ResourceLocaleChecks.Run(args);
