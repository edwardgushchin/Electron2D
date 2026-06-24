# Contributing to Electron2D

Thanks for taking the time to improve Electron2D.

Electron2D is an agent-native, cross-platform 2D game engine for .NET. Contributions are welcome when they keep the public behavior documented, tested and reviewable.

## Before You Start

- Check the [GitHub Wiki](https://github.com/edwardgushchin/Electron2D/wiki) and existing issues before opening a new thread.
- Use GitHub Issues for bugs, feature requests and documentation problems.
- Keep reports focused on one problem or proposal.
- Do not include secrets, private keys, tokens, private customer data or production credentials in issues, screenshots, logs or pull requests.

## Development Setup

Install the .NET SDK version required by `global.json`, then build the solution:

```bash
dotnet restore src/Electron2D.sln
dotnet build src/Electron2D.sln -c Release
```

Run focused tests for the area you changed before opening a pull request. CI runs the broader repository checks on pull requests.

## Pull Requests

Good pull requests are small, documented and easy to review.

Before opening a pull request:

- Describe the user-visible behavior or repository-facing contract being changed.
- Update the relevant documentation under `docs/` when behavior, limits or verification rules change.
- Add or update tests when code behavior changes.
- Keep unrelated formatting, renames and cleanup out of the same pull request.
- Confirm that generated files are intentionally updated when they change.

## Documentation

Public documentation should be written for people discovering or using the project. Avoid internal process notes in public-facing files such as `README.md`, GitHub issue templates and release text.

Repository domain documents under `docs/` describe expected behavior, current behavior, limits and verification rules for a specific area.

## Security

Please do not report security problems in public issues. Follow the process in [SECURITY.md](SECURITY.md).

## Code of Conduct

All participation in this repository is covered by the [Code of Conduct](CODE_OF_CONDUCT.md).
