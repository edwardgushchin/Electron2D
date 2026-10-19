# Time.Month

Last updated: 2026-10-19

- Declaration: `public enum Time.Month`
- Source: [Time.cs](../../src/Core/OS/Time.cs)
- Component: [Time service](../components/time-service.md)

## Description

The calendar month identity used by the complete typed Time family. Numerical values preserve the calendar domain; out-of-range month input rejects. Weekday input metadata is ignored by conversion.

## Example

```csharp
var parts = Time.GetDateTimeInfoFromUnixTime(0);
// parts.Month is Time.Month.January; parts.Weekday is Time.Weekday.Thursday.
```

## Enumeration summary

| Member | Value |
| --- | --- |
| [`Time.Month.January`](#january) | 1 |
| [`Time.Month.February`](#february) | 2 |
| [`Time.Month.March`](#march) | 3 |
| [`Time.Month.April`](#april) | 4 |
| [`Time.Month.May`](#may) | 5 |
| [`Time.Month.June`](#june) | 6 |
| [`Time.Month.July`](#july) | 7 |
| [`Time.Month.August`](#august) | 8 |
| [`Time.Month.September`](#september) | 9 |
| [`Time.Month.October`](#october) | 10 |
| [`Time.Month.November`](#november) | 11 |
| [`Time.Month.December`](#december) | 12 |

## Enumeration Descriptions

<a id="january"></a>
### January

`public const Time.Month January = 1`

Identifies January with calendar value 1.

<a id="february"></a>
### February

`public const Time.Month February = 2`

Identifies February with calendar value 2.

<a id="march"></a>
### March

`public const Time.Month March = 3`

Identifies March with calendar value 3.

<a id="april"></a>
### April

`public const Time.Month April = 4`

Identifies April with calendar value 4.

<a id="may"></a>
### May

`public const Time.Month May = 5`

Identifies May with calendar value 5.

<a id="june"></a>
### June

`public const Time.Month June = 6`

Identifies June with calendar value 6.

<a id="july"></a>
### July

`public const Time.Month July = 7`

Identifies July with calendar value 7.

<a id="august"></a>
### August

`public const Time.Month August = 8`

Identifies August with calendar value 8.

<a id="september"></a>
### September

`public const Time.Month September = 9`

Identifies September with calendar value 9.

<a id="october"></a>
### October

`public const Time.Month October = 10`

Identifies October with calendar value 10.

<a id="november"></a>
### November

`public const Time.Month November = 11`

Identifies November with calendar value 11.

<a id="december"></a>
### December

`public const Time.Month December = 12`

Identifies December with calendar value 12.
