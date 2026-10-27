#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int8 : require
#extension GL_EXT_shader_explicit_arithmetic_types_int16 : require
#extension GL_EXT_shader_explicit_arithmetic_types_int64 : require
#extension GL_EXT_shader_8bit_storage : require
#extension GL_EXT_shader_16bit_storage : require
layout(set=0,binding=0,std430) buffer Values { layout(offset=0) uint8_t a;layout(offset=4) uint16_t b;layout(offset=8) int64_t c;layout(offset=16) double d; };
layout(location=0) out vec4 color;
void main(){if(all(lessThan(gl_FragCoord.xy,vec2(1)))){b=uint16_t(0xbeef);d=1.0001220703125LF;}color=vec4(.25,.5,.75,1);}
