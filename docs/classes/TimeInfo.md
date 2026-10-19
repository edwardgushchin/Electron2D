# TimeInfo

Last updated: 2026-10-19

- Declaration: `public readonly struct TimeInfo`
- Source: [Time.Values.cs](../../src/Core/OS/Time.Values.cs)
- Component: [Time service](../components/time-service.md)

## Description

An immutable whole-second clock query result, without timezone or date. Negative Unix input is reduced to the nonnegative time within its floor-divided day. Leap seconds are absent. These value snapshots need no disposal and are safe to copy or pass between threads.

## Example

```csharp
var clock = Time.GetTimeInfoFromUnixTime(-1); // 23:59:59.
```

## Property summary

| Signature | Meaning |
| --- | --- |
| [`public int Hour { get; }`](#hour) | Hour from zero through 23. |
| [`public int Minute { get; }`](#minute) | Minute from zero through 59. |
| [`public int Second { get; }`](#second) | Second from zero through 59. |

## Property Descriptions

<a id="hour"></a>
### Hour

`public int Hour { get; }`

Hour from zero through 23.

<a id="minute"></a>
### Minute

`public int Minute { get; }`

Minute from zero through 59.

<a id="second"></a>
### Second

`public int Second { get; }`

Second from zero through 59.

## Interactions and verification

[Time](Time.md) owns validation, formatting, calendar arithmetic and system observations. TimeTests exercises these values through the public conversion and clock workflows. [ADR 0111](../decisions/time.md#adr-0111) defines the complete typed payload family.
