#version 450
layout(set=0,binding=0,std430) buffer Values {uint value[];};
void main(){atomicAdd(value[0],1);}
