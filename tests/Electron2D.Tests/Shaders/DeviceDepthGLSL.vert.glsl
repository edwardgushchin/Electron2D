#version 450
layout(set=5,binding=7,std140) uniform Parameters { vec4 tint; vec4 controls; };
void main() { vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2)*2-1; if(controls.y!=0) p.x=-p.x; gl_Position=vec4(p,controls.x+controls.z*p.y,1); }
