#include "eq_filter.h"
#include <cmath>
#include <cstdio>
#include <vector>

int main() {
    const EQ::Preset presets[] = { EQ::PRESET_6_BANDS, EQ::PRESET_10_BANDS, EQ::PRESET_21_BANDS };
    std::printf("[");
    for (int kind = 0; kind < 3; kind++) {
        EQ eq;
        eq.set_mix_rate(44100);
        eq.set_preset_band_mode(presets[kind]);
        const int count = eq.get_band_count();
        std::vector<EQ::BandProcess> left(count), right(count);
        for (int band = 0; band < count; band++) {
            left[band] = eq.get_band_processor(band);
            right[band] = eq.get_band_processor(band);
        }
        if (kind) std::printf(",");
        std::printf("{\"Bands\":%d,\"PCM\":[", count);
        for (int frame = 0; frame < 256; frame++) {
            float inputLeft = frame == 0 ? .75f : float((frame * 37) % 101 - 50) / 128;
            float inputRight = frame == 0 ? -.5f : float((frame * 37 + 11) % 101 - 50) / 128;
            float outputLeft = 0, outputRight = 0;
            for (int band = 0; band < count; band++) {
                float sampleLeft = inputLeft, sampleRight = inputRight;
                left[band].process_one(sampleLeft);
                right[band].process_one(sampleRight);
                const float db = band == 0 ? 6 : band == count - 1 ? -9 : 0;
                const float gain = std::exp(db * 0.11512925464970228420089957273422f);
                outputLeft += sampleLeft * gain;
                outputRight += sampleRight * gain;
            }
            std::printf("%s%.9g,%.9g", frame ? "," : "", outputLeft, outputRight);
        }
        std::printf("]}");
    }
    std::printf("]\n");
}
