#version 450
#extension GL_EXT_buffer_reference : require
layout(buffer_reference,std430,buffer_reference_align=4) readonly buffer Values { uint values[]; };
layout(push_constant,std430) uniform Root { Values data; } pc;
layout(location=0) out vec4 color;
void main() { color = vec4(pc.data.values[0],pc.data.values[1],0,1); }
