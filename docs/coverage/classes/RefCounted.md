# RefCounted API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/RefCounted.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RefCounted.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RefCounted`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RefCounted.xml) | — | Excluded | Public manual reference counting is excluded by ADRs 0003 and 0014. RefCounted ancestry maps to ElectronObject managed lifetime and IDisposable; descendant APIs are audited on their own pages. |
| [`method get_reference_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RefCounted.xml) | — | Excluded | Public manual reference counting is excluded by ADRs 0003 and 0014. RefCounted ancestry maps to ElectronObject managed lifetime and IDisposable; descendant APIs are audited on their own pages. |
| [`method init_ref() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RefCounted.xml) | — | Excluded | Public manual reference counting is excluded by ADRs 0003 and 0014. RefCounted ancestry maps to ElectronObject managed lifetime and IDisposable; descendant APIs are audited on their own pages. |
| [`method reference() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RefCounted.xml) | — | Excluded | Public manual reference counting is excluded by ADRs 0003 and 0014. RefCounted ancestry maps to ElectronObject managed lifetime and IDisposable; descendant APIs are audited on their own pages. |
| [`method unreference() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RefCounted.xml) | — | Excluded | Public manual reference counting is excluded by ADRs 0003 and 0014. RefCounted ancestry maps to ElectronObject managed lifetime and IDisposable; descendant APIs are audited on their own pages. |
