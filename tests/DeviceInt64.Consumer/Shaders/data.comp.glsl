#version 450
#extension GL_ARB_gpu_shader_int64 : require
layout(local_size_x=1) in;
layout(constant_id=0) const int64_t signedFactor=-1099511627773l;
layout(constant_id=1) const uint64_t unsignedFactor=0xfedcba9800000011ul;
layout(constant_id=2) const bool choose=true;
struct Pair { int64_t s; uint64_t u; };
layout(set=0,binding=0,std430) readonly buffer Input { Pair values[]; } source;
layout(set=0,binding=1,std430) buffer Output { uint64_t values[]; } target;
layout(set=1,binding=0,std140) uniform Parameters { i64vec2 offsets; u64vec2 masks; } parameters;
layout(push_constant,std430) uniform Push {
 layout(offset=0) int64_t delta;
 layout(offset=16) u64vec2 values;
 layout(offset=32) int64_t offsets[2];
 layout(offset=48) Pair pair;
 layout(offset=64) uint64_t index;
 layout(offset=72) int64_t divisor;
 layout(offset=80) uint64_t shift;
 layout(offset=120) uint64_t last;
} pc;
int64_t pair_value(Pair p){ return p.s; }
uint64_t vector_value(u64vec2 v,uint64_t i){return v[uint(i)];}
void main(){
 uint i=gl_GlobalInvocationID.x; uint j=i*16; int64_t s=source.values[i].s;uint64_t u=source.values[i].u;
 target.values[j]=uint64_t(s+signedFactor+pc.delta);
 target.values[j+1]=u*unsignedFactor+vector_value(pc.values,uint64_t(i&1));
 target.values[j+2]=uint64_t(s/pc.divisor);
 target.values[j+3]=uint64_t(s-(s/pc.divisor)*pc.divisor);
 target.values[j+4]=u>>pc.shift; target.values[j+5]=uint64_t(s>>pc.shift); target.values[j+6]=u<<pc.shift;
 target.values[j+7]=((u&parameters.masks.x)^parameters.masks.y)|pc.last;
 target.values[j+8]=uint64_t(s>signedFactor); target.values[j+9]=uint64_t(u>pc.values[i&1]);
 target.values[j+10]=uint64_t(pc.offsets[i&1]+parameters.offsets[i&1]+pair_value(pc.pair));
 target.values[j+11]=pc.pair.u^pc.values[uint(pc.index)]; target.values[j+12]=unsignedFactor;target.values[j+13]=uint64_t(signedFactor);
 target.values[j+14]=choose?u:uint64_t(s);target.values[j+15]=uint64_t(uint(u))+uint64_t(int64_t(int(s)));
}
