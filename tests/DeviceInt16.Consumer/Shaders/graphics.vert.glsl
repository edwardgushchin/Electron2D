#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_int16:require
#extension GL_EXT_shader_16bit_storage:require
layout(constant_id=0)const int16_t factor=int16_t(2);
layout(push_constant,std430)uniform Push { int16_t a; int16_t b; } pc;
layout(set=0,binding=0,std140)uniform Values { i16vec2 value; } parameters;
layout(location=0)out vec2 color;
void main(){vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);color=vec2(float(parameters.value.x+pc.a+factor),float(parameters.value.y+pc.b));}
