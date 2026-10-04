# ENetTransport

Last updated: 2026-10-04

Internal runtime support for [native ENet](../components/enet.md). Source: [ENetTransport.cs](../../src/Core/Networking/ENetTransport.cs).

ENetTransport owns native .NET UDP sockets or the existing UDPServer/DTLSServer packet layer. Weak callback registration avoids rooting abandoned owners. Prepared endpoint routing tokens retain IP/scope/port addresses, capped/reclaimed census and cached socket addresses. ENetEndpoint retains a prepared address and optional owned UDP/DTLS session. Callbacks capture managed exceptions instead of unwinding native frames.

ENetTests exercises this support through public host/peer/scene API. Native allocator work and foreign/routed/rendered acceptance remain separate.
