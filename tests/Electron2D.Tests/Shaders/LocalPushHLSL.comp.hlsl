struct Node { float2 weights; float factor; };
struct Parameters { float values[3]; float2 offset; float2x2 transform; float4 tint; uint index; uint target; Node node; };
[[vk::push_constant]] ConstantBuffer<Parameters> pc;
RWStructuredBuffer<float> result : register(u7,space3);
cbuffer User : register(b0,space0) { float4 multiplier; }
float matrixValue(float2x2 m) { return m[0][1]*2+m[1][0]*3; }
float evaluate(Node n) { return n.weights.x+n.weights.y+n.factor; }
[numthreads(1,1,1)]
void main() { result[pc.target]=(pc.values[pc.index]+pc.offset.x+pc.offset.y+pc.transform[0][0]+pc.transform[1][1]+evaluate(pc.node)+matrixValue(pc.transform))*multiplier.x+pc.tint.x; }
