#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int8 : require
#extension GL_EXT_shader_explicit_arithmetic_types_int16 : require
#extension GL_EXT_shader_explicit_arithmetic_types_int64 : require
#extension GL_EXT_shader_8bit_storage : require
#extension GL_EXT_shader_16bit_storage : require
layout(set=0,binding=0,std430) buffer Values { layout(offset=0) uint8_t a;layout(offset=4) uint16_t b;layout(offset=8) int64_t c;layout(offset=16) double d; };
void main(){if(gl_VertexIndex==0){a=uint8_t(0xa5);c=int64_t(-1234567);}vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);}
