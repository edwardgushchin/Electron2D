# WaterPlayground

A resizable pastel water scene. A stream enters from above the screen and gradually releases 65,536 simulated fluid particles, then a rubber duck and sailboat fall into the basin. A translucent surface follows the solved particles, without a separate waterline painted over objects.

```sh
dotnet run --project examples/WaterPlayground -c Release
```

Drag toys to lift them, or stir the water. **H** hides or restores the whole interface; hidden buttons do not intercept the mouse. **Space** pauses, **R** refills, **F11** toggles borderless fullscreen. **CPU/GPU** buttons or **Tab** switch liquid computation without resetting it. `--cpu` starts with CPU fluid; `--compatibility` independently selects the compatibility renderer.

The bottom toolbar toggles a hollow bucket, a water wheel with a lift, a movable gate, a wooden block, a steel ball and a buoyant ball. Press a toy button to create its object; press it again to remove it, including while paused. The wheel and lift disappear together. Reset clears all toy buttons. **Q/E** or the mouse wheel tilts a held object; holding the wheel brakes it. Scoop and pour water, raise the gate to release a wave, or load the boat.

Drag the top handle to move the faucet. With no toy held, **Q/E** aims the jet and the mouse wheel adjusts flow. **F** cycles flow; **D** toggles the bottom drain. The reservoir is finite: after the initial pour, open the drain to recirculate water through the inlet.

The world always measures 1152×800 units and contains about 3.07 m³ after the eight-second pour. Resizing scales the view uniformly to the window width and anchors the bottom. Shapes keep their proportions, and existing positions and mass stay unchanged. Water and toys enter above the visible top and may pass through it; the sides and bottom remain closed.

Both fluid modes use the same particle population and numerical passes, and exchange impulses with CPU PhysicsServer bodies. This is an approximate two-dimensional particle liquid; it does not simulate air or three-dimensional turbulence. CPU mode is a comparison/compatibility path and is substantially slower at 65,536 particles. Rendering is capped at 144 FPS with VSync disabled where the platform supports it.
