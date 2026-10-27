[[vk::binding(7,3)]]Buffer<float4> texels;
float4 main():SV_Target0{return texels.Load(4);}
