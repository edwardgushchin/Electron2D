#version 450
#extension GL_ARB_gpu_shader_int64 : require
layout(set=0,binding=0,std140) uniform Parameters { u64vec2 values; } parameters;
layout(push_constant,std430) uniform Push { uint64_t base;int64_t signedBase;uint64_t mask;uint64_t expected; } pc;
layout(location=0) flat in i64vec4 wideValues;
layout(location=2) flat in u64vec2 wideOffsets;
layout(location=0) out vec4 color;
void main(){uint64_t r=uint64_t(wideValues.x-wideValues.z)+(wideOffsets.x-0x10000000000ul);int64_t g=wideValues.y-wideValues.w;uint64_t b=((parameters.values.y&pc.mask)^wideOffsets.y)&63ul;bool high=wideValues.x>0x10000000000000l&&wideOffsets.x>0x10000000000ul&&wideValues.y<0l;color=vec4(float(r)/64,float(g)/64,float(b)/64,high?1:0);}
