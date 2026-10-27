[[vk::constant_id(0)]] const uint unsignedSource=5;
[[vk::constant_id(1)]] const int signedSource=17;
static const uint64_t unsignedFactor=(1ull<<40)+uint64_t(unsignedSource);
static const int64_t signedFactor=-(1ll<<42)+int64_t(signedSource);
[[vk::binding(0,0)]] cbuffer Parameters { vector<uint64_t,2> values; }
struct Push { uint64_t base;int64_t signedBase;uint64_t mask;uint64_t expected; };
[[vk::push_constant]] ConstantBuffer<Push> pc;
struct Varying { float4 position:SV_Position; [[vk::location(0)]] nointerpolation vector<int64_t,4> wideValues:TEXCOORD0; [[vk::location(2)]] nointerpolation vector<uint64_t,2> wideOffsets:TEXCOORD1; };
Varying main(uint id:SV_VertexID){Varying o;float2 p=float2((id<<1)&2,id&2)*2-1;o.position=float4(p,0,1);o.wideValues=vector<int64_t,4>(int64_t(values.x),signedFactor,int64_t(pc.base),pc.signedBase);o.wideOffsets=vector<uint64_t,2>(unsignedFactor,pc.expected);return o;}
