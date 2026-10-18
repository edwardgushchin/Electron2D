# Device sensor values

Last updated: 2026-10-18

## Selection and scope

| Candidate | Useful public surface and dependent slices | Feasibility |
| --- | --- | --- |
| Manual device values | Eight methods; device-free motion debugging and four producer endpoints for native sensor acquisition | Selected: retained Input state and numeric Vector3 already exist. Complete manual input can drive an ordinary scene on current backends. |
| Live Script association | Three binding roles, then eight Node duplication declarations | Needs the typed owner identity/lifetime/replacement/failure contract recorded in ADR 0091. |
| Low-processor rendering | Two OS controls and RenderingServer.HasChanged; one idle scheduler | Needs complete renderer invalidation and time-dependent consumers before suppressing work. |

The slice owns Input.Get/SetAccelerometer, Get/SetGravity, Get/SetGyroscope and Get/SetMagnetometer. Their implementation is [Input.DeviceSensors.cs](../../src/Core/Input/Input.DeviceSensors.cs). It adds no public supporting type, native library or independent service. Static operations delegate to the retained Input object under ADR 0095; Vector3 is the numeric data tuple accepted by ADR 0033.

## State and execution

Four independent Vector3 fields initially contain zero. Each getter returns a copy and each setter copies its argument under the existing Input lock. Complete vectors are serialized on any thread, including while an Engine loop runs; four separate channel calls are not one transaction. Changing a caller's value or a returned value does not change retained state.

Acceleration and gravity use metres per second squared, gyroscope uses device-axis radians per second and magnetic field uses microteslas. Components are retained exactly, including signed zero, finite extremes, infinity and NaN test payloads. There is no coordinate conversion, gravity normalization, calibration or filtering in this raw simulation path. Consumers interpret the values they request; they must handle non-finite test inputs when using them for scene math.

Calls do not construct a display, rendering service, controller or input event. Scene callbacks poll the values normally. Window focus and controller sampling do not supply these fields. Engine stop/restart retains the last values; resetting them is an explicit setter call. Per-controller motion remains independently owned by [the controller adapter](gamepad-motion.md).

The pinned Input.xml setters explicitly support debugging without hardware, and input.cpp retains synchronized numeric fields at lines 753–799 and 1335–1356 of revision ed1daf0bf001b61586d9930840f2f1394092c079. This source resolves the broad getter documentation's desktop-zero wording: zero is the initial absent-producer result, while an explicit manual value remains readable. Neither the owner nor these eight declarations is deprecated.

## Example

These calls work before opening a window. An ordinary Entity callback can project the acceleration X/Y components into a game-specific control vector and use gyro Z for planar rotation; the engine does not turn sensor acceleration into an implicit physical force.

```csharp
Input.SetAccelerometer(new Vector3(.25f, -.5f, 0));
Input.SetGravity(new Vector3(0, -9.80665f, 0));
Input.SetGyroscope(new Vector3(0, 0, .2f));
Input.SetMagnetometer(new Vector3(20, -5, 30));
Vector3 tilt = Input.GetAccelerometer();
Vector3 gravity = Input.GetGravity();
Vector3 rotationRate = Input.GetGyroscope();
Vector3 magneticField = Input.GetMagnetometer();
```

## Verification and remaining delivery

DeviceSensorContract is shared by the root checks and the public portability consumer. It exercises all eight operations, channel isolation, copied values, exact component bits, concurrent whole-vector snapshots and a real Engine/SceneTree/Entity consumer. It checks writes from another thread while the Engine runs, stop/restart retention, no synthesized input callbacks and prepared active/idle frame allocations. Linux Release root checks, the public JIT consumer and the strict NativeAOT public consumer pass. Each executes 256 prepared active and 256 idle Engine/SceneTree/Entity frames at 0/0 owner/all-thread managed bytes. NativeAOT executes without CoreCLR. Existing static-service checks, the full managed Input check, and managed/numerical plus native SDL controller-motion regression profiles also pass. Native acquisition and rendered pixels are not established by these headless scene frames. The engine-wide full suite was not repeated for this additive retained-value slice.

The four setters implement the complete manual contract. The four getters remain Partial: actual automatic Android/iOS acquisition requires the owning host adapter, capability and project-setting/permission handling, sensor selection, delivery and retirement. Accelerometer, gravity, gyroscope and magnetometer must enter that first complete native mobile slice; controller channels cannot stand in for device sensors. No native mobile acquisition or physical-device accuracy is established by manual simulation. Foreign execution, native allocator totals, real rendering and human acceptance remain separate verification boundaries.
