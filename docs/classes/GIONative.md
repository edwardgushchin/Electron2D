# GIONative

Last updated: 2026-10-26

- Declaration: `internal static unsafe partial class GIONative`
- Source: [GIONative.cs](../../src/Servers/Accessibility/GIONative.cs)
- Component: [Native accessibility](../components/accessibility.md)

Private LibraryImport boundary for optional Linux GIO, GLib and GObject. VTable mirrors three callback pointers plus eight ABI padding slots; Error mirrors GError's domain, code and message pointer. No native identity enters the public API.

Tuple/array helpers consume floating GVariant child references; returned variants transfer to GIO replies or signals. Temporary type and builder references are released in finally blocks. Parameter readers own/unref their child variants. Error conversion copies and frees GError; property failures allocate an owned native D-Bus GError for the caller. Small fixed tuples use ReadOnlySpan/nint stack storage; variable native query builders are cold. Native allocator traffic is independent of managed allocation measurements.
