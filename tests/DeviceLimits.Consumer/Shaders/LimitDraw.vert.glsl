#version 450
layout(location=0) in float v0;
layout(location=1) in float v1;
layout(location=2) in float v2;
layout(location=3) in float v3;
layout(location=4) in float v4;
layout(location=5) in float v5;
layout(location=6) in float v6;
layout(location=7) in float v7;
layout(location=8) in float v8;
layout(location=9) in float v9;
layout(location=10) in float v10;
layout(location=11) in float v11;
layout(location=12) in float v12;
layout(location=13) in float v13;
layout(location=14) in float v14;
layout(location=15) in float v15;
layout(location=0) out float brightness;
void main() { brightness=v0+v1+v2+v3+v4+v5+v6+v7+v8+v9+v10+v11+v12+v13+v14+v15; vec2 p=vec2((gl_VertexIndex<<1)&2,gl_VertexIndex&2); gl_Position=vec4(p*2-1,0,1); }
