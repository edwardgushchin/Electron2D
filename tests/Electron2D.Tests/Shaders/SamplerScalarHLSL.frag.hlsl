#define SLOT NonUniformResourceIndex(slot)
[[vk::binding(0,2)]] Texture2D<float4> maps;
[[vk::binding(0,2)]] SamplerState mapSamplers;
[[vk::binding(1,2)]] Texture2DArray<float4> layers;
[[vk::binding(1,2)]] SamplerState layerSamplers;
[[vk::binding(2,2)]] Texture2D<int4> signedMaps;
[[vk::binding(3,2)]] Texture2D<uint4> unsignedMaps;
[[vk::binding(0,3)]] cbuffer Material { int selected; int mode; };
float4 readMap(Texture2D<float4> source, SamplerState state, float2 uv) { return source.Sample(state,uv); }
float4 main(float4 color:TEXCOORD0,float4 position:SV_Position):SV_Target0 {
    int slot=selected;
    if(mode>=10) slot=int(position.x)&1;
    float2 uv=position.xy*.125;
    float4 value;
    if(mode==0 || mode==10) value=readMap(maps,mapSamplers,uv);
    else if(mode==1) value=maps.SampleLevel(mapSamplers,uv,0);
    else if(mode==2) value=maps.Load(int3(selected==0?1:3,selected==0?1:3,0));
    else if(mode==3) { uint w,h; maps.GetDimensions(w,h); value=float4(float2(w,h)/8,0,1); }
    else if(mode==4) value=maps.SampleGrad(mapSamplers,uv,float2(.125,0),float2(0,.125));
    else if(mode==5) value=float4(maps.GatherRed(mapSamplers,uv).xyz,1);
    else if(mode==6) value=layers.SampleLevel(layerSamplers,float3(uv,float(selected)),0);
    else if(mode==7) value=float4(float(signedMaps.Load(int3(0,0,0)).x)*.125+.5,0,0,1);
    else if(mode==8) value=float4(float(unsignedMaps.Load(int3(0,0,0)).x)/255,0,0,1);
    else if(mode==9) { uint w,h,n; layers.GetDimensions(w,h,n); value=float4(float3(w,h,n)/8,1); }
    else { float lod=maps.CalculateLevelOfDetail(mapSamplers,uv); float raw=maps.CalculateLevelOfDetailUnclamped(mapSamplers,uv); value=float4(float2(lod,raw)*.125+.5,0,1); }
    return color*value;
}
