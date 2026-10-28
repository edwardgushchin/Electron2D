#version 450
layout(push_constant) uniform Parameters {vec4 tint;vec4 controls;};
layout(location=0) out vec4 color;
void main(){color=tint;}
