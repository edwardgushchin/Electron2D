#version 450
layout(location=0) out vec4 outputColor;
layout(set=3,binding=0,std140) uniform Shared{float materialGain;};
layout(set=3,binding=1,std140) uniform Instance{
    layout(offset=0) float f1;
    layout(offset=16) vec2 f2;
    layout(offset=32) vec3 f3;
    layout(offset=48) vec4 f4;
    layout(offset=64) int i1;
    layout(offset=80) ivec2 i2;
    layout(offset=96) ivec3 i3;
    layout(offset=112) ivec4 i4;
    layout(offset=128) uint u1;
    layout(offset=144) uvec2 u2;
    layout(offset=160) uvec3 u3;
    layout(offset=176) uvec4 u4;
    layout(offset=192) bool b1;
    layout(offset=208) bvec2 b2;
    layout(offset=224) bvec3 b3;
    layout(offset=240) bvec4 b4;
};
void main(){
if (!((f1 == 0.125))) { outputColor=vec4(1/255.0,0,1,1); return; }
if (!(all(equal(f2,vec2(0.125,0.25))))) { outputColor=vec4(4/255.0,0,1,1); return; }
if (!(all(equal(f3,vec3(0.125,0.25,0.375))))) { outputColor=vec4(7/255.0,0,1,1); return; }
if (!(all(equal(f4,vec4(0.125,0.25,0.375,0.5))))) { outputColor=vec4(10/255.0,0,1,1); return; }
if (!((i1 == -7))) { outputColor=vec4(13/255.0,0,1,1); return; }
if (!(all(equal(i2,ivec2(-7,2))))) { outputColor=vec4(16/255.0,0,1,1); return; }
if (!(all(equal(i3,ivec3(-7,2,-3))))) { outputColor=vec4(19/255.0,0,1,1); return; }
if (!(all(equal(i4,ivec4(-7,2,-3,4))))) { outputColor=vec4(22/255.0,0,1,1); return; }
if (!((u1 == 4294967295u))) { outputColor=vec4(25/255.0,0,1,1); return; }
if (!(all(equal(u2,uvec2(4294967295u,2147483648u))))) { outputColor=vec4(28/255.0,0,1,1); return; }
if (!(all(equal(u3,uvec3(4294967295u,2147483648u,7u))))) { outputColor=vec4(31/255.0,0,1,1); return; }
if (!(all(equal(u4,uvec4(4294967295u,2147483648u,7u,1u))))) { outputColor=vec4(34/255.0,0,1,1); return; }
if (!((b1 == true))) { outputColor=vec4(37/255.0,0,1,1); return; }
if (!(all(equal(b2,bvec2(true,false))))) { outputColor=vec4(40/255.0,0,1,1); return; }
if (!(all(equal(b3,bvec3(true,false,true))))) { outputColor=vec4(43/255.0,0,1,1); return; }
if (!(all(equal(b4,bvec4(true,false,true,false))))) { outputColor=vec4(46/255.0,0,1,1); return; }
    outputColor=vec4(0,materialGain,0,1);
}
