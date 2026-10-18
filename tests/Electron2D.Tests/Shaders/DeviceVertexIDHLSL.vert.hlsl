cbuffer Frame : register(b0, space1) { float2 size; };
cbuffer Parameters : register(b1, space1) { float raw; };
struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1, [[vk::location(2)]] float2 uv : TEXCOORD2, uint id : SV_VertexID) {
    Output o; o.position = float4(position.x / size.x * 2 - 1, 1 - position.y / size.y * 2, 0, 1);
    o.color = raw > 0 ? float4(float(id) / 32, 0, 0, 1) : color * float(id == uint(uv.x)); o.uv = uv; return o;
}
