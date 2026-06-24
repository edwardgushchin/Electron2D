# Agent Instructions

This is the Electron2D 0.1.0-preview Platformer project.

- Project name: `Platformer`
- .NET SDK: `.NET 10.0.101`
- Renderer profile: `Automatic`

Use these commands from the project root:

- `e2d validate --project .`
- `dotnet build`
- `e2d run --project .`
- `e2d export --project .`

Project structure:

- `Platformer.e2d` stores project settings, Input Map, export presets and reproducibility metadata.
- `scenes/` stores scene files.
- `scripts/` stores C# gameplay code.
- `assets/` stores imported game assets copied from the checked reference asset pack.
- `resources/platformer.manifest.json` maps imported files to gameplay roles.
- `.electron2d/tasks/` stores ProjectTaskManager task documents.

Rules for agents:

- Prefer an active Electron2D.Editor session when the project is open.
- Do not edit `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/` or `.electron2d/user/` by hand.
- Keep stable project file paths intact unless the documented operation intentionally moves resources.
- Run `e2d validate --project .`, `dotnet build` and `dotnet run -- --verify` after changing project files.
- Use ProjectTaskManager through Editor, Tooling or MCP. Do not mark acceptance task documents as done for the user.
