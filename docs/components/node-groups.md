# Node group membership

Node owns literal ordinal group identifiers independently of tree membership.
The first AddToGroup chooses persistence; another add leaves that choice intact.
RemoveFromGroup reports whether a member existed. Removal followed by another add
can choose a new policy. Add rejects empty names; empty lookup/removal/tree queries
have no matches. Whitespace, Unicode and underscore-prefixed identifiers survive
storage. Null identifiers reject. GetGroups produces an independent read-only
snapshot in ordinal order; explicit snapshots allocate.

The scene owner or current processing group may access local group metadata,
including physical nodes within its group. Other callers reject; detached callers
coordinate access. Pack capture rejects writes. Physical transforms and lifetime
keep their owner guards, validated before virtual consumer mutation hooks.
Metadata edits retain pose, RID and sleep. Tree queries observe current depth-first
order, including internal children; moving/detaching nodes changes tree results
without changing the node's memberships or old public snapshots. Group calls
retain current eligibility checks, reverse order and deferred membership lookup.

Only persistent memberships enter PackedScene and file archives. The public-only
--node-groups consumer checks both persistence histories and remove/readd, literal
identifiers, snapshots, tree transfer, group calls, wrong-thread/peer rejection,
actual worker read/write on pure and physical nodes, and a validation override.
Each profile reconstructs a physical hierarchy in a fresh process and verifies
exact groups, backend and motion. After warmup, 128 groups of two real intervals
combine worker membership churn, physical steps and moving-body callbacks at
strict zero owner/all-thread managed bytes; sleeping metadata-only participants
stay asleep. Evidence: bin/physics-node-groups/2026-10-16/.

## Runtime duplication dependency

The pinned Node duplication family is nondeprecated. Its default combines Scripts
and UseInstantiation. The pinned implementation creates a packed hierarchy and
then changes scripts on the existing identities. C# CLR subtype replacement cannot
provide this binding while preserving the accepted object identity. ADR 0091 still
requires a typed live-binding/identity contract. Ordinary Pack/Instantiate also
selects children/resources differently from runtime duplication. No duplicate
method, inert flags, script-dropping substitute or completion claim is added by
this group slice. The duplication rows stay open.

Native/driver allocation budgets, foreign-platform execution, complete inherited
Node API, live script binding, networking and hardware/window performance remain
separate acceptance work. GPU verification here uses pinned software Vulkan.
