# Agent Instructions

This is an Electron2D 0.1-preview project.

- Project name: `Electron2D.Empty`
- .NET SDK: `.NET 10.0.101`
- Renderer profile: `Automatic`

Use these commands from the project root:

- `e2d validate --project .`
- `dotnet build`
- `dotnet test`
- `e2d run --project .`
- `e2d export --project .`
- `e2d api compare-godot <type>`

Project structure:

- `project.e2d.json` stores project settings.
- `scenes/` stores scene files.
- `Scripts/` stores C# gameplay code.
- `.taskboard/` stores ProjectTaskManager task documents; mutate them only through `e2d tasks`.
- `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/` and `.electron2d/user/` are generated or local-only working directories.

Rules for agents:

- Prefer the active Editor session through MCP or Tooling when the project is open in Electron2D.Editor.
- Do not edit `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/` or `.electron2d/user/` by hand.
- Keep stable UID values intact unless the documented operation intentionally creates a new object.
- Run `e2d validate --project .` after changing project files.
- Do not use external API members outside the approved Electron2D Godot 4.7 public API contract. Use `e2d api compare-godot <type>` to check whether a type is approved by the current Electron2D public API profile. The command checks only manual profile approval; it does not prove full Godot 4.7 strict parity, which requires separate parity evidence.
- Read and mutate ProjectTaskManager only through `e2d tasks`; Editor, Tooling and MCP are read-only consumers of the same CLI contract. Do not edit task storage files directly.
- Link changes, tests, diagnostics, jobs and artifacts to the active task when the workflow exposes that operation.
- Submit completed agent work for human acceptance with `e2d tasks submit`; do not mark work as accepted for the user.
