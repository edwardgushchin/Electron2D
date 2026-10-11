#version 450
layout(set=5,binding=7,std140) uniform Parameters { vec4 tint; vec4 controls; };
layout(set=2,binding=13) uniform sampler2DArray depthMap;
layout(location=0) out vec4 color;
void main() { float d=textureLod(depthMap,vec3(.5,.5,controls.z),controls.w).r; color=vec4(d,d,d,1); }
