#version 450
layout(location=0) in vec4 color;
layout(location=1) in vec2 uv;
layout(set=3,binding=0,std140) uniform Instance { vec4 padding; float intensity; vec2 displacement; };
layout(set=3,binding=1,std140) uniform Parameters { vec4 padding2; float gain; };
layout(set=2,binding=0) uniform sampler2D vertexMap;
layout(location=0) out vec4 result;
void main() { result = color * intensity * gain * texture(vertexMap,uv) + padding + padding2; }
