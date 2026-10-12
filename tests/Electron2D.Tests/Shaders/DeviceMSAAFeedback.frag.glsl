#version 450
layout(set=2,binding=13) uniform sampler2D source;
layout(location=0) out vec4 color;
void main() { color=textureLod(source,vec2(.5),0); }
