#version 450
layout(set=3,binding=0,std140) uniform Instance { float f; vec2 v2; vec3 v3; vec4 v4; int i; uint u; ivec2 i2; ivec3 i3; ivec4 i4; uvec2 u2; uvec3 u3; uvec4 u4; bool b; bvec2 b2; bvec3 b3; bvec4 b4; };
layout(location=0) out vec4 outputColor;
void main() { outputColor = vec4(f+v2.x+v3.x+v4.x+float(i)+float(u)+float(i2.x+i3.x+i4.x)+float(u2.x+u3.x+u4.x)+(b?1.0:0.0)+(b2.x?1.0:0.0)+(b3.x?1.0:0.0)+(b4.x?1.0:0.0)); }
