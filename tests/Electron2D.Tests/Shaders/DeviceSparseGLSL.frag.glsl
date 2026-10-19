#version 450
layout(constant_id=3) const float scale=1;
layout(location=1) in vec4 tint;
layout(location=1) out vec4 color;
layout(location=6) out uvec4 numbers;
void main() { color=vec4(tint.rgb*scale,tint.a); numbers=uvec4(42,43,44,45); }
