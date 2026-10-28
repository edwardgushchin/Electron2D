#version 450
void main(){vec2 p[3]=vec2[3](vec2(-.75,0),vec2(.75,0),vec2(0,.75));gl_Position=vec4(p[gl_VertexIndex%3],0,1);}
