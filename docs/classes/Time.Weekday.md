# Time.Weekday

Last updated: 2026-10-19

- Declaration: `public enum Time.Weekday`
- Source: [Time.cs](../../src/Core/OS/Time.cs)
- Component: [Time service](../components/time-service.md)

## Description

The calendar weekday identity used by the complete typed Time family. Numerical values preserve the calendar domain; out-of-range month input rejects. Weekday input metadata is ignored by conversion.

## Example

```csharp
var parts = Time.GetDateTimeInfoFromUnixTime(0);
// parts.Month is Time.Month.January; parts.Weekday is Time.Weekday.Thursday.
```

## Enumeration summary

| Member | Value |
| --- | --- |
| [`Time.Weekday.Sunday`](#sunday) | 0 |
| [`Time.Weekday.Monday`](#monday) | 1 |
| [`Time.Weekday.Tuesday`](#tuesday) | 2 |
| [`Time.Weekday.Wednesday`](#wednesday) | 3 |
| [`Time.Weekday.Thursday`](#thursday) | 4 |
| [`Time.Weekday.Friday`](#friday) | 5 |
| [`Time.Weekday.Saturday`](#saturday) | 6 |

## Enumeration Descriptions

<a id="sunday"></a>
### Sunday

`public const Time.Weekday Sunday = 0`

Identifies Sunday with calendar value 0.

<a id="monday"></a>
### Monday

`public const Time.Weekday Monday = 1`

Identifies Monday with calendar value 1.

<a id="tuesday"></a>
### Tuesday

`public const Time.Weekday Tuesday = 2`

Identifies Tuesday with calendar value 2.

<a id="wednesday"></a>
### Wednesday

`public const Time.Weekday Wednesday = 3`

Identifies Wednesday with calendar value 3.

<a id="thursday"></a>
### Thursday

`public const Time.Weekday Thursday = 4`

Identifies Thursday with calendar value 4.

<a id="friday"></a>
### Friday

`public const Time.Weekday Friday = 5`

Identifies Friday with calendar value 5.

<a id="saturday"></a>
### Saturday

`public const Time.Weekday Saturday = 6`

Identifies Saturday with calendar value 6.
