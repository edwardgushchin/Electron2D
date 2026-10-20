[[vk::push_constant]] cbuffer Settings { float4 tint; };
float4 main(nointerpolation uint authored:TEXCOORD0, uint view:SV_ViewID):SV_Target0 { return (authored==view ? float4(view==0,view==1,view==2,1) : float4(1,1,1,1))*tint; }
