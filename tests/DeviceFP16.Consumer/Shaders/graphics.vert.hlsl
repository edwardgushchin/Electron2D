[[vk::constant_id(0)]] const float factorSource=0.25;
static const float16_t factor=float16_t(factorSource);
struct Push { float16_t a;float16_t b; };[[vk::push_constant]] ConstantBuffer<Push> pc;
struct Interstage { float4 position:SV_Position; [[vk::location(0)]] float2 color:COLOR0; };
[[vk::binding(0,0)]] cbuffer Values { vector<float16_t,2> value; }
Interstage main(uint id:SV_VertexID){Interstage o;float2 p=float2((id<<1)&2,id&2);o.position=float4(p*2-1,0,1);o.color=float2(float(value.x+pc.a+factor),float(value.y+pc.b));return o;}
