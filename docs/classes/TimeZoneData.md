# TimeZoneData

Last updated: 2026-10-19

- Declaration: `public readonly struct TimeZoneData`
- Source: [Time.Values.cs](../../src/Core/OS/Time.Values.cs)
- Component: [Time service](../components/time-service.md)

## Description

An immutable current local timezone sample. The platform supplies its standard/daylight display name and current signed UTC offset, including daylight adjustment. The value does not own or change timezone settings. These value snapshots need no disposal and are safe to copy or pass between threads.

## Example

```csharp
var zone = Time.GetTimeZoneFromSystem();
string offset = Time.GetOffsetStringFromOffsetMinutes(zone.Bias);
```

## Property summary

| Signature | Meaning |
| --- | --- |
| [`public string Name { get; }`](#name) | Platform display name; empty in a default value. |
| [`public int Bias { get; }`](#bias) | Current signed offset east of UTC in whole minutes. |

## Property Descriptions

<a id="name"></a>
### Name

`public string Name { get; }`

Platform display name; empty in a default value.

<a id="bias"></a>
### Bias

`public int Bias { get; }`

Current signed offset east of UTC in whole minutes.

## Interactions and verification

[Time](Time.md) owns validation, formatting, calendar arithmetic and system observations. TimeTests exercises these values through the public conversion and clock workflows. [ADR 0111](../decisions/time.md#adr-0111) defines the complete typed payload family.
