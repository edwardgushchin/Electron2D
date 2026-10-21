cbuffer Values:register(b0,space3) { float4 tint:packoffset(c0) = float4(.25,.5,.75,1); bool enabled:packoffset(c1) = true; };
float4 main(float4 modulate:TEXCOORD0,float2 uv:TEXCOORD1):SV_Target0 { return enabled ? tint : float4(1,0,1,1); }
