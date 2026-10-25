#version 450
layout(input_attachment_index=0,set=1,binding=0)uniform subpassInput image0;
layout(location=0)out vec4 result;
void main(){result=subpassLoad(image0);}
