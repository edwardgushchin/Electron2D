cbuffer Frame : register(b0, space1) { float2 size;
#ifdef E2D_LIGHTING
uint lightingItem; uint padding;
#endif
 };
struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; [[vk::location(2)]] float4 instanceCustom : TEXCOORD2; [[vk::location(4)]] float2 uv2 : TEXCOORD4;
#ifdef E2D_LIGHTING
[[vk::location(3)]] nointerpolation uint lightingItem : TEXCOORD3;
#endif
 };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1, [[vk::location(2)]] float2 uv : TEXCOORD2, [[vk::location(3)]] float4 instanceCustom : TEXCOORD3, [[vk::location(7)]] float2 uv2 : TEXCOORD7)
{
    Output o;
    o.position = float4(position.x / size.x * 2 - 1, 1 - position.y / size.y * 2, 0, 1);
    o.color = color;
    o.uv = uv;
    o.uv2 = uv2;
    o.instanceCustom = instanceCustom;
#ifdef E2D_LIGHTING
    o.lightingItem = lightingItem;
#endif
    return o;
}
