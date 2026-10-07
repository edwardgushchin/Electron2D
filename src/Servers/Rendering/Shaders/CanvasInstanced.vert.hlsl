cbuffer Frame : register(b0, space1) { float2 size; };
struct Output { float4 position : SV_Position; float4 color : TEXCOORD0; float2 uv : TEXCOORD1; float4 instanceCustom : TEXCOORD2; };
Output main(float2 position : TEXCOORD0, float4 color : TEXCOORD1, float2 uv : TEXCOORD2,
    float4 basis : TEXCOORD3, float4 translation : TEXCOORD4, float4 tint : TEXCOORD5, float4 custom : TEXCOORD6)
{
    Output o;
    precise float2 p = position.x * basis.xy + position.y * basis.zw + translation.xy;
    o.position = float4(p.x / size.x * 2 - 1, 1 - p.y / size.y * 2, 0, 1);
    o.color = color * tint;
    o.uv = uv;
    o.instanceCustom = custom;
    return o;
}
