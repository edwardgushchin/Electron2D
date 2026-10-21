cbuffer Shared:register(b0,space3){float materialGain = 1;};
cbuffer Instance:register(b1,space3){
    float f1 : packoffset(c0) = 0.125;
    float2 f2 : packoffset(c1) = float2(0.125,0.25);
    float3 f3 : packoffset(c2) = float3(0.125,0.25,0.375);
    float4 f4 : packoffset(c3) = float4(0.125,0.25,0.375,0.5);
    int i1 : packoffset(c4) = -7;
    int2 i2 : packoffset(c5) = int2(-7,2);
    int3 i3 : packoffset(c6) = int3(-7,2,-3);
    int4 i4 : packoffset(c7) = int4(-7,2,-3,4);
    uint u1 : packoffset(c8) = 4294967295u;
    uint2 u2 : packoffset(c9) = uint2(4294967295u,2147483648u);
    uint3 u3 : packoffset(c10) = uint3(4294967295u,2147483648u,7u);
    uint4 u4 : packoffset(c11) = uint4(4294967295u,2147483648u,7u,1u);
    bool b1 : packoffset(c12) = true;
    bool2 b2 : packoffset(c13) = bool2(true,false);
    bool3 b3 : packoffset(c14) = bool3(true,false,true);
    bool4 b4 : packoffset(c15) = bool4(true,false,true,false);
};
float4 main():SV_Target0{
if (!((f1 == 0.125))) { return float4(1/255.0,0,1,1); }
if (!(all(f2 == float2(0.125,0.25)))) { return float4(4/255.0,0,1,1); }
if (!(all(f3 == float3(0.125,0.25,0.375)))) { return float4(7/255.0,0,1,1); }
if (!(all(f4 == float4(0.125,0.25,0.375,0.5)))) { return float4(10/255.0,0,1,1); }
if (!((i1 == -7))) { return float4(13/255.0,0,1,1); }
if (!(all(i2 == int2(-7,2)))) { return float4(16/255.0,0,1,1); }
if (!(all(i3 == int3(-7,2,-3)))) { return float4(19/255.0,0,1,1); }
if (!(all(i4 == int4(-7,2,-3,4)))) { return float4(22/255.0,0,1,1); }
if (!((u1 == 4294967295u))) { return float4(25/255.0,0,1,1); }
if (!(all(u2 == uint2(4294967295u,2147483648u)))) { return float4(28/255.0,0,1,1); }
if (!(all(u3 == uint3(4294967295u,2147483648u,7u)))) { return float4(31/255.0,0,1,1); }
if (!(all(u4 == uint4(4294967295u,2147483648u,7u,1u)))) { return float4(34/255.0,0,1,1); }
if (!((b1 == true))) { return float4(37/255.0,0,1,1); }
if (!(all(b2 == bool2(true,false)))) { return float4(40/255.0,0,1,1); }
if (!(all(b3 == bool3(true,false,true)))) { return float4(43/255.0,0,1,1); }
if (!(all(b4 == bool4(true,false,true,false)))) { return float4(46/255.0,0,1,1); }
    return float4(0,materialGain,0,1);
}
