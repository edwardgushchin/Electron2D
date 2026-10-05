# Security policy

## Supported code

Electron2D is under active development and has no published stable release. Security fixes currently target `main`; older source snapshots have no guaranteed maintenance. Report the exact affected commit and, when practical, check whether the problem is present on current `main`.

## Report a vulnerability privately

Use [GitHub private vulnerability reporting](https://github.com/edwardgushchin/Electron2D/security/advisories/new). Do not disclose exploits in public issues, discussions or pull requests.

Include the affected commit, OS/architecture, build configuration, a minimal proof of concept, reproduction steps, impact and any proposed mitigation. Remove real credentials, personal information and unrelated application data. The repository provides a structured private report form.

Maintainers will assess the report, discuss a fix privately and coordinate disclosure through GitHub security advisories when appropriate. A response or fix date is not guaranteed. There is no announced bug bounty program.

## Scope and deployment

Reports about Electron2D code, its native integrations, resource parsing, networking and packaging are welcome. For an upstream dependency defect, identify the dependency and version so maintainers can coordinate with its project. An application must review its own trust boundaries and deployment configuration; platform verification in this repository does not establish a security audit.
