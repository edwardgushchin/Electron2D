#version 450
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_buffer_reference_uvec2 : require
layout(buffer_reference,std430,buffer_reference_align=4) buffer Values { uint values[]; };
layout(push_constant,std430) uniform Addresses { uvec2 address; } pc;
void main() { if (gl_VertexIndex == 0) Values(pc.address).values[0] += 1; vec2 p = vec2((gl_VertexIndex << 1) & 2, gl_VertexIndex & 2); gl_Position = vec4(p * 2.0 - 1.0, 0, 1); }
