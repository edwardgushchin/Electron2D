cbuffer Frame : register(b0, space1) { float2 size; };
struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1, [[vk::location(2)]] float2 uv : TEXCOORD2, uint instance : SV_InstanceID)
{
    Output output;
    output.position = float4(position.x / size.x * 2 - 1, 1 - position.y / size.y * 2, 0, 1);
    output.color = color * (instance == 0 ? float4(1, 0, 0, 1) : float4(0, 1, 0, 1));
    output.uv = uv;
    return output;
}
