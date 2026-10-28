struct Parameters {float4 tint;float4 controls;};
[[vk::push_constant]] ConstantBuffer<Parameters> pc;
float4 main(uint id:SV_VertexID):SV_Position{uint side=id/6,k=id%6;if((side!=0)!=(pc.controls.y!=0))k=k/3*3+2-k%3;uint corners[6]={0,1,2,2,1,3};uint c=corners[k];return float4(float(c&1)+float(side)-1,float(c>>1)*2-1,pc.controls.x,1);}
