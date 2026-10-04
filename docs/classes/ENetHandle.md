# ENetHandle

Last updated: 2026-10-04

Internal runtime support for [native ENet](../components/enet.md). Source: [ENetNative.cs](../../src/Core/Networking/ENetNative.cs).

ENetNative owns the thirteen private source-generated C entry points and callback preparation. ENetHandle releases native host state through SafeHandle. The two ABI records have a fixed private event/buffer layout; no pointer or backend type enters consumer API.

ENetTests exercises this support through public host/peer/scene API. Native allocator work and foreign/routed/rendered acceptance remain separate.
