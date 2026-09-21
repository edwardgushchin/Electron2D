# InputEventGesture

Last updated: 2026-09-21

- Source/declaration: [`InputEventTouch.cs`](../../src/Core/Input/InputEventTouch.cs), `public abstract class InputEventGesture : InputEventWithModifiers`.
- Responsibility: common finite local `Position` plus modifier/window/device data for multi-touch gestures; constructor changes the inherited keyboard default device to touch device zero.
- Complete declared API: protected constructor; `Position`; protected overrides `CopyEventStateTo` and `GetPropertyDescriptors`. `Position` is a stored typed descriptor.
- Lifecycle/threading: caller-owned mutable Resource; disposed/non-finite access fails; no internal synchronization.
- Verification: concrete magnify/pan copy and transform tests exercise the base contract.
