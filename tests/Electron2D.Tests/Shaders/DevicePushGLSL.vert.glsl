#version 450
layout(push_constant,std430) uniform Parameters { vec4 tint; vec4 controls; } pc;
void main() { vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2)*2-1; gl_Position=vec4(p*vec2(.5,1)+vec2(pc.controls.x,0),0,1); }
