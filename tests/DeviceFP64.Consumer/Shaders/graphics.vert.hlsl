[[vk::constant_id(0)]] const float factorSource=2.0f;
static const double factor=double(factorSource);
[[vk::binding(0,0)]] cbuffer Parameters { double2 inputValue; }
struct Push { double delta; double fragmentValue; };
[[vk::push_constant]] ConstantBuffer<Push> pc;
struct Varying { float4 position:SV_Position; nointerpolation float signalValue:TEXCOORD0; };
Varying main(uint id:SV_VertexID){
 Varying o; float2 p=float2((id<<1)&2,id&2)*2-1;
 double resultValue=(inputValue.x-1.0)*1099511627776.0+pc.delta;
 o.position=float4(p,0,1); o.signalValue=float(resultValue*factor/16.0); return o;
}
