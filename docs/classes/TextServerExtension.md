# TextServerExtension

Last updated: 2026-10-29

**Inherits:** [TextServer](TextServer.md)

- **Source:** [TextServerExtension.cs](../../src/Servers/Text/TextServerExtension.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class Electron2D.TextServerExtension`

## Description

A caller-owned text implementation. Override the inherited typed protected On-prefixed callbacks; the inherited defaults execute the native backend. Public TextServer methods keep engine admission and identity checks. Override OnGetName to provide a stable implementation name. Register and select through TextServerManager, restore another primary before removal, and release owned identities before disposal. Registration and selection retain this object without transferring caller ownership.

Opaque implementations allocate font and buffer identities through AllocateFontRID and AllocateShapedTextRID. Each font supplies an independent duplication callback. FontGetResource exposes its stable borrowed Font projection to measurement, drawing, retained text objects and GUI consumers. NotifyFontChanged publishes edits made directly to provider data. See the complete [consumer and ownership contract](../components/text-server.md#external-implementations), including callback errors, native fallback, source generations, private buffer retirement and raster-scale subscriptions.

## Example

Complete provider registration/selection lifetime; inherited operations continue using native text.

```csharp
using Electron2D;

var previous = TextServerManager.GetPrimaryInterface();
using var provider = new NamedText();
TextServerManager.AddInterface(provider);
try { TextServerManager.SetPrimaryInterface(provider); }
finally
{
    TextServerManager.SetPrimaryInterface(previous);
    TextServerManager.RemoveInterface(provider);
}

sealed class NamedText : TextServerExtension
{
    protected override string OnGetName() => "Application text";
}
```

## Protected integration

The protected constructor creates an unregistered implementation with independent native fallback ownership. ValidateDisposal rejects registered/selected/active implementations and implementations with owned identities. Dispose balances detached raster-scale subscriptions and completes base cleanup. The callback family, typed allocation helpers and explicit font-change notification are inherited from [TextServer](TextServer.md). No public operation is overridable; typed hooks run outside engine resource/registry monitors.
