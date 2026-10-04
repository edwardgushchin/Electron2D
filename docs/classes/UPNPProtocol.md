# UPNPProtocol

Last updated: 2026-10-04

Internal bounded description/HTTP/SOAP implementation; [source](../../src/Core/Networking/UPNPProtocol.cs). Reuses HTTPClient and private StreamPeerTCP local endpoint access. One-MiB/depth-64 XML parsing prohibits DTD/external resolution. Five-second polled exchanges, exact SOAP response namespaces/actions, deterministic fault projection and per-device service fallback are documented in [UPNP gateway control](../components/upnp.md). No public backend or arbitrary action API is exposed.
