# ResourceFieldRole

Last updated: 2026-10-12

Internal byte-domain in [MissingResourceData.cs](../../src/Core/IO/MissingResourceData.cs). Scalar, Resource, ResourceArray and Node identify the existing portable descriptor encodings in version-2 archives. Known descriptors validate their exact role before decoding; missing resources support the first three and reject Node references. This is wire metadata, not a public universal value type.
