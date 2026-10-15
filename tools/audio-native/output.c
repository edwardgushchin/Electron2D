#include "FAudio_internal.h"
#include <SDL3/SDL.h>

/* The four-field SDL3 platform payload is pinned by the FAudio manifest. This bridge is
 * compiled into that same target, so its FAudio layout/defines match the mixer exactly. */
typedef struct E2DOutput {
    FAudio *audio;
    SDL_AudioStream *stream;
    float *stagingBuffer;
    size_t stagingLen;
} E2DOutput;

static void Mix(void *userdata, SDL_AudioStream *stream, int additional, int total)
{
    E2DOutput *device = userdata;
    if (!device->audio->active) return;
    while (additional > 0) {
        SDL_memset(device->stagingBuffer, 0, device->stagingLen);
        FAudio_INTERNAL_UpdateEngine(device->audio, device->stagingBuffer);
        SDL_PutAudioStreamData(stream, device->stagingBuffer, (int)device->stagingLen);
        additional -= (int)device->stagingLen;
    }
}

static double Latency(SDL_AudioStream *stream)
{
    SDL_AudioSpec device, source;
    int frames = 0;
    if (!stream || !SDL_GetAudioDeviceFormat(SDL_GetAudioStreamDevice(stream), &device, &frames) ||
        !SDL_GetAudioStreamFormat(stream, &source, NULL)) return -1;
    int queued = SDL_GetAudioStreamQueued(stream);
    if (queued < 0 || frames <= 0 || device.freq <= 0 || source.freq <= 0 || source.channels <= 0) return -1;
    return frames / (double)device.freq + queued / ((double)source.freq * source.channels * sizeof(float));
}

FAUDIOAPI double e2d_audio_output_latency(FAudio *audio)
{
    E2DOutput current;
    if (!audio || !audio->platform) return -1;
    SDL_memcpy(&current, audio->platform, sizeof(current));
    return Latency(current.stream);
}

FAUDIOAPI int e2d_audio_select_output(FAudio *audio, SDL_AudioDeviceID id)
{
    E2DOutput current;
    if (!audio || !audio->platform) return SDL_SetError("Audio output has no prepared platform stream"), 0;
    SDL_memcpy(&current, audio->platform, sizeof(current));
    SDL_AudioSpec source;
    if (!current.stream || !SDL_GetAudioStreamFormat(current.stream, &source, NULL)) return 0;
    E2DOutput *next = SDL_calloc(1, sizeof(*next));
    if (!next) return SDL_OutOfMemory(), 0;
    next->audio = audio; next->stagingLen = current.stagingLen;
    next->stagingBuffer = SDL_calloc(1, next->stagingLen);
    if (!next->stagingBuffer) { SDL_free(next); return SDL_OutOfMemory(), 0; }
    next->stream = SDL_OpenAudioDeviceStream(id, &source, Mix, next);
    if (!next->stream) { SDL_free(next->stagingBuffer); SDL_free(next); return 0; }
    if (Latency(next->stream) < 0 || !SDL_ResumeAudioStreamDevice(next->stream)) { FAudio_PlatformQuit(next); return 0; }
    void *old = audio->platform;
    audio->platform = next;
    FAudio_PlatformQuit(old);
    return 1;
}

/* Adapted from the pinned FAudio master initialization. Offline output owns no
 * SDL stream; the ordinary voice destructor accepts a null platform. */
FAUDIOAPI uint32_t e2d_audio_create_offline_master(FAudio *audio,
    FAudioMasteringVoice **master, uint32_t channels, uint32_t rate)
{
    if (!audio || audio->master || !master ||
        (channels != 2 && channels != 4 && channels != 6 && channels != 8) ||
        rate < FAUDIO_MIN_SAMPLE_RATE || rate > FAUDIO_MAX_SAMPLE_RATE) return FAUDIO_E_INVALID_CALL;
    FAudioMasteringVoice *voice = audio->pMalloc(sizeof(FAudioVoice));
    if (!voice) return FAUDIO_E_OUT_OF_MEMORY;
    FAudio_zero(voice, sizeof(FAudioVoice));
    voice->audio = audio;
    voice->type = FAUDIO_VOICE_MASTER;
    voice->effectLock = FAudio_PlatformCreateMutex();
    voice->volumeLock = FAudio_PlatformCreateMutex();
    voice->volume = 1.0f;
    voice->master.inputChannels = channels;
    voice->master.inputSampleRate = rate;
    FAudioVoice_SetEffectChain(voice, NULL);
    audio->master = voice;
    WriteWaveFormatExtensible(&audio->mixFormat, channels, rate, &DATAFORMAT_SUBTYPE_IEEE_FLOAT);
    audio->updateSize = rate / 100;
    *master = voice;
    return 0;
}

FAUDIOAPI void e2d_audio_mix_offline(FAudio *audio, float *output)
{
    FAudio_zero(output, audio->updateSize * audio->master->outputChannels * sizeof(float));
    FAudio_INTERNAL_UpdateEngine(audio, output);
}
