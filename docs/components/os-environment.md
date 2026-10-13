# Process environment and operating-system directories

Last updated: 2026-10-13

## Executable scope

The retained static [OS](../classes/OS.md) service provides process-local environment lookup, mutation and removal, global configuration/data/cache/temp paths, project user data and eight typed user-directory roles. These are cold allocating operations, not frame callbacks. Queries do not create directories or establish write permission.

GetEnvironment returns an empty string for an absent variable; HasEnvironment distinguishes absence from an existing empty value. Names are platform case-sensitive except on Windows. Empty names, equals/null characters in names, null values, malformed Unicode and null characters in values are rejected before mutation. SetEnvironment accepts literal strings, including empty values. UnsetEnvironment also succeeds for an absent name. No changes persist in the user or machine environment after process exit.

Managed environment changes do not update SDL's environment automatically on Unix. OS mutations update both paths and restore the managed value if the native update fails. Native child creation and these edits use one retained gate. Consequently OS.CreateProcess, CreateInstance, Execute and ExecuteWithPipe inherit subsequent edits; children already running retain their own environment. External BCL/native edits are outside this synchronized service and applications must coordinate process-wide changes. SDL's environment is library-owned and survives normal engine sessions; no private public singleton is exposed.

## Directories

| Query | Current desktop policy |
| --- | --- |
| GetConfigDir | Linux absolute XDG_CONFIG_HOME, otherwise HOME/.config; Windows roaming ApplicationData; macOS Library/Application Support |
| GetDataDir | Linux absolute XDG_DATA_HOME, otherwise HOME/.local/share; Windows/macOS configuration directory |
| GetCacheDir | Linux absolute XDG_CACHE_HOME, otherwise HOME/.cache; Windows LocalApplicationData; macOS Library/Caches |
| GetTempDir | Linux global /tmp; other hosts use .NET temporary-directory policy |
| GetUserDataDir | Exact current ProjectSettings.UserDataRoot, used by user:// |
| GetSystemDir | SDL native Desktop, Pictures, Documents, Downloads, Videos and Music directories |

Relative/empty XDG values are ignored. Missing HOME uses the platform user-profile lookup; an unavailable home fails. SystemDir retains all eight values: Linux/Windows DCIM uses Pictures and Ringtones uses Music. macOS has no camera/ringtone roles and rejects those two explicitly. sharedStorage defaults to true; desktop hosts have no separate storage selector. Mobile/TV shared/app directory adapters and browser configuration/data/cache/user-managed directory services are unavailable and fail explicitly. GetTempDir returns Linux /tmp and otherwise uses the current host's temporary-directory policy without promising persistence. GetUserDataDir remains backed by the existing directory-based project resolver; it does not add browser persistent storage or permissions.

Paths may be absent, redirected, read-only or sandbox-restricted. The application owns directory creation and file-access failures. GetUserDataDir reflects ConfigurePaths immediately, rather than guessing from an application name. No project path schema or existing default user root changes.

## Verification

OSEnvironmentTests covers missing/empty/literal/replaced values, invalid input without mutation, removal, fresh managed and native child inheritance, all eight directory roles and invalid enum values, XDG absolute/relative/empty overrides with restoration, query non-creation and a public FileAccess save through user://. The tests create only an isolated temporary directory. Existing OSProcessTests covers capture, pipe ownership and independent-child lifetime. Native executable evidence is the current Linux desktop; Windows/macOS/mobile/TV/browser execution remains unverified under ADR 0021.

## Decisions and sources

[ADR 0004](../decisions/product.md#adr-0004) owns the typed public boundary, [ADR 0019](../decisions/core-data-io.md#adr-0019) owns virtual paths, [ADR 0051](../decisions/enum-identities.md#adr-0051) owns enum identities, and [ADR 0095](../decisions/singleton-services.md#adr-0095) owns retained static access. Native policy follows [SDL_GetUserFolder](https://wiki.libsdl.org/SDL3/SDL_GetUserFolder); the managed/native environment distinction is documented by [Environment.SetEnvironmentVariable](https://learn.microsoft.com/en-us/dotnet/api/system.environment.setenvironmentvariable?view=net-10.0).
