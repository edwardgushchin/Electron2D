# TextServerFallback

Last updated: 2026-10-29

**Inherits:** [TextServerExtension](TextServerExtension.md)

- **Source:** [TextServerFallback.cs](../../src/Servers/Text/TextServerFallback.cs)
- **Declaration:** `public sealed class Electron2D.TextServerFallback`

Nominal scalar LTR text implementation using the real shared dynamic/bitmap/MSDF raster sources; contextual substitutions and BIDI reordering are unavailable. See the [concrete profile contract](../components/text-server.md#concrete-implementation-profiles) for exact defaults, feature differences, failure results, lifetime and verification limits.

## Construction and lifetime

`new TextServerFallback()` creates an independent unregistered implementation. Register/select it through TextServerManager. The initial registry already holds one instance of each concrete implementation. Selection/registration retains the object; restore another primary, remove its registration and release operational owned identities before disposal. Inherited public methods retain engine admission and delegate through the concrete protected callbacks. The disabled profile's empty factories do not weaken external-provider allocation rules.

## API

The public operation family is inherited from [TextServer](TextServer.md). The public parameterless constructor and protected implementation callbacks add no second service singleton or platform-specific public API. ProjectSettings.TextDriver can select a registered instance by its exact name at engine startup.
