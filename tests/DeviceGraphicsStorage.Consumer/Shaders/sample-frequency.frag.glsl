#version 450
layout(set=0,binding=0,std430) buffer Values {uint value[];};
layout(location=0) out vec4 color;
void main(){atomicAdd(value[0],1);color=vec4(1,.5,.25,.25);}
