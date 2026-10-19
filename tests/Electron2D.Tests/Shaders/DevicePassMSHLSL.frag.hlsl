[[vk::input_attachment_index(0)]] [[vk::binding(7,6)]] SubpassInputMS<float4> source;
float4 main() : SV_Target0 { return (source.SubpassLoad(0)+source.SubpassLoad(1)+source.SubpassLoad(2)+source.SubpassLoad(3)).bgra*.25; }
