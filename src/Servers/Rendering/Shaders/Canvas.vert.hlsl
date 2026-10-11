cbuffer Frame : register(b0, space1) { float2 size;
#ifdef E2D_LIGHTING
uint lightingItem; uint padding;
#endif
 };
struct Output { float4 position : SV_Position; float4 color : TEXCOORD0; float2 uv : TEXCOORD1; float4 instanceCustom : TEXCOORD2;
#ifdef E2D_LIGHTING
nointerpolation uint lightingItem : TEXCOORD3;
#endif
 };
Output main(float2 position : TEXCOORD0, float4 color : TEXCOORD1, float2 uv : TEXCOORD2, float4 instanceCustom : TEXCOORD3)
{
    Output o;
    o.position = float4(position.x / size.x * 2 - 1, 1 - position.y / size.y * 2, 0, 1);
    o.color = color;
    o.uv = uv;
    o.instanceCustom = instanceCustom;
#ifdef E2D_LIGHTING
    o.lightingItem = lightingItem;
#endif
    return o;
}
