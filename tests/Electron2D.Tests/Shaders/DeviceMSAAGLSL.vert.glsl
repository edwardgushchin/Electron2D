#version 450
layout(set=5,binding=7,std140) uniform Parameters { vec4 tint; vec4 controls; };
void main() { vec2 p[3]=vec2[3](vec2(-.83,-.71),vec2(.91,-.57),vec2(-.61,.93)); gl_Position=vec4(p[gl_VertexIndex],controls.x,1); }
