#version 450
layout(location=1) in vec4 tint;
layout(location=1) out vec4 color;
layout(location=6) out ivec4 numbers;
void main() { color=tint; numbers=ivec4(-7,17,-31,1); }
