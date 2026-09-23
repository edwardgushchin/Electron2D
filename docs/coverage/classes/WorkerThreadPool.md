# WorkerThreadPool API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/WorkerThreadPool.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class WorkerThreadPool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method add_group_task(Callable action, int elements, int tasks_needed = -1, bool high_priority = false, String description = "") -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method add_task(Callable action, bool high_priority = false, String description = "") -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method get_caller_group_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method get_caller_task_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method get_group_processed_element_count(int group_id) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method is_group_task_completed(int group_id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method is_task_completed(int task_id) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method wait_for_group_task_completion(int group_id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
| [`method wait_for_task_completion(int task_id) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorkerThreadPool.xml) | — | Blocked | Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). |
