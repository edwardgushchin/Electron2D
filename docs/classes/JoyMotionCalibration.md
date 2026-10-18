# JoyMotionCalibration

Last updated: 2026-10-18

**Namespace:** `Electron2D`

**Declaration:** `public readonly struct JoyMotionCalibration`

**Source:** [JoyMotionCalibration.cs](../../src/Core/Input/JoyMotionCalibration.cs)

**Used by:** [Input](Input.md), [controller motion](../components/gamepad-motion.md)

## Description

Independent typed controller calibration data. GyroscopeOffset stores the stationary pitch, yaw and roll bias in radians per second. Default construction represents zero bias. The Input setter validates finite, usable components before changing a controller; changing a returned Vector3 copy cannot modify this value or live calibration.

## Constructors

| Member | Contract |
| --- | --- |
| `public JoyMotionCalibration(Vector3 gyroscopeOffset)` | Creates a calibration value containing the supplied stationary angular bias. |

## Properties

| Member | Contract |
| --- | --- |
| `public Vector3 GyroscopeOffset { get; }` | Independent pitch, yaw and roll offset in radians per second. |

## Lifecycle and use

Input.GetJoyMotionSensorsCalibration returns null until calibration has completed or data has been explicitly restored. Input.SetJoyMotionSensorsCalibration(device, calibration) restores data on the owner thread when calibration is not running. Bias is applied to the next sampled angular rate. The value borrows no resource and needs no disposal. Sensor and controller lifetimes are owned by the display.

## Verification and limits

The shared public JoyMotionContract checks copying, zero/default and numeric rejection on managed and native compilation paths as they are exercised. JoyMotionNativeTests verifies actual native bias acquisition and restoration. The component records completed profiles and outstanding target-device checks.
