# Background route updates

Write assets and run the consumer in separate processes:

```sh
dotnet run --project examples/IterationRoutes/IterationRoutes.csproj -c Release -- write /tmp/iteration-routes
dotnet run --project examples/IterationRoutes/IterationRoutes.csproj -c Release -- /tmp/iteration-routes
```

The scene establishes an initial room map, then stages a ring geometry replacement. It keeps the previous route during background rebuilding and draws the completed path around the red central wall. Automatic region/map worker policies are enabled by default; explicit Synchronize provides deterministic initial batch preparation.
