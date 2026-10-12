# MissingResourceData.Field

Last updated: 2026-10-12

Private immutable field record in [MissingResourceData.cs](../../src/Core/IO/MissingResourceData.cs): original name/schema, scalar/reference/array role, scalar byte order, owned encoded bytes, borrowed resource identities and null-array marker. Resources are re-encoded through the current archive identity table; IDs are not copied as opaque scalar bytes. Copies duplicate bytes and transform references through the existing ownership session.
