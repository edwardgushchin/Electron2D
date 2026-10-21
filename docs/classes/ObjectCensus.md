# ObjectCensus

Last updated: 2026-10-21

**Declaration:** internal static class ObjectCensus. **Source:** [ObjectCensus.cs](../../src/Core/Object/ObjectCensus.cs). **Component:** [Object lifetime monitors](../components/object-lifetime-monitors.md).

Owns weak ElectronObject registrations for the retained Performance service. Register appends the shared identity and periodically compacts collected or committed identities; Count scans and compacts under the same lock. No target is strongly retained between calls. The captured Engine SceneTree identity is read before taking the registry lock, so service type initialization cannot invert a constructor's registration lock. No derived callback executes under this lock.

Count distinguishes all objects, attached nodes and nodes outside the attached tree. Resource cache counts remain in Resource under its path lock. Concurrent transitions give nearby observations rather than a transaction across selectors. Scans allocate no managed memory and never force collection or native initialization. Construction is allocating cold work; retained list capacity follows its high-water mark and is reused. Public-only lifecycle, weak ownership, concurrency, allocation and native checks are linked from the component.
