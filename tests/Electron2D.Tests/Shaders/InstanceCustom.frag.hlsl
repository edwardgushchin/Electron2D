float4 main(float4 color : TEXCOORD0, float2 uv : TEXCOORD1, float4 instanceCustom : TEXCOORD2) : SV_Target0
{
    if (any(uv < 0)) discard;
    return float4(instanceCustom.rgb, instanceCustom.a * color.a);
}
