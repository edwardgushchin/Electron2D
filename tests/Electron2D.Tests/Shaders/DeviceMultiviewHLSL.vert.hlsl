struct Output { float4 position:SV_Position; nointerpolation uint view:TEXCOORD0; };
Output main(uint id:SV_VertexID, uint view:SV_ViewID) { Output result; result.position=float4(float2((id<<1)&2,id&2)*2-1,0.25,1); result.view=view; return result; }
