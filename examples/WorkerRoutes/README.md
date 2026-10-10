# Worker routes

Run `dotnet run --project examples/WorkerRoutes -c Release` for the GPU renderer,
or append `-- compatibility` for the compatibility renderer.

Four independent AStarGrid instances compute routes on two workers. Each frame
submits an indexed group, waits for its bounded computation, and publishes marker
positions through SceneTree.Defer. The scene owns and disposes the grids after
waiting; callbacks borrow them. All rendering and position publication run on the
scene owner. Routes allocate on their first search; repeated worker dispatch,
publication and prepared drawing are checked separately.

Configure worker count, ordinary priority ratio and pending record capacity before
the first submission. Always wait for every returned ID. Browser worker startup
requires a threaded runtime; desktop rendering and worker execution do not prove
mobile, Web, foreign-platform or human acceptance.
