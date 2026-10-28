[[vk::binding(0,0)]] RWTexture2DArray<float4> pixels;
float4 main(uint id:SV_VertexID):SV_Position{uint w,h,l;pixels.GetDimensions(w,h,l);float2 p=float2((id<<1)&2,id&2);return float4(p*2-1,l==1&&pixels[int3(0,0,0)].a==1?0:2,1);}
