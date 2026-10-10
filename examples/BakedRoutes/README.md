# Baked routes

Author a portable room with `dotnet run --project examples/BakedRoutes -c Release -- write /path/to/assets`.
Run a fresh process with `dotnet run --project examples/BakedRoutes -c Release -- /path/to/assets`.

The separately loaded scene owns navigation outlines and static collision source data.
Its region parses root-local geometry, prepares a navigation bake on workers,
publishes on its SceneTree owner and queries the resulting obstacle-avoiding map.
The window renders the wall, routed line and a frame marker through the public API.

The native verification host holds both worker lanes for thirty rendered frames,
then checks owner completion and real wall/route pixels on GPU and compatibility.
Preparation and file authoring may allocate. Current allocation/target/acceptance
limits are recorded in the navigation baking component.
