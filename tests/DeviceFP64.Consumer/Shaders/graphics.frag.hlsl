[[vk::constant_id(0)]] const float factorSource=2.0f;
static const double factor=double(factorSource);
[[vk::binding(0,0)]] cbuffer Parameters { double2 inputValue; }
struct Push { double delta; double fragmentValue; };
[[vk::push_constant]] ConstantBuffer<Push> pc;
struct Varying { float4 position:SV_Position; nointerpolation float signalValue:TEXCOORD0; };
float4 main(Varying i):SV_Target0 {
 double resultValue=(inputValue.y-1.0)*1099511627776.0+pc.fragmentValue;
 return float4(i.signalValue,float(resultValue*factor/16.0),float(factor/8.0),1);
}
