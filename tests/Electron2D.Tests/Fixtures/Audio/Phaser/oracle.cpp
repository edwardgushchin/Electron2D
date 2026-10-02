#include <cmath>
#include <iomanip>
#include <iostream>
#include <vector>

struct AudioFrame { float left, right; };
namespace Math { constexpr float TAU = 6.2831853071795864769f; }

class AudioServer {
public:
    static AudioServer *get_singleton() { static AudioServer value; return &value; }
    float rate = 44100;
    float get_mix_rate() const { return rate; }
};

class AudioEffectPhaser {
public:
    float range_min = 440, range_max = 1600, rate = .5f, feedback = .7f, depth = 1;
};

class AudioEffectPhaserInstance {
    struct AllpassDelay {
        float a = 0, h = 0;
        void delay(float d) { a = (1.f - d) / (1.f + d); }
        float update(float s) { float y = s * -a + h; h = y * a + s; return y; }
    };
public:
    AudioEffectPhaser *base;
    float phase = 0;
    AudioFrame h = {0, 0};
    AllpassDelay allpass[2][6];
    void process(const AudioFrame *p_src_frames, AudioFrame *p_dst_frames, int p_frame_count);
};

// generate.py extracts the pinned process function byte-for-byte.
#include "process.inc"

int main() {
    constexpr int frames = 4096;
    std::cout << std::setprecision(9) << '[';
    for (int rate : {44100, 48000}) for (int profile = 0; profile < 2; ++profile) {
        if (rate != 44100 || profile) std::cout << ',';
        AudioServer::get_singleton()->rate = (float)rate;
        AudioEffectPhaser effect;
        if (profile) { effect.range_min = 100; effect.range_max = 8500; effect.rate = 20; effect.feedback = .9f; effect.depth = 2; }
        AudioEffectPhaserInstance instance; instance.base = &effect;
        std::vector<AudioFrame> input(frames), output(frames);
        for (int i = 0; i < frames; ++i) {
            input[i].left = .2f * std::sin(i * .037f) + .05f * std::cos(i * .002f);
            input[i].right = .3f * std::cos(i * .043f) - .04f * std::sin(i * .005f);
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
