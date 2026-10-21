#version 450
layout(location=0) out vec4 result;
layout(set=3,binding=0,std140) uniform Values { vec4 tint; bool enabled; };
void main() { result=enabled ? tint : vec4(1,0,1,1); }
