[[vk::binding(0,0)]] RWTexture2DArray<float4> pixels;
float4 main():SV_Target0{return pixels[int3(0,0,0)];}
