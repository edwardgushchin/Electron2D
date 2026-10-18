cbuffer Frame : register(b0, space1) { float2 size; };
cbuffer Parameters : register(b1, space1) { float2 generated[4] : packoffset(c0); float2 offset : packoffset(c4); float homogeneous_w : packoffset(c4.z); };
[[vk::binding(0,0)]] Texture2D<float4> position_map : register(t0,space0);
[[vk::binding(0,0)]] SamplerState position_sampler : register(s0,space0);
struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; [[vk::builtin("PointSize")]] float pointSize : PSIZE; };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1, [[vk::location(2)]] float2 uv : TEXCOORD2, [[vk::location(10)]] float4 custom0 : TEXCOORD10, uint id : SV_VertexID) {
    float2 p = position + generated[id % 4] + offset + position_map.SampleLevel(position_sampler, uv, 0).xy;
    float w = 1 + homogeneous_w * (id % 4 + 1);
    Output o; o.position = float4((p.x / size.x * 2 - 1) * w, (1 - p.y / size.y * 2) * w, 0, w);
    o.pointSize = 1; o.color = color * float4(1 - custom0.x, 1, 1, 1); o.uv = uv; return o;
}
