#version 450
#extension GL_ARB_gpu_shader_int64 : require
layout(constant_id=0) const uint64_t unsignedFactor=0x10000000005ul;
layout(constant_id=1) const int64_t signedFactor=-4398046511087l;
layout(set=0,binding=0,std140) uniform Parameters { u64vec2 values; } parameters;
layout(push_constant,std430) uniform Push { uint64_t base;int64_t signedBase;uint64_t mask;uint64_t expected; } pc;
layout(location=0) flat out i64vec4 wideValues;
layout(location=2) flat out u64vec2 wideOffsets;
void main(){vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2)*2-1;gl_Position=vec4(p,0,1);wideValues=i64vec4(int64_t(parameters.values.x),signedFactor,int64_t(pc.base),pc.signedBase);wideOffsets=u64vec2(unsignedFactor,pc.expected);}
