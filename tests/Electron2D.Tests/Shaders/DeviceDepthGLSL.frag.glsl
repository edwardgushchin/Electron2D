#version 450
layout(set=5,binding=7,std140) uniform Parameters { vec4 tint; vec4 controls; };
layout(location=0) out vec4 color;
void main() { color=tint; }
