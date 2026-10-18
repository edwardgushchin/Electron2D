cbuffer Frame : register(b0, space1) { float4 rectangle; float4 modulation; float4 size; };
struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 modulate : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; };
Output main(uint id : SV_VertexID)
{
    const float2 coordinates[6] = { float2(0,0), float2(1,0), float2(0,1), float2(0,1), float2(1,0), float2(1,1) };
    Output o; o.uv = coordinates[id]; o.modulate = modulation;
    float2 pixel = rectangle.xy + o.uv * rectangle.zw;
    o.position = float4(pixel.x / size.x * 2 - 1, 1 - pixel.y / size.y * 2, 0, 1);
    return o;
}
