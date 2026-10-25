# SessionDocumentKind

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [ProjectSession.Models.cs](../../editor/Session/ProjectSession.Models.cs).
- **Declaration:** `public enum SessionDocumentKind`

The two document roles in the first authoring session.

## Values

| Name | Value | Contract |
| --- | ---: | --- |
| [Scene](#scene) | 0 | A typed packed scene. |
| [Code](#code) | 1 | A strict UTF-8 C# source buffer. |

## Enumeration descriptions

### Scene

A typed packed scene.

### Code

A strict UTF-8 C# source buffer.

## Usage and verification

Used by [SessionClient](SessionClient.md) and [SessionResult](SessionResult.md). See the [session example](ProjectSession.md#example) and [public consumer](../../tests/ProjectSession/Program.cs). Undefined document/reconciliation values reject; no enum starts a game loop, UI or transport. See [the component](../components/project-session.md) for owner/failure and platform boundaries.
