#include <algorithm>
#include <cmath>
#include <iomanip>
#include <iostream>
#include <vector>

struct AudioFrame { float left, right; };
namespace Math {
inline float db_to_linear(float value) { return std::exp(value * (float)0.11512925464970228420089957273422); }
inline float abs(float value) { return std::fabs(value); }
inline float lerp(float from, float to, float weight) { return from + (to - from) * weight; }
}
#define MAX(a, b) std::max(a, b)
#define MIN(a, b) std::min(a, b)

class AudioServer {
public:
    static AudioServer *get_singleton() { static AudioServer value; return &value; }
    float rate = 44100;
    float get_mix_rate() const { return rate; }
};

class AudioEffectHardLimiter {
public:
    float pre_gain = 0, ceiling = -.3f, sustain = .02f, release = .1f;
    const float attack = .002f;
};

class AudioEffectHardLimiterInstance {
public:
    AudioEffectHardLimiter *base;
    int sample_cursor = 0;
    float release_factor = 0, attack_factor = 0, gain = 1, gain_target = 1;
    std::vector<float> sample_buffer_left, sample_buffer_right;
    int gain_samples_to_store = 0, gain_bucket_cursor = 0, gain_bucket_size = 0;
    std::vector<float> gain_buckets;
    void process(const AudioFrame *p_src_frames, AudioFrame *p_dst_frames, int p_frame_count);
};

// generate.py extracts this function byte-for-byte from the pinned source.
#include "process.inc"

int main() {
    constexpr int frames = 4096;
    std::cout << std::setprecision(9) << '[';
    for (int rate : {44100, 48000}) for (int profile = 0; profile < 2; ++profile) {
        if (rate != 44100 || profile) std::cout << ',';
        AudioServer::get_singleton()->rate = (float)rate;
        AudioEffectHardLimiter effect;
        if (profile) { effect.pre_gain = 6; effect.ceiling = -9; effect.release = .015f; }
        AudioEffectHardLimiterInstance instance;
        instance.base = &effect;
        for (int i = 0; i < (int)std::ceil(rate * effect.attack) + 1; ++i) {
            instance.sample_buffer_left.push_back(0);
            instance.sample_buffer_right.push_back(0);
        }
        instance.gain_samples_to_store = (int)std::ceil(rate * (effect.attack + effect.sustain) + 1);
        instance.gain_bucket_size = (int)(rate * effect.attack);
        for (int i = 0; i < instance.gain_samples_to_store; i += instance.gain_bucket_size) instance.gain_buckets.push_back(1);
        std::vector<AudioFrame> input(frames), output(frames);
        for (int i = 0; i < frames; ++i) {
            input[i].left = .28f * std::sin(i * .071f) + (i % 1133 == 0 ? 1.9f : 0);
            input[i].right = .31f * std::cos(i * .061f) - (i % 997 == 0 ? 1.55f : 0);
        }
        instance.process(input.data(), output.data(), frames);
        std::cout << "{\"Rate\":" << rate << ",\"Profile\":" << profile << ",\"Input\":[";
        for (int i = 0; i < frames; ++i) { if (i) std::cout << ','; std::cout << input[i].left << ',' << input[i].right; }
        std::cout << "],\"PCM\":[";
        for (int i = 0; i < frames; ++i) { if (i) std::cout << ','; std::cout << output[i].left << ',' << output[i].right; }
        std::cout << "]}";
    }
    std::cout << "]\n";
}
