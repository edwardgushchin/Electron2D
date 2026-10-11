#version 450
layout(location=0) out vec4 color;
layout(location=1) out uvec4 numbers;
void main() { color=vec4(0,0,1,1); numbers=uvec4(42,43,44,45); }
