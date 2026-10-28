#version 450
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_buffer_reference_uvec2 : require
layout(buffer_reference,std430,buffer_reference_align=4) buffer Values { uint values[]; };
layout(push_constant,std430) uniform Addresses { uvec2 address; } pc;
layout(location=0) out vec4 color;
void main() { if (all(lessThan(gl_FragCoord.xy,vec2(1)))) Values(pc.address).values[1] += 5; color = vec4(1,0,0,1); }
