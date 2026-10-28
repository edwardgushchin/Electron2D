#version 450
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_buffer_reference_uvec2 : require
layout(local_size_x=1) in;
layout(constant_id=0) const uint factor = 1;
layout(buffer_reference, std430, buffer_reference_align=4) buffer Values { uint values[]; };
layout(push_constant, std430) uniform Addresses { Values data; uint delta; } pc;
void main() { Values v = pc.data; v.values[0] += pc.delta * factor; v.values[1] = v.values[0] * 3; }
