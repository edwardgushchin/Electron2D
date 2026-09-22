cbuffer Frame : register(b0, space1) { float2 size; };
struct Output { float4 position : SV_Position; float4 color : TEXCOORD0; float2 uv : TEXCOORD1; };
Output main(float2 position : TEXCOORD0, float4 color : TEXCOORD1, float2 uv : TEXCOORD2)
{
    Output o;
    o.position = float4(position.x / size.x * 2 - 1, 1 - position.y / size.y * 2, 0, 1);
    o.color = color;
    o.uv = uv;
    return o;
}
