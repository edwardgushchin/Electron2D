# Windows process adoption failure

Run from the repository root with Python UTF8 enabled:

```powershell
$env:PYTHONUTF8 = '1'
python -B tests/ProcessAdoption.Tests/run.py --previous 9c46536c0308208ac411f1f8af932dcb7a9b8114
python -B tests/ProcessAdoption.Tests/run.py
python -B tests/ProcessAdoption.Tests/run.py --aot
```

The preceding-version control must exit 1 and report both raw handles open and the child alive after the exception. The fixed profiles must exit 0. The fixture cleans up the intentionally leaked control child and handles. Native handle counts measure warmed capture infrastructure; each scenario independently checks the original raw process/thread handles and a separate native observer handle retained before the exception, then verifies successful independent retry.

The harness copies the actual launcher into ignored build output and injects calls at four post-create boundaries: raw handles returned, process handle adopted, local pipe writer closed, and managed owner constructed. A preallocated OutOfMemoryException interrupts the call. The ordinary public OS.CreateProcess/Execute paths run with hidden/new consoles and capture disabled/enabled; an independent Win32 observer checks retirement and handle closure. No production fault flag, public API, allocator replacement or copy of the cleanup algorithm is added. The test-only MSBuild overlay includes Probe.cs solely in the instrumented engine; the fixture has the existing root-test friend identity. Normal engine and candidate-package builds must follow these profiles, without the overlay.

Strict NativeAOT uses runtime/ILCompiler 10.0.14, full trimming and linker/ILC warnings as errors. Artifacts and logs remain under `bin/os-process-adoption`. Native platform package, other Windows architectures/sandboxes and full platform qualification remain separate.
