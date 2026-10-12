# MissingResourceData

Last updated: 2026-10-12

Internal implementation in [MissingResourceData.cs](../../src/Core/IO/MissingResourceData.cs). Holds original factory identity, recording state and bounded immutable field records. Each field retains original schema, role and byte order, copied scalar bytes or actual borrowed resource references. Exact-schema replacement commits after validation. Duplicate/remap operations use the existing Resource graph session, preserving aliases and cycles. Metadata descriptors expose copied scalar bytes and typed base-resource identities without CLR member dispatch.

See [missing resource recovery](../components/resource-files.md#missing-resource-recovery) for budgets, version boundaries, ownership and actual checks.
