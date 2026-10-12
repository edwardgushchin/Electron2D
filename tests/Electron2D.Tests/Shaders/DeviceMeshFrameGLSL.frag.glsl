#version 450
layout(location=0) in vec3 normal;
layout(location=1) in vec4 tangent;
layout(location=0) out vec4 outputColor;
void main(){ outputColor=vec4(clamp(normal.z*.5+.5,0,1),clamp(tangent.x*.5+.5,0,1),tangent.w<0?1:0,1); }
