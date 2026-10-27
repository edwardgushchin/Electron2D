[[vk::constant_id(0)]] const float factorSource=0.25;
static const float16_t factor=float16_t(factorSource);
struct Push { float16_t a;float16_t b; };[[vk::push_constant]] ConstantBuffer<Push> pc;
struct Interstage { float4 position:SV_Position; [[vk::location(0)]] float2 color:COLOR0; };
float4 main(Interstage input):SV_Target0{return float4(float((float16_t(input.color.x)+pc.a)*factor),float((float16_t(input.color.y)+pc.b)*factor),float(float16_t(0.125)),1);}
