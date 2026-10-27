#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int8:require
layout(local_size_x=1)in;
layout(set=0,binding=0,std430)buffer Target { int value; } targetData;
void main(){int8_t a=int8_t(gl_GlobalInvocationID.x)-int8_t(7),b=int8_t(3);targetData.value=int(a*b);}
