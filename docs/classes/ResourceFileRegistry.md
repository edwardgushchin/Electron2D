# ResourceFileRegistry

Last updated: 2026-10-11

Internal retained schema state in [ResourceFileTypes.cs](../../src/Core/IO/ResourceFileTypes.cs), owned by ResourceLoader's permanent runtime object. It owns the synchronized stable-ID/type factory and typed value/resource-array codec dictionaries used by the static ResourceFileTypes registration helpers.

Registrations are immutable allocating setup; metadata lookup never loads an assembly or invokes reflected members. ResourceArchiveTests exercises registration and file reconstruction. See [resource files](../components/resource-files.md) and [ADR 0095](../decisions/singleton-services.md#adr-0095). No independent public service instance or native ownership is introduced.

The registry additionally owns synchronized direct per-T TileCustomDataType factories. Built-in codec initialization precedes first lookup/construction, including cold generic schema edits; no reflected generic construction is used.
