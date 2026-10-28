#version 450
layout(push_constant) uniform Push {ivec4 value;};
layout(location=0) out ivec4 color;void main(){color=value;}
