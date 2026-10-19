# DateInfo

Last updated: 2026-10-19

- Declaration: `public readonly struct DateInfo`
- Source: [Time.Values.cs](../../src/Core/OS/Time.Values.cs)
- Component: [Time service](../components/time-service.md)

## Description

A complete immutable Gregorian date query result. Year uses astronomical numbering; Month/Day and Weekday describe the same day. Values returned by Time are valid; a default struct is not an authored date. These value snapshots need no disposal and are safe to copy or pass between threads.

## Example

```csharp
var date = Time.GetDateInfoFromUnixTime(0); // 1970-01-01, Thursday.
```

## Property summary

| Signature | Meaning |
| --- | --- |
| [`public long Year { get; }`](#year) | Signed astronomical year, including zero and negative years. |
| [`public Time.Month Month { get; }`](#month) | One-based January through December. |
| [`public int Day { get; }`](#day) | One-based day within the actual month length. |
| [`public Time.Weekday Weekday { get; }`](#weekday) | Sunday numbered zero through Saturday six. |

## Property Descriptions

<a id="year"></a>
### Year

`public long Year { get; }`

Signed astronomical year, including zero and negative years.

<a id="month"></a>
### Month

`public Time.Month Month { get; }`

One-based January through December.

<a id="day"></a>
### Day

`public int Day { get; }`

One-based day within the actual month length.

<a id="weekday"></a>
### Weekday

`public Time.Weekday Weekday { get; }`

Sunday numbered zero through Saturday six.

## Interactions and verification

[Time](Time.md) owns validation, formatting, calendar arithmetic and system observations. TimeTests exercises these values through the public conversion and clock workflows. [ADR 0111](../decisions/time.md#adr-0111) defines the complete typed payload family.
