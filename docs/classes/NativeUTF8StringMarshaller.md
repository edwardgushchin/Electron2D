# NativeUTF8StringMarshaller

Internal temporary input-string owner in [NativeUTF8StringMarshaller.cs](../../src/Core/Config/NativeUTF8StringMarshaller.cs). Its nested `In` ref struct supplies source-generated calls with 256 bytes of pinned caller scratch, using the common native allocation ledger only when the encoded bytes plus terminator exceed that scratch. `Allocate` supplies explicit caller-owned pointer arrays with the same encoding and ledger.

Only managed-to-native input marshalling is registered. Returned/borrowed provider strings are not freed through this owner. Null, empty, embedded null and replacement of unpaired UTF16 surrogates retain the existing encoding behavior. Generated cleanup retires heap ownership on return, argument-preparation failure or native entry-point failure; explicit pointers require the caller's finally block.

The [input allocation domain](../coverage/performance-producers.md#utf8-input-marshalling-domain-2026-10-29) owns scope, refusal/retry, error preservation, native identity and verification limits. This internal family adds no public declaration and does not complete the composed memory monitors.
