#version 450
layout(push_constant) uniform Parameters { vec4 tint; vec4 controls; };
void main(){int side=gl_VertexIndex/6,k=gl_VertexIndex%6;if((side!=0)!=(controls.y!=0))k=k/3*3+2-k%3;int corners[6]=int[](0,1,2,2,1,3);int c=corners[k];vec2 p=vec2(float(c&1)+side-1,float(c>>1)*2-1);gl_Position=vec4(p,controls.x,1);}
