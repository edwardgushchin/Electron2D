# Logger API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/Logger.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Logger`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
| [`enum ErrorType`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
| [`enum_value ERROR_TYPE_ERROR [ErrorType] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
| [`enum_value ERROR_TYPE_SCRIPT [ErrorType] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
| [`enum_value ERROR_TYPE_SHADER [ErrorType] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
| [`enum_value ERROR_TYPE_WARNING [ErrorType] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
| [`method _log_error(String function, String file, int line, String code, String rationale, bool editor_notify, int error_type, ScriptBacktrace[] script_backtraces) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
| [`method _log_message(String message, bool error) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Logger.xml) | — | Blocked | Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). |
