#version 450
#extension GL_EXT_buffer_reference : require
#extension GL_EXT_shader_explicit_arithmetic_types_int64 : require
#extension GL_EXT_shader_atomic_int64 : require
layout(local_size_x=8) in;
layout(buffer_reference,std430,buffer_reference_align=8) buffer Values { uint64_t count; uint64_t previous; };
layout(push_constant,std430) uniform Root { Values data; } pc;
void main() { uint64_t value = atomicAdd(pc.data.count,uint64_t(1)); atomicMax(pc.data.previous,value); }
