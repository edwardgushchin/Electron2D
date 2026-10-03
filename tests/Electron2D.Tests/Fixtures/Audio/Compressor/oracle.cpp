#include <algorithm>
#include <cmath>
#include <iomanip>
#include <iostream>
#include <string>
#include <vector>
struct AudioFrame {
    float left = 0, right = 0;
    AudioFrame operator*(float value) const { return {left * value, right * value}; }
    AudioFrame operator+(AudioFrame value) const { return {left + value.left, right + value.right}; }
};
namespace Math {
inline float db_to_linear(float value) { return std::exp(value * (float)0.11512925464970228420089957273422); }
inline float linear_to_db(float value) { return std::log(value) * (float)8.6858896380650365530225783783321; }
inline float abs(float value) { return std::fabs(value); }
}
#define MAX(a, b) std::max(a, b)
using StringName = std::string;
class AudioServer {
public:
    float rate = 44100;
    std::vector<AudioFrame> detector;
    static AudioServer *get_singleton() { static AudioServer value; return &value; }
    float get_mix_rate() const { return rate; }
    int thread_find_bus_index(const StringName &name) { return name == "Detector" ? 1 : 0; }
    const AudioFrame *thread_get_channel_mix_buffer(int bus, int channel) { return detector.data(); }
};
class AudioEffectCompressor {
public:
    float threshold = 0, ratio = 4, gain = 0, attack_us = 20, release_ms = 250, mix = 1;
    StringName sidechain;
};
class AudioEffectCompressorInstance {
public:
    AudioEffectCompressor *base;
    float rundb = 0, averatio = 0, runratio = 0, runmax = 0, maxover = 0, gr_meter = 1;
    int current_channel = -1;
    void process(const AudioFrame *p_src_frames, AudioFrame *p_dst_frames, int p_frame_count);
};
#include "process.inc"
int main() {
    std::cout << std::setprecision(9) << '['; bool first = true;
    for (int rate : {44100, 48000}) for (int profile = 0; profile < 7; ++profile) {
        if (!first) std::cout << ','; first = false;
        auto server = AudioServer::get_singleton(); server->rate = (float)rate;
        AudioEffectCompressor effect;
        if (profile == 1) { effect.threshold = -18; effect.gain = 3; effect.attack_us = 250; effect.release_ms = 30; }
        if (profile == 2) { effect.threshold = -24; effect.ratio = 12; effect.gain = -3; effect.attack_us = 1800; effect.release_ms = 1000; effect.mix = .35f; }
        if (profile == 3) { effect.threshold = -6; effect.ratio = .5f; effect.attack_us = 0; effect.release_ms = 0; effect.mix = .5f; }
        if (profile == 4 || profile == 5) { effect.threshold = -18; effect.sidechain = profile == 4 ? "Detector" : "Missing"; }
        if (profile == 6) { effect.threshold = -18; effect.ratio = -4; effect.mix = 1.5f; }
        std::vector<AudioFrame> input(1024), output(1024); server->detector.resize(1024);
        for (int i = 0; i < 1024; ++i) {
            input[i] = i >= 700 ? AudioFrame{} : AudioFrame{.6f * std::sin(i * .043f), .8f * std::cos(i * .057f)};
            server->detector[i] = i < 350 ? AudioFrame{.9f, -.7f} : AudioFrame{};
        }
        AudioEffectCompressorInstance instance; instance.base = &effect; if (!effect.sidechain.empty()) instance.current_channel = 0;
        instance.process(input.data(), output.data(), (int)input.size());
        std::cout << "{\"Rate\":" << rate << ",\"Profile\":" << profile << ",\"Threshold\":" << effect.threshold
                  << ",\"Ratio\":" << effect.ratio << ",\"Gain\":" << effect.gain << ",\"AttackUS\":" << effect.attack_us
                  << ",\"ReleaseMS\":" << effect.release_ms << ",\"Mix\":" << effect.mix << ",\"Sidechain\":\"" << effect.sidechain << "\",\"Input\":[";
        for (size_t i = 0; i < input.size(); ++i) { if (i) std::cout << ','; std::cout << input[i].left << ',' << input[i].right; }
        std::cout << "],\"Detector\":[";
        for (size_t i = 0; i < input.size(); ++i) { if (i) std::cout << ','; std::cout << server->detector[i].left << ',' << server->detector[i].right; }
        std::cout << "],\"PCM\":[";
        for (size_t i = 0; i < output.size(); ++i) { if (i) std::cout << ','; std::cout << output[i].left << ',' << output[i].right; }
        std::cout << "]}";
    }
    std::cout << "]\n";
}
