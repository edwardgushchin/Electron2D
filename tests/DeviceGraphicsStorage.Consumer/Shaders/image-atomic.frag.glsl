#version 450
layout(set=0,binding=2,r32ui) uniform uimage2D U;
layout(set=0,binding=3,r32i) uniform iimage2D I;
layout(set=0,binding=4,std430) buffer Results {uvec2 result[];};
layout(constant_id=0) const uint factor=1;
void atomics(ivec2 p,uint index){
 uint u=0;u+=imageAtomicExchange(U,p,40);u+=imageAtomicAdd(U,p,2);u+=imageAtomicMin(U,p,10);u+=imageAtomicMax(U,p,20);u+=imageAtomicAnd(U,p,15);u+=imageAtomicOr(U,p,8);u+=imageAtomicXor(U,p,3);u+=imageAtomicCompSwap(U,p,15,100);u+=imageAtomicCompSwap(U,p,15,999);u+=imageAtomicAdd(U,p,1);
 int i=0;i+=imageAtomicExchange(I,p,-40);i+=imageAtomicAdd(I,p,2);i+=imageAtomicMin(I,p,-50);i+=imageAtomicMax(I,p,-20);i+=imageAtomicAnd(I,p,15);i+=imageAtomicOr(I,p,16);i+=imageAtomicXor(I,p,3);i+=imageAtomicCompSwap(I,p,31,-100);i+=imageAtomicCompSwap(I,p,31,999);i+=imageAtomicAdd(I,p,1);
 result[index]=imageSize(U)==ivec2(4)&&imageSize(I)==ivec2(4)&&imageLoad(U,p).r==101&&imageLoad(I,p).r==-99 ? uvec2(u,uint(i))*factor : uvec2(0);
}
layout(location=0) out vec4 color;void main(){ivec2 p=ivec2(gl_FragCoord.xy);atomics(p,4+uint(p.x+4*p.y));color=vec4(1);}
