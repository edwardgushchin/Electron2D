float4 main([[vk::location(0)]] float4 color : TEXCOORD0, [[vk::location(1)]] float2 uv : TEXCOORD1, [[vk::location(2)]] float4 instanceCustom : TEXCOORD2, [[vk::location(4)]] float2 uv2 : TEXCOORD4, [[vk::location(5)]] float3 normal : TEXCOORD5, [[vk::location(6)]] float4 tangent : TEXCOORD6, [[vk::location(7)]] float4 custom0 : TEXCOORD7, [[vk::location(8)]] float4 custom1 : TEXCOORD8, [[vk::location(9)]] float4 custom2 : TEXCOORD9, [[vk::location(10)]] float4 custom3 : TEXCOORD10) : SV_Target0 {
    float4 xs=float4(custom0.x,custom1.x,custom2.x,custom3.x);
    bool padding=all(custom0.yzw==float3(0,0,1))&&all(custom1.yzw==float3(0,0,1))&&all(custom2.yzw==float3(0,0,1))&&all(custom3.yzw==float3(0,0,1));
    padding=padding&&color.a==1&&uv.x>=0&&all(instanceCustom==0)&&all(uv2==0)&&normal.z==-1&&tangent.w==1;
    return padding&&(all(xs==1)||all(xs==float4(1,-1,1,1)))?float4(0,1,0,1):padding&&all(xs==0)?float4(1,0,1,1):float4(1,0,0,1);
}
