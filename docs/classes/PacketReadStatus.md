# PacketReadStatus

Last updated: 2026-10-04

**Source:** [PacketPeer.cs](../../src/Core/Networking/PacketPeer.cs). **Component:** [Networking](../components/networking.md).

| Name | Value | Contract |
| --- | ---: | --- |
| `Error` | 2 | A read failed for another reason; LastReadException carries details. |
| `OK` | 0 | The read succeeded, or no read has been attempted. |
| `Unavailable` | 1 | No complete packet was available. |
