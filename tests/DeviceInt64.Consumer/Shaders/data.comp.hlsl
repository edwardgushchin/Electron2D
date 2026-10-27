[[vk::constant_id(0)]] const int signedSource=3;
static const int64_t signedFactor=-(1ll<<40)+int64_t(signedSource);
[[vk::constant_id(1)]] const uint unsignedSource=17;
static const uint64_t unsignedFactor=(uint64_t(0xfedcba98u)<<32)|uint64_t(unsignedSource);
[[vk::constant_id(2)]] const bool choose=true;
struct Pair { int64_t s; uint64_t u; };
struct Push {
 [[vk::offset(0)]] int64_t delta;
 [[vk::offset(16)]] vector<uint64_t,2> values;
 [[vk::offset(32)]] int64_t offsets[2];
 [[vk::offset(48)]] Pair pair;
 [[vk::offset(64)]] uint64_t index;
 [[vk::offset(72)]] int64_t divisor;
 [[vk::offset(80)]] uint64_t shift;
 [[vk::offset(120)]] uint64_t last;
};
[[vk::push_constant]] ConstantBuffer<Push> pc;
[[vk::binding(0,0)]] StructuredBuffer<Pair> source;
[[vk::binding(1,0)]] RWStructuredBuffer<uint64_t> target;
[[vk::binding(0,1)]] cbuffer Parameters { vector<int64_t,2> offsets; vector<uint64_t,2> masks; }
int64_t pair_value(Pair p){return p.s;}
uint64_t vector_value(vector<uint64_t,2> v,uint64_t i){return v[uint(i)];}
[numthreads(1,1,1)] void main(uint3 id:SV_DispatchThreadID){
 uint i=id.x,j=i*16;int64_t s=source[i].s;uint64_t u=source[i].u;
 target[j]=uint64_t(s+signedFactor+pc.delta);target[j+1]=u*unsignedFactor+vector_value(pc.values,uint64_t(i&1));
 target[j+2]=uint64_t(s/pc.divisor);target[j+3]=uint64_t(s-(s/pc.divisor)*pc.divisor);
 target[j+4]=u>>pc.shift;target[j+5]=uint64_t(s>>pc.shift);target[j+6]=u<<pc.shift;
 target[j+7]=((u&masks.x)^masks.y)|pc.last;target[j+8]=uint64_t(s>signedFactor);target[j+9]=uint64_t(u>pc.values[i&1]);
 target[j+10]=uint64_t(pc.offsets[i&1]+offsets[i&1]+pair_value(pc.pair));target[j+11]=pc.pair.u^pc.values[uint(pc.index)];
 target[j+12]=unsignedFactor;target[j+13]=uint64_t(signedFactor);target[j+14]=choose?u:uint64_t(s);target[j+15]=uint64_t(uint(u))+uint64_t(int64_t(int(s)));
}
