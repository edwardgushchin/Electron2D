#version 450
layout(location = 0) out vec4 outputColor;
layout(set = 3, binding = 0, std140) uniform Frame { float TIME; };
void main() { outputColor = vec4(fract(TIME * 4), 0, 0, 1); }
