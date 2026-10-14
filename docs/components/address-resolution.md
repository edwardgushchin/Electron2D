# Address resolution

Last updated: 2026-10-14

## Scope and runtime flow

[IP](../classes/IP.md) adds static operations over one retained Engine service, [IPType](../classes/IPType.md) family selection, [ResolverStatus](../classes/ResolverStatus.md) queue phases and [IPInterfaceInfo](../classes/IPInterfaceInfo.md) typed adapter snapshots. It uses the private .NET native DNS/network-information backend; no platform handles or public backend dependencies escape.

Synchronous lookup checks the exact hostname/family cache before native resolution. Results preserve order, remove duplicate strings and retain IPv6 scope. Queued requests reserve prepared slots, publish Waiting then Done/Error, and remain retained until erase. Generations retire a slot before cancellation so late work cannot overwrite a new request. ClearCache removes existing entries and prevents older work from repopulating removed cache state while leaving queue completion intact. Public blocking DNS failure is empty; transport establishment still requires an address and raises its existing typed failures.

NetworkSockets resolution supplies TCP/UDP/ENet callers through the same retained backend/cache; HTTP keeps its cancellable asynchronous establishment while using that cache. Interface enumeration returns fresh typed field/array snapshots. Ordinary service lookup preserves object identity and rejects disposal/unregistration. Calls are thread safe; worker completion does not mutate a scene.

## Dependencies and invariants

The component depends on ElectronObject lifetime, Engine named-service registration, native .NET DNS/network interfaces and existing sockets. 256 completed/pending queue items are the explicit capacity. Host spelling/family form ordinal keys; success persists until ClearCache and failure is not negatively cached. None/Any use unspecified-family policy. Arrays belong to callers. Public operations are static under ADR 0095; no Instance accessor is added.

## Verification and limits

IPTests checks independent controlled completion/erasure/cache races, 256 occupied slots and immediate reuse, failure/status semantics, snapshot isolation, identity/lifetime, real IPv4/IPv6 localhost and adapter data. Its public Node/SceneTree consumer resolves and completes a native TCP request. NetworkingTests and HTTPTests cover native IPv4/IPv6 TCP/UDP/UDS, HTTP/HTTPS/proxy/scene/worker protocol paths and existing span budgets. 128 prepared cache/queue/status/first-string/erase cycles require zero managed owner bytes; cold DNS/tasks, native allocations, convenience arrays/interface snapshots and routed/foreign/browser hardware behavior remain outside that interval.

This transport capability creates no rendered output. ADRs 0094/0095/0001/0014/0021/0051 define its typed, lifetime and platform gates.

A fresh self-contained public-only NuGet consumer executes host interface snapshots and a queued localhost lookup followed by native TCP request from Node/SceneTree. Its machine-readable result reports passed, sceneTCP=true and queueCapacity=256. It runs from /tmp with LD_LIBRARY_PATH unset; Linux package audits cover 70 ELF files and 78 notices. This is headless native transport execution and does not claim rendering or routed DNS acceptance.
