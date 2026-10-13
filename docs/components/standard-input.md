# Process standard handles and input

Last updated: 2026-10-13

## Executable scope

The retained static [OS](../classes/OS.md) service reports the actual native device behind standard input/output/error and reads owned byte buffers or bounded UTF-8 lines. This supplies runtime primitives for pipe-driven applications and tools. It does not establish complete editor/batch-host acceptance under [ADR 0090](../decisions/agent-native.md#adr-0090).

OS.StdHandleType retains Invalid=0, Console=1, File=2, Pipe=3 and Unknown=4. GetSTDINType, GetSTDOUTType and GetSTDERRType inspect current handles without consuming or emitting data. Linux uses isatty and the fixed statx descriptor ABI: regular files are File, FIFO/anonymous pipes are Pipe, other valid devices are Unknown, and closed descriptors are Invalid. A missing libc/kernel statx adapter fails explicitly. Windows uses GetStdHandle/GetConsoleMode/GetFileType. macOS/mobile/TV/browser standard-handle adapters are currently unavailable and throw PlatformNotSupportedException; Windows native execution remains unverified. No device is inferred solely from a managed redirected flag.

## Reading contract

| API | Behavior |
| --- | --- |
| ReadBufferFromSTDIN(int bufferSize = 1024) | Reads exactly the requested count from pipes/files, or a shorter owned prefix at EOF; terminal reads return an available line or prefix. Preserves all raw bytes. |
| ReadStringFromSTDIN(int bufferSize = 1024) | Reads through LF, EOF or bufferSize minus one bytes, without consuming the next line. Decodes strict UTF-8, normalizes CRLF to LF and removes trailing LF. |

Binary capacity must be positive; text capacity must be at least two. Both reject values beyond Array.MaxLength before reading or allocating. The text capacity includes the terminating boundary from the line-input contract. Text is an ordinary C# string and retains embedded NUL; malformed or split UTF-8 sequences throw DecoderFallbackException. Consumed bytes are not restored after decoding/I/O failure. Raw buffers plus an application-owned decoder are the correct path for arbitrary chunk framing.

EOF yields an empty array/string. Empty text can also represent an empty line; the text API does not expose a separate EOF flag. A closed/absent input handle throws IOException and is distinguished from EOF by GetSTDINType. Reads borrow the current process input handle; disposal of a temporary reader never closes it. Raw/text reads share a retained gate and do not read ahead into each other's data. External Console/native readers and descriptor replacement remain application-owned coordination. Console redirection/text wrappers are not used as an input cache. Windows console hosts must provide UTF-8 bytes when using the text method.

These operations block and allocate. Pipe producers must eventually supply the requested count or close their writer; text producers must send LF, reach the byte limit or close. Applications that need responsive rendering run reads on a worker and own cancellation/EOF/shutdown policy. No nonblocking or cancellation option is added to this API family.

## Verification

[OSStandardInputTests](../../tests/Electron2D.Tests/OSStandardInputTests.cs) launches actual child applications and verifies native pipe/file/terminal/null/closed-handle types, exact binary data, delayed fragmented pipe filling, empty/partial EOF, bounded text, CRLF, raw/text alternation, malformed/split UTF-8 consumption, validation before consumption and an existing OS.ExecuteWithPipe request/response. The Linux [fixture](../../tests/Electron2D.Tests/Fixtures/StandardInputHost.py) supplies real file and pseudoterminal descriptors; it does not alter the user's terminal. All temporary files/processes are owned by the tests. Other platforms, physical interactive-console acceptance and throughput remain separate gates.

## Decisions and sources

[ADR 0004](../decisions/product.md#adr-0004) owns typed errors, [ADR 0020](../decisions/core-data-io.md#adr-0020) owns explicit stream/UTF-8 behavior, [ADR 0045](../decisions/product.md#adr-0045) owns uppercase STDIN/STDOUT/STDERR member spelling, [ADR 0051](../decisions/enum-identities.md#adr-0051) owns enum identity and [ADR 0095](../decisions/singleton-services.md#adr-0095) owns retained static access. Native classification follows [statx](https://man7.org/linux/man-pages/man2/statx.2.html) and [GetFileType](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-getfiletype).
