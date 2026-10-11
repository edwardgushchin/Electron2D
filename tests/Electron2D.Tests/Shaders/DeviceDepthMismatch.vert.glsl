#version 450
layout(set=5,binding=7,std140) uniform Parameters { vec4 tint; vec4 controls; };
layout(set=2,binding=13) uniform sampler2D depthMap;
void main() { vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2)*2-1; if(controls.y!=0) p.x=-p.x; gl_Position=vec4(p,controls.x+textureLod(depthMap,vec2(.5),0).r,1); }
