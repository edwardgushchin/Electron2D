[[vk::binding(0,0)]] StructuredBuffer<uint> value;
float4 main():SV_Target0{return float4(float(value[0])/10,float(value[1])/100,float(value[2])/100,1);}
