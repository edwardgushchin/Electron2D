# ENetNative

Last updated: 2026-10-05

Internal runtime support for [native ENet](../components/enet.md). Source: [ENetNative.cs](../../src/Core/Networking/ENetNative.cs).

ENetNative owns the thirteen private source-generated C entry points and callback preparation. ENetHandle releases native host state through SafeHandle. The two ABI records have a fixed private event/buffer layout; no pointer or backend type enters consumer API.

ENetTests exercises this support through public host/peer/scene API. Native allocator work and foreign/routed/rendered acceptance remain separate.

Desktop and Android profiles resolve their target-built private bridge. Android's executable native host verifies a bound public ENet connection; this smoke check does not replace the full desktop wire, codecs, replication or routed tests.

iOS/tvOS statically link the same private bridge with executable-symbol imports and caller-thread callbacks. The Apple native app checks a bound public ENet host during two engine lifecycles; simulator execution remains required and does not replace complete desktop wire/codec tests.
