#define WIN32_LEAN_AND_MEAN
#define _WINMM_
#include <windows.h>
#include <mmsystem.h>
#include <wchar.h>

typedef void (CALLBACK *MidiCallback)(HMIDIIN, UINT, DWORD_PTR, DWORD_PTR, DWORD_PTR);

typedef struct {
    MidiCallback callback;
    DWORD_PTR token;
    UINT device;
    BOOL open;
} Port;

static Port ports[64];
static UINT next_port;
static Port *active[2];
static Port *retired;
static UINT count = 2;
static BOOL fail_second_start;
static BOOL interface_present = TRUE;
static const WCHAR *names[] = { L"Fixture A", L"Fixture B \x03a9" };
static const WCHAR interface_path[] = L"\\\\?\\Electron2D-fixture";

#define EXPORT __declspec(dllexport)

#pragma comment(linker, "/export:midiInGetNumDevs")
UINT WINAPI midiInGetNumDevs(void) { return count; }

#pragma comment(linker, "/export:midiInGetDevCapsW")
MMRESULT WINAPI midiInGetDevCapsW(UINT_PTR id, LPMIDIINCAPSW caps, UINT size)
{
    if (id >= 2) {
        Port *port = (Port *)id;
        if (!port->open) return MMSYSERR_INVALHANDLE;
        id = port->device;
    }
    if (id >= count || size != sizeof(*caps)) return MMSYSERR_BADDEVICEID;
    ZeroMemory(caps, size);
    wcscpy_s(caps->szPname, MAXPNAMELEN, names[id]);
    return MMSYSERR_NOERROR;
}

#pragma comment(linker, "/export:midiInOpen")
MMRESULT WINAPI midiInOpen(LPHMIDIIN result, UINT device, DWORD_PTR callback, DWORD_PTR token, DWORD flags)
{
    if (device >= count || flags != (CALLBACK_FUNCTION | MIDI_IO_STATUS) || next_port == ARRAYSIZE(ports))
        return MMSYSERR_INVALPARAM;
    if (active[device]) return MMSYSERR_ALLOCATED;
    Port *port = &ports[next_port++];
    *port = (Port){ (MidiCallback)callback, token, device, TRUE };
    active[device] = port;
    *result = (HMIDIIN)port;
    port->callback(*result, MIM_OPEN, token, 0, 0);
    return MMSYSERR_NOERROR;
}

#pragma comment(linker, "/export:midiInStart")
MMRESULT WINAPI midiInStart(HMIDIIN handle)
{
    Port *port = (Port *)handle;
    return fail_second_start && port->device == 1 ? MMSYSERR_ERROR : MMSYSERR_NOERROR;
}

#pragma comment(linker, "/export:midiInClose")
MMRESULT WINAPI midiInClose(HMIDIIN handle)
{
    Port *port = (Port *)handle;
    if (!port->open) return MMSYSERR_INVALHANDLE;
    port->open = FALSE;
    active[port->device] = NULL;
    retired = port;
    port->callback(handle, MIM_CLOSE, port->token, 0, 0);
    return MMSYSERR_NOERROR;
}

#pragma comment(linker, "/export:midiInReset")
MMRESULT WINAPI midiInReset(HMIDIIN handle) { return handle ? MMSYSERR_NOERROR : MMSYSERR_INVALHANDLE; }

#pragma comment(linker, "/export:midiInMessage")
MMRESULT WINAPI midiInMessage(HMIDIIN id, UINT message, DWORD_PTR data, DWORD_PTR size)
{
    if ((UINT_PTR)id != 1) return MMSYSERR_NOTSUPPORTED;
    if (message == 0x80D) {
        *(DWORD *)data = sizeof(interface_path);
        return MMSYSERR_NOERROR;
    }
    if (message != 0x80C || size != sizeof(interface_path)) return MMSYSERR_INVALPARAM;
    CopyMemory((void *)data, interface_path, sizeof(interface_path));
    return MMSYSERR_NOERROR;
}

EXPORT ULONG WINAPI CM_Get_Device_Interface_PropertyW(const WCHAR *path, const void *key,
    ULONG *type, BYTE *enabled, ULONG *size, ULONG flags)
{
    if (wcscmp(path, interface_path) != 0 || !key || *size != 1 || flags != 0) return 1;
    *type = 0x11;
    *enabled = interface_present ? 0xFF : 0;
    return 0;
}

EXPORT void fixture_send(UINT device, UINT message, DWORD data)
{
    Port *port = device < 2 ? active[device] : retired;
    if (port) port->callback((HMIDIIN)port, message, port->token, data, 0);
}

EXPORT void fixture_sources(UINT number) { count = number; }
EXPORT void fixture_fail_start(BOOL fail) { fail_second_start = fail; }
EXPORT void fixture_interface(BOOL present) { interface_present = present; }
EXPORT UINT fixture_open_count(void) { return (active[0] != NULL) + (active[1] != NULL); }
