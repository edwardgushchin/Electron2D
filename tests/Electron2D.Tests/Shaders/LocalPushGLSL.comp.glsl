#version 450
layout(local_size_x=1) in;
struct Node { vec2 weights; float factor; };
layout(push_constant,std430) uniform Parameters { float values[3]; vec2 offset; mat2 transform; vec4 tint; uint index; uint target; Node node; } pc;
layout(set=3,binding=7,std430) buffer Output { float result[]; };
layout(set=0,binding=0,std140) uniform User { vec4 multiplier; };
float matrixValue(mat2 m) { return m[0][1]*2+m[1][0]*3; }
float evaluate(Node n) { return n.weights.x+n.weights.y+n.factor; }
void main() { result[pc.target]=(pc.values[pc.index]+pc.offset.x+pc.offset.y+pc.transform[0][0]+pc.transform[1][1]+evaluate(pc.node)+matrixValue(pc.transform))*multiplier.x+pc.tint.x; }
