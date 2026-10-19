[[vk::input_attachment_index(0)]] [[vk::binding(7,6)]] SubpassInput<float4> source;
float4 main() : SV_Target0 { return source.SubpassLoad().bgra; }
