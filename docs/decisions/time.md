# Time service and calendar values

<a id="adr-0111"></a>
## ADR 0111: Typed calendar conversions and independent system clocks

Last updated: 2026-10-19

Status: Accepted. Follows [0001](product.md#adr-0001), [0003](core-object-runtime.md#adr-0003), [0004](product.md#adr-0004), [0016](core-object-runtime.md#adr-0016), [0045](product.md#adr-0045), [0051](enum-identities.md#adr-0051) and [0095](singleton-services.md#adr-0095).

### Decision

Time is a retained process-lifetime ElectronObject with static public operations and a permanent Engine registry identity. It requires neither a window nor a running loop. Calendar, system wall-clock and elapsed monotonic time are separate observations; Engine.TimeScale, scene pause and loop restart do not change or reset the service clock.

The complete applicable reference Time family is preserved. Dictionary payloads become DateInfo, TimeInfo, DateTimeInfo and TimeZoneData. The date/time dictionary method-name word becomes Info across the whole family: GetDateInfoFromSystem, GetDateTimeInfoFromUnixTime, GetDateTimeStringFromDateTimeInfo and their siblings. DateTimeInfo has nullable authored components to retain missing-key defaults, optional computed Weekday and system-only DST. Omitted calendar components resolve to the Unix epoch; an entirely empty input is invalid, matching the pinned implementation's error path rather than its inconsistent empty-dictionary prose. Weekday and DST input metadata do not perform timezone conversion. Date-only and time-only strings resolve missing components to the epoch too. Month and Weekday remain nested Time enums; no other current engine enum owns these domains. Acronyms use UTC, DST, MSEC and USEC.

Conversions use the proleptic Gregorian calendar, astronomical year numbering including zero/negative years, signed 64-bit Unix seconds and invariant decimal strings. Integer calendar arithmetic preserves exact negative floor/remainder semantics and the full Unix range without double rounding or DateTime's year 1..9999 bound. Wider intermediate arithmetic rejects unrepresentable Unix results rather than wrapping. Date/time components validate actual month lengths and exclude leap seconds. String input accepts a date, time or their T/space-separated combination; decimal second fractions are discarded. It does not silently apply timezone suffixes or DST. Malformed strings, empty inputs and invalid values raise typed exceptions; permissive malformed-token coercion is not a public parsing contract. Formatting uses at least four year characters including a negative sign, following the pinned numeric width.

System queries sample .NET's current UTC/local clock and actual local timezone. DateTimeInfo.DST is present only for system samples; UTC reports false. TimeZoneData contains the current UTC offset in minutes and the platform's standard/daylight display name. Wall time may be adjusted externally. GetTicksMSEC/GetTicksUSEC instead measure Stopwatch elapsed time since retained service initialization, independently of wall time. Both use unsigned 64-bit units and allocation-free integer scaling. Static initialization establishes the same identity and epoch through direct Time access or Engine registry bootstrap.

### Verification boundary

Exercise all method families, Gregorian cycles and epoch roundtrips, signed/extreme Unix seconds, partial components, invalid domains, invariant parsing/formatting, fractional seconds, offsets, current UTC/local/timezone observations, registry/disposal, cross-thread monotonic reads and warmed allocation. Use the actual service in Engine-driven scene callbacks to verify pause/time-scale/stop independence. Local managed checks do not establish other platform timezone databases, external wall-clock adjustment, native allocator, physical display or owner acceptance.
