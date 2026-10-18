cbuffer Values : register(b0,space3) {
    row_major float4x4 a4[2];
    column_major float3x3 a3[2];
    column_major float4x4 m4;
    row_major float3x3 m3;
    float tail;
};
float4 main() : SV_Target0 {
    float3 x=mul(float3(1,2,3),m3); float4 y=mul(float4(1,2,3,4),m4);
    bool valid=tail==0.875;
    for(int c=0;c<3;c++) valid=valid && all(m3[c]==a3[0][c]) && all(a3[1][c]==float3(c==0?1:0,c==1?1:0,c==2?1:0));
    for(int c=0;c<4;c++) valid=valid && all(m4[c]==a4[1][c]) && all(a4[0][c]==float4(c==0?1:0,c==1?1:0,c==2?1:0,c==3?1:0));
    return valid?float4(x.x,x.y,y.z,1):float4(1,0,1,1);
}
