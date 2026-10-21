cbuffer Frame : register(b0, space1) { float2 size;
uint lightingItem; uint flags;
float4 canvasModulation;
 };
struct Output { [[vk::builtin("PointSize")]] float pointSize; float4 position : SV_Position; [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; [[vk::location(2)]] float4 instanceCustom : TEXCOORD2; [[vk::location(4)]] float2 uv2 : TEXCOORD4; [[vk::location(5)]] float3 normal : TEXCOORD5; [[vk::location(6)]] float4 tangent : TEXCOORD6; [[vk::location(7)]] float4 custom0 : TEXCOORD7; [[vk::location(8)]] float4 custom1 : TEXCOORD8; [[vk::location(9)]] float4 custom2 : TEXCOORD9; [[vk::location(10)]] float4 custom3 : TEXCOORD10; [[vk::location(11)]] nointerpolation float4 glyph : TEXCOORD11;
#ifdef E2D_LIGHTING
[[vk::location(3)]] nointerpolation uint lightingItem : TEXCOORD3;
#endif
 };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1, [[vk::location(2)]] float2 uv : TEXCOORD2,
    [[vk::location(3)]] float4 basis : TEXCOORD3, [[vk::location(4)]] float4 translation : TEXCOORD4, [[vk::location(5)]] float4 tint : TEXCOORD5, [[vk::location(6)]] float4 custom : TEXCOORD6, [[vk::location(7)]] float2 uv2 : TEXCOORD7, [[vk::location(8)]] float3 normal : TEXCOORD8, [[vk::location(9)]] float4 tangent : TEXCOORD9, [[vk::location(10)]] float4 custom0 : TEXCOORD10, [[vk::location(11)]] float4 custom1 : TEXCOORD11, [[vk::location(12)]] float4 custom2 : TEXCOORD12, [[vk::location(13)]] float4 custom3 : TEXCOORD13, [[vk::location(14)]] float4 glyph : TEXCOORD14)
{
    Output o;
    o.pointSize = 1;
    precise float2 p = position.x * basis.xy + position.y * basis.zw + translation.xy;
    if ((flags & 1) != 0) p = floor(p + 0.5);
    o.position = float4(p.x / size.x * 2 - 1, 1 - p.y / size.y * 2, 0, 1);
    o.color = color * tint * canvasModulation;
    o.uv = (flags & 2) != 0 ? p / size : uv;
    o.uv2 = uv2; o.normal = normal; o.tangent = tangent; o.custom0 = custom0; o.custom1 = custom1; o.custom2 = custom2; o.custom3 = custom3;
    o.glyph = glyph;
    o.instanceCustom = custom;
#ifdef E2D_LIGHTING
    o.lightingItem = lightingItem;
#endif
    return o;
}
