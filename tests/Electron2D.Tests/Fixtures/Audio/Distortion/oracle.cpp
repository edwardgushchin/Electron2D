#include <cmath>
#include <cstdint>
#include <cstring>
#include <iomanip>
#include <iostream>
#include <vector>

struct AudioFrame { float left, right; };
static_assert(sizeof(AudioFrame) == sizeof(float) * 2);

namespace Math {
constexpr double TAU = 6.283185307179586476925286766559;
inline float db_to_linear(float value) { return std::exp(value * (float)0.11512925464970228420089957273422); }
}

inline float undenormalize(float value) {
    uint32_t bits;
    std::memcpy(&bits, &value, sizeof(bits));
    return (bits & 0x7f800000u) < 0x08000000u ? 0.0f : value;
}

class AudioServer {
public:
    static AudioServer *get_singleton() { static AudioServer value; return &value; }
    float rate = 44100;
    float get_mix_rate() const { return rate; }
};

class AudioEffectDistortion {
public:
    enum Mode { MODE_CLIP, MODE_ATAN, MODE_LOFI, MODE_OVERDRIVE, MODE_WAVESHAPE };
    Mode mode = MODE_CLIP;
    float pre_gain = 3;
    float post_gain = -2;
    float keep_hf_hz = 4000;
    float drive = 0;
};

class AudioEffectDistortionInstance {
public:
    AudioEffectDistortion *base;
    float h[2] = {};
    void process(const AudioFrame *p_src_frames, AudioFrame *p_dst_frames, int p_frame_count);
};

// generate.py extracts this function byte-for-byte from the pinned source.
#include "process.inc"

int main() {
    constexpr int frames = 256;
    constexpr float drives[] = { 0.65f, 0.8f, 0.4f, 0.9f, 0.75f };
    std::cout << std::setprecision(9) << '[';
    for (int rate : { 44100, 48000 }) {
      AudioServer::get_singleton()->rate = static_cast<float>(rate);
      for (int mode = 0; mode < 5; ++mode) {
        if (rate != 44100 || mode) std::cout << ',';
        AudioEffectDistortion effect;
        effect.mode = static_cast<AudioEffectDistortion::Mode>(mode);
        effect.drive = drives[mode];
        AudioEffectDistortionInstance instance;
        instance.base = &effect;
        std::vector<AudioFrame> input(frames), output(frames);
        for (int i = 0; i < frames; ++i) {
            input[i].left = 0.37f * std::sin(i * .081f) + (i % 53 == 0 ? .9f : 0);
            input[i].right = .29f * std::cos(i * .113f) - (i % 71 == 0 ? .7f : 0);
        }
        instance.process(input.data(), output.data(), frames);
        std::cout << "{\"Rate\":" << rate << ",\"Mode\":" << mode << ",\"Drive\":" << drives[mode] << ",\"Input\":[";
        for (int i = 0; i < frames; ++i) {
            if (i) std::cout << ',';
            std::cout << input[i].left << ',' << input[i].right;
        }
        std::cout << "],\"PCM\":[";
        for (int i = 0; i < frames; ++i) {
            if (i) std::cout << ',';
            std::cout << output[i].left << ',' << output[i].right;
        }
        std::cout << "]}";
      }
    }
    std::cout << "]\n";
}
