#include "reverb_filter.h"
#include <algorithm>
#include <cstdio>
#include <vector>

int main() {
    constexpr int frames = 4096;
    std::vector<float> leftInput(frames), rightInput(frames), leftOutput(frames), rightOutput(frames);
    for (int i = 0; i < frames; i++) {
        leftInput[i] = i == 0 ? .75f : float((i * 37) % 101 - 50) / 512;
        rightInput[i] = i == 0 ? -.5f : float((i * 37 + 11) % 101 - 50) / 512;
    }
    std::printf("[");
    for (int profile = 0; profile < 3; profile++) {
        Reverb left, right;
        left.set_mix_rate(44100); right.set_mix_rate(44100);
        left.set_extra_spread_base(0); right.set_extra_spread_base(.000521f);
        Reverb* channels[] = { &left, &right };
        for (auto channel : channels) {
            channel->set_room_size(profile == 1 ? .9f : profile == 2 ? 0 : .8f);
            channel->set_damp(profile == 1 ? .2f : profile == 2 ? 1 : .5f);
            channel->set_wet(profile == 0 ? .5f : 1);
            channel->set_dry(profile == 0 ? 1 : 0);
            channel->set_predelay(profile == 1 ? 20 : profile == 2 ? 500 : 150);
            channel->set_predelay_feedback(profile == 1 ? .8f : profile == 2 ? 0 : .4f);
            channel->set_highpass(profile == 1 ? .5f : profile == 2 ? 1 : 0);
            channel->set_extra_spread(profile == 1 ? 0 : 1);
        }
        for (int offset = 0; offset < frames;) {
            int count = std::min(frames - offset, offset == 0 ? 113 : offset < 1024 ? 257 : 1024);
            left.process(leftInput.data() + offset, leftOutput.data() + offset, count);
            right.process(rightInput.data() + offset, rightOutput.data() + offset, count);
            offset += count;
        }
        std::printf("%s{\"Profile\":%d,\"PCM\":[", profile ? "," : "", profile);
        for (int i = 0; i < frames; i++) std::printf("%s%.9g,%.9g", i ? "," : "", leftOutput[i], rightOutput[i]);
        std::printf("]}");
    }
    std::printf("]\n");
}
