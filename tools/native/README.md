# Electron2D platform packages

Install `Electron2D` for the managed C# runtime, then add `Electron2D.Windows`, `Electron2D.Linux`, `Electron2D.MacOS`, `Electron2D.Android`, `Electron2D.iOS`, `Electron2D.tvOS` or `Electron2D.Web` for your game targets. Each platform package contains its audited native files and selects the matching SDL, image, shader and font dependencies. It contains no managed backend assembly.

For example, a Linux game uses:

```bash
dotnet add package Electron2D --prerelease
dotnet add package Electron2D.Linux --prerelease
```

The platform package version is the minimum supported Electron2D version, including its prerelease suffix. `Electron2D.Linux` version `0.1.0-alpha.1` therefore requires `Electron2D` version `0.1.0-alpha.1` or later. NuGet records and enforces that minimum dependency; a version of SDL, FreeType or OpenSSL is not the version of the Electron2D platform package. Use matching product/platform release versions unless a different combination has been verified.

Publish for a specific RID to include only its native assets under `runtimes/<RID>/native`. Platform package installation does not establish target execution or complete game hosting; refer to the repository platform report for current verification limits. Native archives for Apple and Web still follow their SDK's static-link rules.

Native producers keep source receipts, binary SHA-256 values, architecture/loader/export checks and bundled notices. The maintainer source-build mode and CI prepare packages; game developers do not compile the native backends.
