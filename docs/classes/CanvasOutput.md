# CanvasOutput

Last updated: 2026-10-27

- Declaration: `internal readonly record struct CanvasOutput(bool Available, bool Supported, bool Enabled, float Reference, float Maximum)`
- Source: [CanvasOutput.cs](../../src/Servers/Rendering/CanvasOutput.cs)
- Component: [Native HDR output](../components/native-hdr-output.md)
- Visibility: internal; unavailable to engine consumers.

A committed per-window capability/output snapshot. Available describes native GPU
composition formats; Supported additionally requires a claimed window with native HDR headroom.
Enabled applies the request. Reference and Maximum hold current proportional nits
only for active HDR. MaxLinear returns their ratio or one for SDR. It owns no
native resource and carries no untyped data or public backend identity.
