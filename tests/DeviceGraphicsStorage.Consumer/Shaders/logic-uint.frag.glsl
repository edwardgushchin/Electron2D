#version 450
layout(push_constant) uniform Push {uvec4 value;};
layout(location=0) out uvec4 color;void main(){color=value;}
