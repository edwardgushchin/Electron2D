#version 450
layout(push_constant) uniform Push {vec4 value;};
layout(location=0) out vec4 color;void main(){color=value;}
