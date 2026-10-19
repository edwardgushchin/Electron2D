# DateTimeInfo

Last updated: 2026-10-19

- Declaration: `public readonly struct DateTimeInfo`
- Source: [Time.Values.cs](../../src/Core/OS/Time.Values.cs)
- Component: [Time service](../components/time-service.md)

## Description

A copied typed calendar value with optional authored components and metadata. Inputs default missing components to the Unix epoch and reject an entirely empty value. Weekday/DST inputs are ignored. Time conversions and parsing produce all six calendar components; Weekday is optional for parsing, and DST is present only for system samples. Returned metadata performs no timezone conversion. Initializers may hold invalid values; consuming Time methods validate actual month lengths and clock ranges. These value snapshots need no disposal and are safe to copy or pass between threads.

## Example

```csharp
var leapDay = new DateTimeInfo { Year = 2024, Month = Time.Month.February, Day = 29 };
long seconds = Time.GetUnixTimeFromDateTimeInfo(leapDay);
```

## Property summary

| Signature | Meaning |
| --- | --- |
| [`public long? Year { get; init; }`](#year) | Null defaults to astronomical year 1970. |
| [`public Time.Month? Month { get; init; }`](#month) | Null defaults to January. |
| [`public int? Day { get; init; }`](#day) | Null defaults to day one. |
| [`public int? Hour { get; init; }`](#hour) | Null defaults to zero. |
| [`public int? Minute { get; init; }`](#minute) | Null defaults to zero. |
| [`public int? Second { get; init; }`](#second) | Null defaults to zero; valid seconds are 0..59. |
| [`public Time.Weekday? Weekday { get; init; }`](#weekday) | Absent when not requested, or a calculated weekday. Ignored as input. |
| [`public bool? DST { get; init; }`](#dst) | Absent outside system samples; otherwise daylight-saving state. Ignored as input. |

## Property Descriptions

<a id="year"></a>
### Year

`public long? Year { get; init; }`

Null defaults to astronomical year 1970.

<a id="month"></a>
### Month

`public Time.Month? Month { get; init; }`

Null defaults to January.

<a id="day"></a>
### Day

`public int? Day { get; init; }`

Null defaults to day one.

<a id="hour"></a>
### Hour

`public int? Hour { get; init; }`

Null defaults to zero.

<a id="minute"></a>
### Minute

`public int? Minute { get; init; }`

Null defaults to zero.

<a id="second"></a>
### Second

`public int? Second { get; init; }`

Null defaults to zero; valid seconds are 0..59.

<a id="weekday"></a>
### Weekday

`public Time.Weekday? Weekday { get; init; }`

Absent when not requested, or a calculated weekday. Ignored as input.

<a id="dst"></a>
### DST

`public bool? DST { get; init; }`

Absent outside system samples; otherwise daylight-saving state. Ignored as input.

## Interactions and verification

[Time](Time.md) owns validation, formatting, calendar arithmetic and system observations. TimeTests exercises these values through the public conversion and clock workflows. [ADR 0111](../decisions/time.md#adr-0111) defines the complete typed payload family.
