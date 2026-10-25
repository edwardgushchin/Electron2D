#version 450
#extension GL_EXT_multiview : require
layout(set=0,binding=0)uniform sampler2D vertexImage;
layout(location=0)out vec4 tint;
void main(){vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2);gl_Position=vec4(p*2-1,0,1);tint=textureLod(vertexImage,vec2(.5),0)*float(gl_ViewIndex+1);}
