#version 450
#extension GL_EXT_shader_explicit_arithmetic_types_float16:require
#extension GL_EXT_shader_16bit_storage:require
layout(constant_id=0)const float16_t factor=float16_t(0.25);
layout(push_constant,std430)uniform Push { float16_t a; float16_t b; } pc;
layout(set=0,binding=0,std140)uniform Values { f16vec2 value; } parameters;
layout(location=0)out vec2 color;
void main(){vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);color=vec2(float(parameters.value.x+pc.a+factor),float(parameters.value.y+pc.b));}
