[[vk::binding(0,0)]] RWTexture2D<float4> destination;
[[vk::binding(1,0)]] RWTexture2D<float4> sourceImage;
[[vk::binding(2,0)]] Texture2D<float4> sampled;
[[vk::binding(2,0)]] SamplerState nearest;
[[vk::binding(3,0)]] RWStructuredBuffer<uint> result;
struct Parameters {float4 increment;};[[vk::binding(4,0)]] ConstantBuffer<Parameters> parameters;
struct Push {uint step;};[[vk::push_constant]] ConstantBuffer<Push> pc;
float4 main(float4 position:SV_Position):SV_Target0{int2 p=int2(position.xy);destination[p]=sourceImage[p]+sampled.SampleLevel(nearest,(float2(p)+.5)/4,0)+parameters.increment;result[p.x+4*p.y]=pc.step+uint(p.x+4*p.y);return 0;}
