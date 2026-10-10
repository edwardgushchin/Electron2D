# Async gallery

Author portable files with `dotnet run --project examples/AsyncGallery -c Release -- write /path/to/assets`.
Run a fresh process with `dotnet run --project examples/AsyncGallery -c Release -- /path/to/assets`.

The scene requests a PackedScene and permits parallel image dependency preparation.
It polls progress during normal frames, receives cache publication on its SceneTree
owner, instantiates the ready scene, and disposes the template while the instance
retains its resource graph. A cancellation token and completion get cover shutdown.
Every accepted request must be collected, including failure.

The native verification host controls two real image load hooks: frames keep
rendering while I/O waits; after release it checks owner scene consumption and
red/blue pixels on GPU and compatibility backends. Source authoring and load/run
use separate processes. Native/foreign/AOT/browser/human gates are recorded
separately; the example itself uses only public Electron2D API.
