#version 450
layout(push_constant,std430) uniform Parameters { vec4 tint; vec4 controls; } pc;
layout(set=0,binding=0,std140) uniform User { vec4 multiplier; };
layout(location=0) out vec4 color;
void main() { color=pc.tint*multiplier; }
