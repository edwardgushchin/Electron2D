cbuffer Frame : register(b0,space1) { float2 size; };
cbuffer Values : register(b1,space1) {
    row_major float4x4 a4[2];
    column_major float3x3 a3[2];
    column_major float4x4 m4;
    row_major float3x3 m3;
    float tail;
};
struct Output { float4 position : SV_Position; [[vk::location(0)]] float4 color : TEXCOORD0; [[vk::location(1)]] float2 uv : TEXCOORD1; };
Output main([[vk::location(0)]] float2 position : TEXCOORD0, [[vk::location(1)]] float4 color : TEXCOORD1, [[vk::location(2)]] float2 uv : TEXCOORD2) {
    float3 x=mul(float3(1,2,3),m3); float4 y=mul(float4(1,2,3,4),m4);
    bool valid=tail==0.875;
    for(int c=0;c<3;c++) valid=valid && all(m3[c]==a3[0][c]) && all(a3[1][c]==float3(c==0?1:0,c==1?1:0,c==2?1:0));
    for(int c=0;c<4;c++) valid=valid && all(m4[c]==a4[1][c]) && all(a4[0][c]==float4(c==0?1:0,c==1?1:0,c==2?1:0,c==3?1:0));
    float2 p=position+float2(x.x*8,0);
    Output o; o.position=float4(p.x/size.x*2-1,1-p.y/size.y*2,0,1); o.color=valid?color:float4(1,0,1,1); o.uv=uv; return o;
}
