# Time service

Last updated: 2026-10-19

## Scope and ownership

[Time](../classes/Time.md) supplies calendar/Unix/string conversions, current UTC/local wall time, timezone metadata and monotonic elapsed ticks. It is a process-lifetime ElectronObject with static operations under ADR 0095 and a permanent Engine registry identity. No caller can dispose or unregister it. The service opens no window/device and changes no system clock or timezone.

The copied payloads are [DateInfo](../classes/DateInfo.md), [TimeInfo](../classes/TimeInfo.md), [DateTimeInfo](../classes/DateTimeInfo.md) and [TimeZoneData](../classes/TimeZoneData.md). [Time.Month](../classes/Time.Month.md) and [Time.Weekday](../classes/Time.Weekday.md) preserve their numerical calendar domains. DateTimeInfo expresses omitted input components and optional computed weekday/system DST without a dynamic dictionary.

## Runtime flow and invariants

Signed Unix seconds split into floor-divided days and a nonnegative within-day remainder. Gregorian 400-year cycles locate the astronomical year, followed by its actual month/day. Wide integer intermediates retain exactness across signed 64-bit timestamps and reject overflowing authored dates. Year zero and negative years are valid. Calendar conversion performs no DST/timezone adjustment and excludes leap seconds.

Typed partial components resolve to epoch defaults; entirely empty input rejects. String parsing validates decimal components and actual calendar ranges. Date-only/time-only values use epoch defaults for omitted parts. Decimal seconds fractions are discarded. Formatting uses invariant digits, minimum field widths and T or space separation; timezone suffixes are not interpreted. The pinned empty-dictionary description disagrees with its implementation error path; the typed API explicitly rejects an empty value.

System samples use the managed platform's current local or UTC clock; timezone names, offsets and daylight state come from its timezone data. Wall time is externally adjustable. Stopwatch elapsed ticks instead share one initialization origin, remain independent of game scheduling and use allocation-free integer scaling. Engine bootstrap and direct Time access retain the same object and epoch.

## Integration and current implementation

Engine registers Time permanently alongside its other process services. Registry tests also cover the previously missing NavigationServer unregister guard. An Engine-driven SceneTree consumes Time in ordinary Node process callbacks at TimeScale zero, across pause and Stop, proving that elapsed observation is independent of game deltas. Numeric calendar and ticks have no renderer or physics dependency. String construction intentionally allocates; configuration/clock injection is not part of the public API.

## Verification limits

TimeTests checks all operations, a full 400-year cycle against the independent BCL calendar, signed Unix extrema and 4096 full-width values, year zero/negative years, leap/invalid dates, optional fields, parse errors, invariant cultures, second fractions, offset extrema, system UTC/local samples, fresh-process UTC/Kathmandu/New York timezones, service identity/disposal, concurrent monotonic reads, Engine integration and warmed numeric owner allocations. Engine registry and physics-clock regressions exercise existing scheduler consumers. Current checks are local managed execution, not other platform timezone databases, external wall-clock adjustment, native allocator or owner acceptance. [ADR 0111](../decisions/time.md#adr-0111) owns the decision.
