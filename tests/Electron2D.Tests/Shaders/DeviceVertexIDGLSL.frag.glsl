#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(location=0) out vec4 outputColor;
void main() { outputColor = color + vec4(uv.y * 0.00001); }
