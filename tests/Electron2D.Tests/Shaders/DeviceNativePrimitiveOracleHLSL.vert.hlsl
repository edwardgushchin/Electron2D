cbuffer Frame : register(b0, space1) { float2 size; float2 options; };
struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; [[vk::builtin("PointSize")]] float pointSize; };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1, [[vk::location(2)]] float2 uv : TEXCOORD2) {
    float2 p = position + (options.x == 2 ? uv : float2(0,0));
    Output o;
    o.position = float4(p.x / size.x * 2 - 1, 1 - p.y / size.y * 2, 0, 1);
    o.color = color;
    o.uv = options.x == 1 ? uv : float2(0,0);
    o.pointSize = 1;
    return o;
}
