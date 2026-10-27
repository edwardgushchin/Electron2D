[[vk::constant_id(0)]] const int factorSource=2;
static const int16_t factor=int16_t(factorSource);
struct Push { int16_t a;int16_t b; };[[vk::push_constant]] ConstantBuffer<Push> pc;
struct Interstage { float4 position:SV_Position; [[vk::location(0)]] float2 color:COLOR0; };
float4 main(Interstage input):SV_Target0{return float4(float((int16_t(input.color.x)+pc.a)*factor)/64,float((int16_t(input.color.y)+pc.b)*factor)/64,.125,1);}
