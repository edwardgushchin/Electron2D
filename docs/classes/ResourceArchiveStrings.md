# ResourceArchiveStrings

Last updated: 2026-10-05

Internal typed file string codec in [ResourceArchiveStrings.cs](../../src/Core/IO/ResourceArchiveStrings.cs).

Writes a bounded Int32 UTF16 code-unit count and UInt16 values in payload byte order. Read validates count against the remaining byte budget before allocation. It preserves embedded NUL, BOM and unpaired CLR surrogates, including resource metadata and stored strings; it does not inherit the separate networking text decoder's termination/replacement semantics.

ResourceArchiveTests verifies exact resource-name roundtrip with these boundary code units under both byte orders and compression. See [resource files](../components/resource-files.md) for schema and verification limits. File work allocates outside the frame path.
