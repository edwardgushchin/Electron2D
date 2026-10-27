[[vk::constant_id(0)]] const uint unsignedSource=5;
[[vk::constant_id(1)]] const int signedSource=17;
static const uint64_t unsignedFactor=(1ull<<40)+uint64_t(unsignedSource);
static const int64_t signedFactor=-(1ll<<42)+int64_t(signedSource);
[[vk::binding(0,0)]] cbuffer Parameters { vector<uint64_t,2> values; }
struct Push { uint64_t base;int64_t signedBase;uint64_t mask;uint64_t expected; };
[[vk::push_constant]] ConstantBuffer<Push> pc;
struct Varying { float4 position:SV_Position; [[vk::location(0)]] nointerpolation vector<int64_t,4> wideValues:TEXCOORD0; [[vk::location(2)]] nointerpolation vector<uint64_t,2> wideOffsets:TEXCOORD1; };
float4 main(Varying i):SV_Target0 {uint64_t r=uint64_t(i.wideValues.x-i.wideValues.z)+(i.wideOffsets.x-(1ull<<40));int64_t g=i.wideValues.y-i.wideValues.w;uint64_t b=((values.y&pc.mask)^i.wideOffsets.y)&63ull;bool high=i.wideValues.x>(1ll<<52)&&i.wideOffsets.x>(1ull<<40)&&i.wideValues.y<0ll;return float4(float(r)/64,float(g)/64,float(b)/64,high?1:0);}
