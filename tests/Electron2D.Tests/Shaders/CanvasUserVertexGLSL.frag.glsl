#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(set=3,binding=0,std140) uniform Parameters { vec4 padding; float gain; };
layout(location=0) out vec4 result;
void main() { result = color * gain + vec4(uv,0,0) * padding; }
