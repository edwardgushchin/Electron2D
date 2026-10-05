# Contributing to Electron2D

Help improve Electron2D through reproducible reports, documentation, examples and focused changes. First-time contributors are welcome. Follow our [code of conduct](CODE_OF_CONDUCT.md).

## Find the right place

| Contribution | Start here |
| --- | --- |
| Usage question or setup help | [Q&A](https://github.com/edwardgushchin/Electron2D/discussions/categories/q-a) |
| Early idea or design proposal | [Ideas](https://github.com/edwardgushchin/Electron2D/discussions/categories/ideas) |
| Reproducible bug, measured performance issue or documentation correction | [Issue forms](https://github.com/edwardgushchin/Electron2D/issues/new/choose) |
| Security vulnerability | [Private report](https://github.com/edwardgushchin/Electron2D/security/advisories/new) |
| A game, experiment or tutorial | [Discussions](https://github.com/edwardgushchin/Electron2D/discussions) |

Search existing issues and discussions before opening a new one. Use English when possible so more contributors can participate; questions in other languages are welcome. For a substantial change, agree on scope in an issue or discussion before investing in implementation. Maintainers assign priorities and confirm whether a task is suitable for a first contribution; a label is not a promise of a delivery date.

## Prepare your checkout

Follow the [quick start](README.md#quick-start) for the current SDK and native prerequisites. Fork the repository, clone your fork and create a focused branch. Keep unrelated changes out of your pull request.

The runtime lives in `src/`, executable checks in `tests/`, and public-API consumers in `examples/`. Read [AGENTS.md](AGENTS.md), the [maintenance contract](docs/maintaining.md), and relevant [architecture decisions](docs/decisions/index.md). Accepted decisions govern the product; propose decision changes explicitly.

## Make a complete change

- Reuse existing engine contracts and helpers. Keep backend dependencies inside the runtime and its probes.
- For behavior changes, include a regression check that fails for the original defect. Cover ownership, failure handling and affected sibling paths where relevant.
- Update source XML and affected class, component, domain and coverage pages alongside public API or behavior changes. Documentation-only changes do not need invented runtime tests.
- Keep examples on public Electron2D API. Preserve third-party attribution and [license notices](licence/THIRD_PARTY_NOTICES.md).
- State what you actually verified. A local build, headless test, native execution, rendered output and target-device run establish different things.

## Verify before submitting

Run the checks appropriate to your change. For a runtime change, the usual starting point is:

```bash
dotnet build Electron2D.csproj -c Release
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
```

Before every repository commit, the repository also requires:

```bash
dotnet build Electron2D.csproj -c Release
tools/coverage/check.sh
python3 -B tools/wiki/test_generate.py
python3 -B tools/wiki/generate.py
python3 -B tools/wiki/generate.py --check
```

Generated Wiki candidates under `bin/wiki/` stay out of engine commits. Follow [AGENTS.md](AGENTS.md) for formatting and identity checks and [platform verification](docs/platform-verification.md) for native, device and rendered-output gates. Record unavailable prerequisites instead of reporting an unexecuted check as passed.

## Submit a pull request

Use a [Conventional Commit](https://www.conventionalcommits.org/en/v1.0.0/) subject, such as `fix(input): preserve released action state`. Describe the problem, resulting behavior, related issue and exact validation. The PR template helps record these details. Screenshots are useful for visual changes; performance claims need measured results.

Keep the PR focused and respond to review with follow-up commits. Maintainers review correctness, architecture, documentation and verification before merging. See [SUPPORT.md](SUPPORT.md) for help; there is no guaranteed review or support turnaround.
