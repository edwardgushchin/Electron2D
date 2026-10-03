#include <algorithm>
#include <cmath>
#include <iomanip>
#include <iostream>
#include <vector>

struct AudioFrame { float left, right; };
namespace Math {
inline float db_to_linear(float value) { return std::exp(value * (float)0.11512925464970228420089957273422); }
inline float linear_to_db(float value) { return std::log(value) * (float)8.6858896380650365530225783783321; }
inline float abs(float value) { return std::fabs(value); }
}
#define MIN(a, b) std::min(a, b)
class AudioEffectLimiter {
public:
    float threshold, ceiling, soft_clip, soft_clip_ratio;
};
class AudioEffectLimiterInstance {
public:
    AudioEffectLimiter *base;
    void process(const AudioFrame *p_src_frames, AudioFrame *p_dst_frames, int p_frame_count);
};
// generate.py extracts this function byte-for-byte from the pinned source.
#include "process.inc"

int main() {
    const AudioEffectLimiter profiles[] = {
        {0, -.1f, 2, 10}, {-18, -9, 6, 3}, {-30, -20, 0, 20}, {0, 0, 0, 10},
        {6, 3, 6, 10}, {-4, -1, 4, 10}, {0, -.1f, 2, -999}, {0, -.1f, 2, 999}
    };
    std::cout << std::setprecision(9) << '[';
    for (int profile = 0; profile < 8; ++profile) {
        if (profile) std::cout << ',';
        auto effect = profiles[profile];
        AudioEffectLimiterInstance instance; instance.base = &effect;
        std::vector<AudioFrame> input(256), output(256);
        for (int i = 0; i < 256; ++i) {
            input[i] = {(i - 128) * .021f, (128 - i) * .017f};
        }
        instance.process(input.data(), output.data(), (int)input.size());
        std::cout << "{\"ThresholdDB\":" << effect.threshold << ",\"CeilingDB\":" << effect.ceiling
                  << ",\"SoftClipDB\":" << effect.soft_clip << ",\"SoftClipRatio\":" << effect.soft_clip_ratio << ",\"Input\":[";
        for (size_t i = 0; i < input.size(); ++i) { if (i) std::cout << ','; std::cout << input[i].left << ',' << input[i].right; }
        std::cout << "],\"PCM\":[";
        for (size_t i = 0; i < output.size(); ++i) { if (i) std::cout << ','; std::cout << output[i].left << ',' << output[i].right; }
        std::cout << "]}";
    }
    std::cout << "]\n";
}
