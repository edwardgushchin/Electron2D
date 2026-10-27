#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int16:require
layout(local_size_x=1)in;
layout(set=0,binding=0,std430)buffer Target { int value; } targetData;
void main(){int16_t a=int16_t(gl_GlobalInvocationID.x)-int16_t(123),b=int16_t(3);targetData.value=int(a*b);}
