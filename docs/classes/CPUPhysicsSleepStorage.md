# CPUPhysicsSleepStorage

Last updated: 2026-10-21

**Declaration:** `internal sealed class CPUPhysicsSleepStorage` · **Source:** [CPUPhysicsSleepStorage.cs](../../src/Servers/Physics/CPUPhysicsSleepStorage.cs) · **Component:** [World sleep](../components/physics-sleep.md#pooled-dormant-island-storage)

The CPU world owns this optional storage after a dynamic body first becomes sleep
eligible. Separate body/contact/joint/island pools prepare power-of-two reference
arrays for possible island partitions, with four record objects per budget unit.
A rounded partition uses fewer than two record slots per live unit; preparing a
restored partition while preserving the current one can temporarily require four.
The object supply therefore grows linearly with the budget. Array reference
storage grows with the budget times its logarithm, rather than reserving a whole
world-sized record array for every possible sleeping set.

Preparation uses sleep-capable body capacity, the existing four-contacts-per-body
graph budget, and the native joint identity budget. It runs during authored
capacity changes. Each bucket holds at most the number of sleeping sets and the
combined current/saved partitions that could contain its minimum record count. The arrays contain
references to separately reusable record objects; growing a live destination moves
its active references before returning spare records and the old array.

Natural sleep and sleeping-set merge acquire the required arrays. Wake returns
the cleared dormant arrays and records without changing physical identity.
Checkpoint restore prepares the saved live counts before copying logical state,
then returns inactive destination storage. Pool bookkeeping is capacity state,
not a portable or private numerical checkpoint payload. World disposal releases
the pool and clears the native callbacks.

Unprepared authoring and contact graphs beyond the existing budget retain normal
growth behavior. This preserves correctness but does not certify zero allocation
for every possible dense graph. The [sleep report](../components/physics-sleep.md#pooled-dormant-island-storage)
records the executed fixture and the remaining acceptance boundaries.
