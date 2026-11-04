"""Generate private SDL dispatch thunks from the pinned declarations and render calls."""
from pathlib import Path
import re
import hashlib
import sys


def generate(source, render, output):
    header = source / "src/dynapi/SDL_dynapi_procs.h"
    if hashlib.sha256(header.read_bytes()).hexdigest() != "365cca5f0da871d9d88446383cd64c1e070daf408e38e14325d6b9db128bb69e":
        raise ValueError("The pinned SDL dispatch declarations changed")
    declarations = {}
    for line in header.read_text().splitlines():
        match = re.fullmatch(r"SDL_DYNAPI_PROC\(([^,]+),(SDL_\w+),\((.*)\),\((.*)\),([^)]*)\)", line)
        if match:
            declarations[match[2]] = (match[1], match[3], match[4])
    cmake = (render / "CMakeLists.txt").read_text()
    sources = re.search(r"add_library\(Electron2DRender .*?\)", cmake)[0]
    names = set()
    for filename in re.findall(r"\b[\w-]+\.c\b", sources):
        names.update(re.findall(r"\b(SDL_\w+)\s*\(", (render / filename).read_text()))
    # Private inline helpers call SDL too.
    for path in render.glob("*.h"):
        names.update(re.findall(r"\b(SDL_\w+)\s*\(", path.read_text()))
    names &= declarations.keys()
    if "SDL_SetError" in names:
        names.remove("SDL_SetError")
        names.add("SDL_SetErrorV")
    log_names = names & {"SDL_Log", "SDL_LogTrace", "SDL_LogVerbose", "SDL_LogDebug", "SDL_LogInfo",
                         "SDL_LogWarn", "SDL_LogError", "SDL_LogCritical", "SDL_LogMessage"}
    if log_names:
        names -= log_names
        names.add("SDL_LogMessageV")
    lines = ['#define SDL_STATIC_LIB', '#include <SDL3/SDL.h>', '#include <stdint.h>',
             '#include <stdarg.h>', '#ifdef _WIN32', '#include <windows.h>',
             'static void *symbol(void *handle, const char *name) { return (void *)GetProcAddress((HMODULE)handle, name); }',
             '#define E2D_API __declspec(dllexport)',
             'static void *volatile bound;', 'static SRWLOCK binding = SRWLOCK_INIT;',
             'static uintptr_t current(void) { return (uintptr_t)InterlockedCompareExchangePointer(&bound, NULL, NULL); }',
             'static void publish(void *handle) { InterlockedExchangePointer(&bound, handle); }',
             'static void lock(void) { AcquireSRWLockExclusive(&binding); }',
             'static void unlock(void) { ReleaseSRWLockExclusive(&binding); }',
             '#else', '#include <dlfcn.h>', '#include <stdatomic.h>',
             'static void *symbol(void *handle, const char *name) { return dlsym(handle, name); }',
             '#define E2D_API __attribute__((visibility("default")))',
             'static atomic_uintptr_t bound;', 'static atomic_flag binding = ATOMIC_FLAG_INIT;',
             'static uintptr_t current(void) { return atomic_load_explicit(&bound, memory_order_acquire); }',
             'static void publish(void *handle) { atomic_store_explicit(&bound, (uintptr_t)handle, memory_order_release); }',
             'static void lock(void) { while (atomic_flag_test_and_set_explicit(&binding, memory_order_acquire)) {} }',
             'static void unlock(void) { atomic_flag_clear_explicit(&binding, memory_order_release); }', '#endif']
    # SDL headers may alias optimized memory calls to libc. Defining a thunk through
    # that macro would replace libc throughout the common module before SDL binds.
    lines.extend("#undef " + name for name in sorted(names | log_names | {"SDL_SetError"}))
    for name in sorted(names):
        result, params, args = declarations[name]
        lines.append(f'static {result} (SDLCALL *call_{name})({params});')
    lines.extend(['E2D_API int e2d_native_bind_sdl(void *handle) {',
                  '    if (!handle) return 0;',
                  '    lock();',
                  '    uintptr_t previous = current();',
                  '    if (previous) { unlock(); return previous == (uintptr_t)handle; }'])
    for name in sorted(names):
        lines.append(f'    void *p_{name} = symbol(handle, "{name}");')
        lines.append(f'    if (!p_{name}) {{ unlock(); return 0; }}')
    for name in sorted(names):
        result, params, args = declarations[name]
        lines.append(f'    call_{name} = ({result} (SDLCALL *)({params}))p_{name};')
    lines.extend(['    publish(handle);',
                  '    unlock(); return 1;', '}'])
    for name in sorted(names):
        result, params, args = declarations[name]
        if "..." in params:
            raise ValueError("Unexpected variadic SDL dispatch: " + name)
        fallback = 'return;' if result == 'void' else 'return 0;'
        invoke = '' if result == 'void' else 'return '
        lines.append(f'{result} SDLCALL {name}({params}) {{')
        lines.append(f'    if (!current()) {{ {fallback} }}')
        lines.append(f'    {invoke}call_{name}({args});')
        lines.append('}')
    lines.extend(['bool SDLCALL SDL_SetError(const char *format, ...) {',
                  '    va_list args; va_start(args, format); bool result = SDL_SetErrorV(format, args); va_end(args); return result;', '}'])
    for name in sorted(log_names):
        if name == "SDL_Log":
            params, category, priority = "const char *format, ...", "SDL_LOG_CATEGORY_APPLICATION", "SDL_LOG_PRIORITY_INFO"
        elif name == "SDL_LogMessage":
            params, category, priority = "int category, SDL_LogPriority priority, const char *format, ...", "category", "priority"
        else:
            params, category, priority = "int category, const char *format, ...", "category", "SDL_LOG_PRIORITY_" + name.removeprefix("SDL_Log").upper()
        lines.extend([f"void SDLCALL {name}({params}) {{",
                      "    va_list args; va_start(args, format);",
                      f"    SDL_LogMessageV({category}, {priority}, format, args); va_end(args);", "}"])
    output.write_text('\n'.join(lines) + '\n')
    print(f"Generated {len(names)} private SDL entry points")


if __name__ == "__main__":
    generate(*(Path(value).resolve() for value in sys.argv[1:]))
