#version 450
layout(location = 1) in vec2 primaryUV;
layout(location = 2) in vec4 custom;
layout(location = 4) in vec2 secondaryUV;
layout(location = 0) out vec4 color;
void main() { color = vec4(secondaryUV.x, primaryUV.x, custom.b, 1); }
