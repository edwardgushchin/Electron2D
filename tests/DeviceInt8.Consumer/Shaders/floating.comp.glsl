#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_float16:require
layout(local_size_x=1)in;
layout(set=0,binding=0,std430)buffer Target { float value; } targetData;
void main(){float16_t a=float16_t(1.25),b=float16_t(0.5);targetData.value=float(a*b);}
