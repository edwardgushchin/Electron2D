#version 450
layout(set=3,binding=7)uniform samplerBuffer texels;
layout(location=0)out vec4 color;
void main(){color=texelFetch(texels,4);}
