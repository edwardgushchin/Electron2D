#version 450
layout(set=0,binding=0,std430) buffer Values { uint value[]; };
layout(constant_id=0) const uint factor=1;
void main(){atomicAdd(value[0],factor);}
