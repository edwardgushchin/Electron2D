# LightShadowFilter

Last updated: 2026-11-06

- Declaration: `public enum LightShadowFilter`
- Source: [LightShadowFilter](../../src/Scene/2D/Light.cs)
- Component: [Canvas lights and occlusion](../components/canvas-lights.md)

Shared typed selectors retain their numeric contracts. A Max member, where present, is a nonselectable bound. Values execute through the owning light, texture, material or viewport consumer; undefined choices are rejected before mutation.

None=0, PCF5=1 and PCF13=2 compare one, five and thirteen samples from the actual [shadow-depth atlas](../components/canvas-shadow-atlas.md). Max=3 is not selectable. ShadowFilterSmooth scales the spacing in atlas texels by one plus its value; sample offsets wrap for point rows and fall outside directional rows as unshadowed. Filtering compares depths before averaging visibility.
