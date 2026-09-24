# JoypadInfo

Last updated: 2026-09-24

**Namespace:** `Electron2D`

**Declaration:** `public readonly struct JoypadInfo`

**Source:** [JoypadInfo.cs](../../src/Core/Input/JoypadInfo.cs)

**Used by:** [Input.GetJoyInfo](Input.md)

## Description

Immutable typed projection of the native controller information returned by `Input.GetJoyInfo`. It reports the raw operating-system name before mapping, USB vendor and product IDs, and an optional serial number. Steam Input and XInput indices are not available in this projection. An absent controller yields `null` from the query.

## Example

```csharp
foreach (var device in Input.Instance.GetConnectedJoypads())
{
    if (Input.Instance.GetJoyInfo(device) is { } info)
        Console.WriteLine($"{info.RawName}: {info.VendorID:X4}/{info.ProductID:X4}");
}
```

## Properties

| Member | Contract |
| --- | --- |
| `public string RawName { get; }` | Native name before gamepad mapping. |
| `public ushort VendorID { get; }` | USB vendor ID, zero when unavailable. |
| `public ushort ProductID { get; }` | USB product ID, zero when unavailable. |
| `public string? SerialNumber { get; }` | Native serial when provided, otherwise null. |

## Verification and limits

[InputGamepadNativeTests](../../tests/Electron2D.Tests/InputGamepadNativeTests.cs) checks mapped and raw virtual SDL devices on dummy and Linux Wayland, including vendor/product values. Physical hardware, optional serial values and other platforms remain unverified. The parent [Input coverage](../coverage/classes/Input.md) keeps `GetJoyInfo` Partial until platform-specific fields and values are audited.
