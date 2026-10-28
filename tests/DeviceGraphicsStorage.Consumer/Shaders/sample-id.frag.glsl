#version 450
layout(set=0,binding=0,std430) buffer Values {uint value[];};
layout(location=0) out vec4 color;
void main(){atomicAdd(value[gl_SampleID],1);color=vec4(float(gl_SampleID)/4,gl_SamplePosition,1);}
