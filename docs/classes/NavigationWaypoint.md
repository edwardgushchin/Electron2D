# NavigationWaypoint

Last updated: 2026-10-07

- Source: [NavigationWaypoint.cs](../../src/Navigation/2D/NavigationWaypoint.cs)
- Component: [Navigation maps and agents](../components/navigation-maps.md#agent-path-following)

## Description

Immutable value details delivered by NavigationAgent waypoint/link events. Position is mandatory; query-selected Type, RID and OwnerID are nullable. Live scene owners resolve through weak navigation registrations; unknown custom IDs retain their identity with null Owner. Link endpoints describe traversal direction when a live NavigationLink owner is available.

## Properties

| Member | Contract |
| --- | --- |
| [`public Nullable<Vector2> LinkEntryPosition { get; set; }`](#linkentryposition) | Gets the link endpoint nearest this waypoint. |
| [`public Nullable<Vector2> LinkExitPosition { get; set; }`](#linkexitposition) | Gets the opposite link endpoint. |
| [`public ElectronObject Owner { get; set; }`](#owner) | Gets the live scene navigation owner. |
| [`public Nullable<ulong> OwnerID { get; set; }`](#ownerid) | Gets the optional logical owner identity. |
| [`public Vector2 Position { get; set; }`](#position) | Gets the finite world-space waypoint. |
| [`public Nullable<RID> RID { get; set; }`](#rid) | Gets the optional owning primitive identity. |
| [`public Nullable<NavigationPathQueryResult.PathSegmentType> Type { get; set; }`](#type) | Gets the optional owning primitive kind. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public virtual bool Equals(NavigationWaypoint other)`](#equals) | Compiler-generated typed value equality, hash or display contract. |
| [`public override bool Equals(System.Object obj)`](#equals) | Compiler-generated typed value equality, hash or display contract. |
| [`public override int GetHashCode()`](#gethashcode) | Compiler-generated typed value equality, hash or display contract. |
| [`public override string ToString()`](#tostring) | Compiler-generated typed value equality, hash or display contract. |

## Operators

| Member | Contract |
| --- | --- |
| [`public static bool op_Equality(NavigationWaypoint left, NavigationWaypoint right)`](#op_equality) | Compiler-generated typed value equality, hash or display contract. |
| [`public static bool op_Inequality(NavigationWaypoint left, NavigationWaypoint right)`](#op_inequality) | Compiler-generated typed value equality, hash or display contract. |

## Member descriptions

<a id="linkentryposition"></a>
### `public Nullable<Vector2> LinkEntryPosition { get; set; }`

Gets the link endpoint nearest this waypoint. Null unless type and live NavigationLink owner metadata are available.

<a id="linkexitposition"></a>
### `public Nullable<Vector2> LinkExitPosition { get; set; }`

Gets the opposite link endpoint. Null unless type and live NavigationLink owner metadata are available.

<a id="owner"></a>
### `public ElectronObject Owner { get; set; }`

Gets the live scene navigation owner. Null for omitted, unknown or disposed owners.

<a id="ownerid"></a>
### `public Nullable<ulong> OwnerID { get; set; }`

Gets the optional logical owner identity. Null when owner metadata was omitted; zero denotes no assigned owner.

<a id="position"></a>
### `public Vector2 Position { get; set; }`

Gets the finite world-space waypoint. Path position at delivery.

<a id="rid"></a>
### `public Nullable<RID> RID { get; set; }`

Gets the optional owning primitive identity. Null when RID metadata was omitted.

<a id="type"></a>
### `public Nullable<NavigationPathQueryResult.PathSegmentType> Type { get; set; }`

Gets the optional owning primitive kind. Null when type metadata was omitted.

<a id="equals"></a>
### `public virtual bool Equals(NavigationWaypoint other)`

Compiler-generated typed value equality, hash or display contract.

<a id="equals"></a>
### `public override bool Equals(System.Object obj)`

Compiler-generated typed value equality, hash or display contract.

<a id="gethashcode"></a>
### `public override int GetHashCode()`

Compiler-generated typed value equality, hash or display contract.

<a id="tostring"></a>
### `public override string ToString()`

Compiler-generated typed value equality, hash or display contract.

<a id="op_equality"></a>
### `public static bool op_Equality(NavigationWaypoint left, NavigationWaypoint right)`

Compiler-generated typed value equality, hash or display contract.

<a id="op_inequality"></a>
### `public static bool op_Inequality(NavigationWaypoint left, NavigationWaypoint right)`

Compiler-generated typed value equality, hash or display contract.

## Verification

[NavigationAgentTests](../../tests/Electron2D.Tests/NavigationAgentTests.cs) verifies managed progression, source persistence in a fresh process, metadata-driven native link actions and target pixels. See the component contract for tested backend and allocation limits.
