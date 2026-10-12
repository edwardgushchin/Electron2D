#version 450
layout(location=1) in vec2 uv;
layout(location=4) in vec2 uv2;
layout(location=5) in vec3 normal;
layout(location=6) in vec4 tangent;
layout(location=7) in vec4 custom0;
layout(location=8) in vec4 custom1;
layout(location=9) in vec4 custom2;
layout(location=10) in vec4 custom3;
layout(location=0) out vec4 outputColor;
void main(){bool valid=all(lessThan(abs(uv-uv2),vec2(.0001)))&&all(greaterThanEqual(uv,vec2(0)))&&all(lessThanEqual(uv,vec2(1)))&&normal.z<-.999&&tangent.x>.999&&tangent.w>.999&&abs(custom0.x-.25)<.01&&custom1.x<-.999&&custom2.x==.25&&custom3.x==.25;outputColor=valid?vec4(0,1,0,1):vec4(1,0,0,1);}
