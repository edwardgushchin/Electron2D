#version 450
layout(location=0) in float brightness;
layout(location=0) out vec4 c0;
layout(location=1) out vec4 c1;
layout(location=2) out vec4 c2;
layout(location=3) out vec4 c3;
layout(location=4) out vec4 c4;
layout(location=5) out vec4 c5;
layout(location=6) out vec4 c6;
layout(location=7) out vec4 c7;
void main() { c0=vec4(1.0/8.0,brightness,0,1);c1=vec4(2.0/8.0,brightness,0,1);c2=vec4(3.0/8.0,brightness,0,1);c3=vec4(4.0/8.0,brightness,0,1);c4=vec4(5.0/8.0,brightness,0,1);c5=vec4(6.0/8.0,brightness,0,1);c6=vec4(7.0/8.0,brightness,0,1);c7=vec4(8.0/8.0,brightness,0,1); }
