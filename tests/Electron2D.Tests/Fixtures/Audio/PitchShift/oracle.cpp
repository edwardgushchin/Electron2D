#include <cmath>
#include <cstdint>
#include <cstring>
#include <iomanip>
#include <iostream>
#include <vector>

namespace Math { constexpr double PI = 3.1415926535897932384626433832795; }
#define unlikely(value) (value)
#define ERR_PRINT_ONCE(message) (std::cerr << message << '\n')

class SMBPitchShift {
    enum { MAX_FRAME_LENGTH = 8192 };
    float gInFIFO[MAX_FRAME_LENGTH] = {};
    float gOutFIFO[MAX_FRAME_LENGTH] = {};
    float gFFTworksp[2 * MAX_FRAME_LENGTH] = {};
    float gLastPhase[MAX_FRAME_LENGTH / 2 + 1] = {};
    float gSumPhase[MAX_FRAME_LENGTH / 2 + 1] = {};
    float gOutputAccum[2 * MAX_FRAME_LENGTH] = {};
    float gAnaFreq[MAX_FRAME_LENGTH] = {};
    float gAnaMagn[MAX_FRAME_LENGTH] = {};
    float gSynFreq[MAX_FRAME_LENGTH] = {};
    float gSynMagn[MAX_FRAME_LENGTH] = {};
    long gRover = 0;
    void smbFft(float *fftBuffer, long fftFrameSize, long sign);
public:
    void PitchShift(float pitchShift, long numSampsToProcess, long fftFrameSize, long osamp,
                    float sampleRate, float *indata, float *outdata, int stride);
};

// generate.py extracts both functions byte-for-byte from the pinned source.
#include "process.inc"

int main() {
    struct Profile { int size, overlap, frames; float scale; };
    constexpr Profile profiles[] = {{256, 4, 2048, 1.5f}, {2048, 8, 6144, .75f}};
    std::cout << std::setprecision(9) << '[';
    bool first = true;
    for (int rate : {44100, 48000}) for (auto profile : profiles) {
        if (!first) std::cout << ',';
        first = false;
        SMBPitchShift left, right;
        std::vector<float> input(profile.frames * 2), output(profile.frames * 2);
        for (int i = 0; i < profile.frames; ++i) {
            input[2 * i] = .37f * std::sin(i * .037f) + (i % 701 == 0 ? .6f : 0);
            input[2 * i + 1] = .31f * std::cos(i * .046f) - (i % 911 == 0 ? .5f : 0);
        }
        left.PitchShift(profile.scale, profile.frames, profile.size, profile.overlap, (float)rate,
                        input.data(), output.data(), 2);
        right.PitchShift(profile.scale, profile.frames, profile.size, profile.overlap, (float)rate,
                         input.data() + 1, output.data() + 1, 2);
        std::cout << "{\"Rate\":" << rate << ",\"Size\":" << profile.size
                  << ",\"Overlap\":" << profile.overlap << ",\"Scale\":" << profile.scale << ",\"Input\":[";
        for (size_t i = 0; i < input.size(); ++i) { if (i) std::cout << ','; std::cout << input[i]; }
        std::cout << "],\"PCM\":[";
        for (size_t i = 0; i < output.size(); ++i) { if (i) std::cout << ','; std::cout << output[i]; }
        std::cout << "]}";
    }
    std::cout << "]\n";
}
