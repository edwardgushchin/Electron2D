cbuffer Frame : register(b0,space1) { float2 size; };
cbuffer Values : register(b1,space1) {
    float f1 : packoffset(c0) = .125;
    column_major float3x3 m3 : packoffset(c1) = float3x3(1,2,3,4,5,6,7,8,9);
    float vertexOnly : packoffset(c4) = .5;
};
struct Output { float4 position:SV_Position; [[vk::location(0)]] float4 color:TEXCOORD0; [[vk::location(1)]] float2 uv:TEXCOORD1; };
Output main([[vk::location(0)]] float2 position:TEXCOORD0, [[vk::location(1)]] float4 color:TEXCOORD1, [[vk::location(2)]] float2 uv:TEXCOORD2) {
    position.x += (f1 - .125 + vertexOnly - .5 + m3[0][1] - 2) * 8;
    Output o; o.position=float4(position.x/size.x*2-1,1-position.y/size.y*2,0,1); o.color=color; o.uv=uv; return o;
}
