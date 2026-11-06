#define UNICODE
#include <windows.h>
#include <audioclient.h>
#include <audioclientactivationparams.h>
#include <mmdeviceapi.h>
#include <wrl.h>
#include <cstdio>
#include <string>
#include <vector>
#include <cmath>

using Microsoft::WRL::ComPtr;
using Microsoft::WRL::RuntimeClass;
using Microsoft::WRL::RuntimeClassFlags;
using Microsoft::WRL::ClassicCom;
using Microsoft::WRL::FtmBase;

class Activation final : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IActivateAudioInterfaceCompletionHandler, FtmBase> {
public:
    HANDLE ready = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    HRESULT result = E_PENDING;
    ComPtr<IAudioClient> client;
    ~Activation() { if (ready) CloseHandle(ready); }
    HRESULT STDMETHODCALLTYPE ActivateCompleted(IActivateAudioInterfaceAsyncOperation* operation) override {
        ComPtr<IUnknown> value; HRESULT activated = E_FAIL;
        result = operation->GetActivateResult(&activated, &value);
        if (SUCCEEDED(result)) result = activated;
        if (SUCCEEDED(result)) result = value.As(&client);
        SetEvent(ready); return S_OK;
    }
};

static int failure(const char* operation, HRESULT code) { fprintf(stderr, "%s HRESULT=%08lx\n", operation, code); return 1; }
static void marker(const std::wstring& path) { FILE* file = nullptr; _wfopen_s(&file, path.c_str(), L"wb"); if (file) fclose(file); }

int wmain(int argc, wchar_t** argv) {
    if (argc != 3) return 2;
    const DWORD target = wcstoul(argv[1], nullptr, 10);
    HANDLE process = OpenProcess(SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION, FALSE, target);
    if (!process) return failure("target process", HRESULT_FROM_WIN32(GetLastError()));
    HRESULT hr = CoInitializeEx(nullptr, COINIT_MULTITHREADED); if (FAILED(hr)) return failure("COM", hr);
    auto activation = Microsoft::WRL::Make<Activation>();
    AUDIOCLIENT_ACTIVATION_PARAMS parameters{};
    parameters.ActivationType = AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK;
    parameters.ProcessLoopbackParams.TargetProcessId = target;
    parameters.ProcessLoopbackParams.ProcessLoopbackMode = PROCESS_LOOPBACK_MODE_INCLUDE_TARGET_PROCESS_TREE;
    PROPVARIANT property{}; property.vt = VT_BLOB;
    property.blob.cbSize = sizeof(parameters); property.blob.pBlobData = reinterpret_cast<BYTE*>(&parameters);
    ComPtr<IActivateAudioInterfaceAsyncOperation> operation;
    hr = ActivateAudioInterfaceAsync(VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK, __uuidof(IAudioClient), &property, activation.Get(), &operation);
    if (FAILED(hr)) return failure("activate", hr);
    if (WaitForSingleObject(activation->ready, 10000) != WAIT_OBJECT_0) return failure("activation timeout", E_PENDING);
    if (FAILED(activation->result)) return failure("activation result", activation->result);
    WAVEFORMATEX format{}; format.wFormatTag = WAVE_FORMAT_PCM; format.nChannels = 2; format.nSamplesPerSec = 44100;
    format.wBitsPerSample = 16; format.nBlockAlign = 4; format.nAvgBytesPerSec = 176400;
    hr = activation->client->Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK | AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM, 0, 0, &format, nullptr);
    if (FAILED(hr)) return failure("initialize capture", hr);
    HANDLE changed = CreateEventW(nullptr, FALSE, FALSE, nullptr); if (!changed) return 3;
    hr = activation->client->SetEventHandle(changed); if (FAILED(hr)) return failure("capture event", hr);
    ComPtr<IAudioCaptureClient> capture; hr = activation->client->GetService(IID_PPV_ARGS(&capture)); if (FAILED(hr)) return failure("capture service", hr);
    hr = activation->client->Start(); if (FAILED(hr)) return failure("capture start", hr);
    std::wstring stem = argv[2]; marker(stem + L".ready");
    std::vector<short> samples; const auto started = GetTickCount64();
    while (GetTickCount64() - started < 20000 && GetFileAttributesW((stem + L".stop").c_str()) == INVALID_FILE_ATTRIBUTES && WaitForSingleObject(process, 0) == WAIT_TIMEOUT) {
        WaitForSingleObject(changed, 100);
        UINT32 available = 0;
        while (SUCCEEDED(hr = capture->GetNextPacketSize(&available)) && available) {
            BYTE* data = nullptr; UINT32 frames = 0; DWORD flags = 0;
            hr = capture->GetBuffer(&data, &frames, &flags, nullptr, nullptr); if (FAILED(hr)) return failure("capture read", hr);
            auto old = samples.size(); samples.resize(old + frames * 2, 0);
            if (!(flags & AUDCLNT_BUFFERFLAGS_SILENT)) memcpy(samples.data() + old, data, frames * 4);
            capture->ReleaseBuffer(frames);
        }
        if (FAILED(hr)) return failure("capture packet", hr);
    }
    activation->client->Stop();
    FILE* file = nullptr; _wfopen_s(&file, (stem + L".pcm").c_str(), L"wb"); if (!file) return 4;
    fwrite(samples.data(), sizeof(short), samples.size(), file); fclose(file);
    long long energy = 0; int peak = 0; size_t nonzero = 0;
    for (auto sample : samples) { int value = sample; peak = max(peak, abs(value)); energy += static_cast<long long>(value) * value; nonzero += value != 0; }
    _wfopen_s(&file, (stem + L".txt").c_str(), L"wb"); if (!file) return 5;
    fprintf(file, "target_pid=%lu\nrate=44100 channels=2 bits=16\nframes=%zu\nnonzero=%zu\npeak=%d\nrms=%.8f\n", target, samples.size() / 2, nonzero, peak, samples.empty() ? 0 : sqrt(static_cast<double>(energy) / samples.size()));
    fclose(file); CloseHandle(changed); CloseHandle(process);
    capture.Reset(); operation.Reset(); activation.Reset(); CoUninitialize(); return 0;
}
