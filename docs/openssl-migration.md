# OpenSSL 3.6.5 to 4.1.0 migration

At the audited Electron2D base `0a1d6f3d8481fb909d510df9d6e4a9c7eb57ce2e`, the private source pin and runtime guards already use 4.1.0. The migration entered the tree in `b56d0412428836dc2b67b090ce68465d6eaa350c`. Its parent `90a5f53d23a914dc66f172eb677a4fa14fd66164` is the 3.6.5 source baseline. This record governs qualification and promotion under [ADR 0094](decisions/networking.md#adr-0094).

## Upstream sources

On 2026-11-23 the SHA-256 of each downloaded release tarball matched its upstream sidecar and the corresponding Electron2D source pin. The commit column gives the peeled tag target, distinct from the annotated tag object.

| Release tarball and checksum | Release commit | SHA-256 of tar.gz bytes |
| --- | --- | --- |
| [3.6.5 archive](https://github.com/openssl/openssl/releases/download/openssl-3.6.5/openssl-3.6.5.tar.gz) · [SHA-256](https://github.com/openssl/openssl/releases/download/openssl-3.6.5/openssl-3.6.5.tar.gz.sha256) | `c8bd5a57108599ac650bbae77fcabe3109dab2e8` | `a2157c2830efdec3788939b00c9b0638306d3f0bbb76dc4832ee503bb397df98` |
| [4.1.0 archive](https://github.com/openssl/openssl/releases/download/openssl-4.1.0/openssl-4.1.0.tar.gz) · [SHA-256](https://github.com/openssl/openssl/releases/download/openssl-4.1.0/openssl-4.1.0.tar.gz.sha256) | `2f4e3a5fec05394068cfa7379890ab552483a8a6` | `50c17baf54cb008f6ce42d50deb4b28050a1aad711636cabd4b241c7e90cc2fd` |

`git ls-remote --tags https://github.com/openssl/openssl.git` returned tag objects `f93be98fe7c796e92fc999dd1a21a5212d4e4f8e` and `ad2c6be0cf142e3d8ce78fcdb25070674d28130f`, respectively. Both detached tarball signatures passed GnuPG verification with signing subkey `C46ED3F2CBEFDA1FDAADA44264ED7B1DCCE71CB2` and primary key `B146647E45A7B33947AB226B2A2C87D161692D40` from the [OpenSSL release signing certificate](https://www.openssl-library.org/source/).

## ABI, trust and local adaptations

- The archives' `VERSION.dat` changes `SHLIB_VERSION` from 3 to 4. `util/libssl.num` and `util/libcrypto.num` retain `TLS_method`, `DTLS_method`, `SSL_CTX_set_cert_verify_callback`, `OpenSSL_version_num`, `BIO_s_dgram_pair` and `BIO_new_bio_dgram_pair`, but reset their symbol versions to `4_0_0` and change their export ordinals. For example, `BIO_s_dgram_pair` changes from 5605/`3_2_0` to 2947/`4_0_0`. Re-link native consumers and regenerate Windows import libraries from the selected build. Renaming a major-3 library cannot satisfy the new ABI. Check the full required export set and Cdecl argument widths on the selected target, including x86 and LLP64. See the [upstream migration guide](https://docs.openssl.org/4.0/man7/ossl-guide-migration/).
- Keep the producer's target configuration, checksum before extraction, generated `apps/include` path, and target-specific dependency relocation. WebRTC uses matching generated headers, CMake's exact 4.1.0 requirement, and the bridge provider-version export. Keep the package allowlists, manifests, licence and build/publish cleanup synchronized with the resolver. The separate Linux allocation-ledger profile keeps its patch preimage and native ownership checks.
- Linux's .NET PAL must select the same private pair as engine TLS and WebRTC before the first BCL crypto operation. The Linux source profile sets `DOTNET_OPENSSL_VERSION_OVERRIDE=4` and `LD_LIBRARY_PATH` to the selected RID's native directory before startup. Check both first-use orders. An already initialized major-3 PAL or stale WebRTC bridge must fail before successful transport creation.
- Preserve OS-chain trust, followed by OpenSSL certificate policy and DNS/IP checks. Cover a trusted chain, an untrusted root, explicit custom CA and unsafe mode, DNS SAN precedence, CN-only fallback and IP SAN, including mismatch cases. Linux retains the OS CA source via .NET and the producer sets `--openssldir=/etc/ssl`. The existing name callback restores CN fallback when there is no DNS SAN.
- Keep TLS 1.2 as the minimum and exercise TLS 1.3 negotiation. Keep Electron2D packet DTLS endpoints on 1.2, including cookies, retransmission under loss, and independent peers in both roles. 4.1.0 adds DTLS 1.3. Verify the packet DTLS cap and test WebRTC's separate libdatachannel negotiation with the independent browser fixture. See the [upstream DTLS 1.3 guide](https://docs.openssl.org/4.1/man7/ossl-guide-dtlsv13/).

## Local prototype results

The source checks used Electron2D revision `0a1d6f3d8481fb909d510df9d6e4a9c7eb57ce2e` on WSL Arch x86_64. The clean and same-directory repeat builds passed with GCC 16.1.1, Perl 5.42.2 and patchelf 0.18.0. On the clean pair, `readelf` showed both major-4 SONAMEs, SSL's private crypto dependency and `$ORIGIN` on both files. `nm -D` confirmed 33 direct SSL imports and 31 direct crypto imports from `TLSNative`, including the six selected exports above at `OPENSSL_4.0.0`. The producer rejected a corrupted cached archive before extraction and accepted the restored archive. The separate system OpenSSL 3.6.3 probe found SSL's major-3 SONAME and crypto dependency, the `OPENSSL_3.2.0` datagram BIO exports and `/etc/ssl` as the configured directory.

The Arch .NET 10.0.8 PAL has direct ELF `DT_NEEDED` entries for `libssl.so.3` and `libcrypto.so.3`, confirmed with `readelf -d` and saved in the T-0159 reproduction report. It rejected the private source configuration in all three transport probes even with the override set. The separately downloaded Microsoft Linux x64 runtime 10.0.8, verified against its release archive SHA-512, resolved both major-4 libraries dynamically. With that runtime and the locally built pair:

- The TLS fixture passed OS-anchor/unknown-root checks, custom trust and name checks, TLS 1.2/1.3 SslStream interop and independent OpenSSL TLS 1.3 with records in both directions.
- The DTLS fixture passed normal and packet-loss handshakes, cookie/retransmission and authenticated packets, plus independent Bouncy Castle and OpenSSL DTLS 1.2.
- A public WebRTC loopback probe opened a data channel and transferred exact bytes with libdatachannel **0.24.6**. It used the historical native engine bridge from Electron2D revision `c759c728e7b7efd3b8478969996490ccccde170e` with the ELF SHA-256 verified against that build's receipt. This result applies to that historical bridge. The target at `a819dc5aeb9f9fac4aeef42812a2d19796e27aff` pins libdatachannel **0.24.7** and changes `dtlstransport.cpp`. Qualification of that bridge requires a fresh build, loopback and browser interoperability run.
- Process maps in each positive run contained exactly one selected SSL/crypto pair at major 4. The stock-PAL negative test, with both override variables unset, rejected public TLS startup and mapped only the system major-3 pair with both .NET distributions.

The source runs used a focused CLR harness over the repository TLS, DTLS and version-rejection fixtures plus the public WebRTC loopback. The release package matrix, NativeAOT, installed-package startup, promotion and rollback rehearsal were outside these runs.

## Qualification matrix

The [RID registry](../tools/ci/rids.json), [package checker](../tools/native/package.py), [native workflow](../.github/workflows/native.yml) and [runtime workflow](../.github/workflows/rid.yml) define the current build and package rows. The previous 18-target producer run at `8a614afc013bb61f7c07e537128c3c8fb23db7f0`, Windows runtime checks at `975c9693cb66cb11a1359c684da56483354c641d` and public `0.1.0-alpha` run at `17be5f4c3671420715cf70e7bab1e965707673e9` all predate the migration and pin 3.6.5. They provide baseline evidence for the [delivery path](native-packaging.md). A producer pass establishes binary construction and package audit for that run, with runtime acceptance tracked separately.

| Profile | Required producer, package and runtime observations | Local scope |
| --- | --- | --- |
| Linux system x64/ARM64 | On the former baseline, check stock `libssl.so.3` → `libcrypto.so.3`, distro CA and TLS/DTLS. On the current tree, exercise fresh stock-PAL rejection and check that major 4 has not loaded after rejection. Keep this process separate from the private profile. | WSL x64 system OpenSSL 3.6.3: ELF identity and BIO exports checked. Former engine and ARM64 runtime require the historical build and matching ARM64 runner. |
| Linux private `linux-x64`, `linux-arm64` | Source and `Electron2D.Linux`: both major-4 SONAMEs, SSL NEEDED crypto 4, relative `$ORIGIN`, no major 3 in the positive process maps. Run TLS, DTLS and WebRTC in separate fresh processes under CLR and the documented full-trim NativeAOT consumers. | WSL x64 producer and CLR prototype above. Full native engine rebuild and package-only qualification remain open. ARM64 requires a matching runner. |
| Windows `win-x86`, `win-x64`, `win-arm64` | `Electron2D.Windows`: PE machine, export identity, `libssl-4.dll` imports `libcrypto-4.dll`, exact package manifests and stale-output cleanup. Exercise OS trust, TLS, DTLS and WebRTC across the three RIDs, including x86 calling conventions. | This session has no selected MSVC/Perl producer environment or Windows ARM64 runtime host. Use the three Windows CI profiles. |
| macOS `osx-x64`, `osx-arm64` | `Electron2D.MacOS`: private `.4.dylib` IDs under `@rpath`, crypto dependency resolved with `@loader_path`, verify ad-hoc signing after relocation. Run TLS, DTLS and WebRTC with Keychain trust checks. | Windows/WSL host has no macOS or Xcode. |
| Android `android-arm`, `android-arm64`, `android-x86`, `android-x64` | `Electron2D.Android`: private unversioned SONAMEs and SSL NEEDED `libElectron2DCrypto.so`, ELF machine/class and 16 KB alignment. Audit APK ABI and notices; run target TLS trust/records with the Android fixture. | Installed local NDK is 23.2.8568313 (CI needs 28.2.13676358); ADB listed no device or emulator. |
| iOS and tvOS device ARM64 and simulator x64/ARM64 | `Electron2D.iOS` / `Electron2D.tvOS`: link the private static archives through `NativeReference` and `__Internal`; audit object CPU and device/simulator platform. Exercise TLS in all four simulator profiles and record device bundle checks separately from signed-device execution. | Xcode, Apple SDKs, simulators and signing/device access are unavailable on this host. |
| `browser-wasm` (shared source pin) | `Electron2D.Web`: audit wasm32 archive provenance and link. Keep desktop browser-peer interoperability separate from browser-hosted engine transport support. | The local SDK has no wasm-tools workload. |

For each platform, build into an empty producer directory, then repeat in the same directory and compare the selected closure, imports, exports and notices. Run the package checker before packing. Repeat build/publish into a previously populated consumer output and confirm the selected tuple replaces stale assets. Audit the actual published layout, including a negative test with a mixed/stale major or bridge. Android and Apple target TLS fixtures cover a smaller surface. Record mobile DTLS and WebRTC as unqualified until there are equivalent end-to-end target fixtures.

The standalone Linux TLS producer uses the shared WSL build slot automatically. Run from the repository root on Linux x64:

```bash
(
    set -e
    output=$(mktemp -d)
    trap 'rm -rf -- "$output"' EXIT
    for pass in clean repeat; do
        python3 -B tools/native/build_tls.py linux-x64 "$output"
        for library in "$output/libssl.so.4" "$output/libcrypto.so.4"; do
            readelf -d "$library" | grep -E 'SONAME|NEEDED|RUNPATH'
            nm -D --defined-only "$library" > "$library.$pass.exports"
        done
    done
    for library in "$output/libssl.so.4" "$output/libcrypto.so.4"; do
        diff -u "$library.clean.exports" "$library.repeat.exports"
    done
)
```

## Reproduce the Linux x64 prototype

Use a clean checkout at `0a1d6f3d8481fb909d510df9d6e4a9c7eb57ce2e`. Put the T-0159 attachments `Probe.csproj`, `Program.cs`, `WebRTCSmoke.cs` and `libElectron2D.so` in one scratch directory. The ELF attachment is the original `bin/webrtc-memory/2026-11-05/native/libElectron2D.so` from the build recorded in `bin/webrtc-memory/2026-11-05/receipt.json` at revision `c759c728e7b7efd3b8478969996490ccccde170e`. Keep that ELF for reproducing the historical 0.24.6 result.

Build the managed runtime and probe from the checkout root using a .NET 10 SDK. In the Windows/WSL setup, use PowerShell for these commands and place the probe in a directory visible to WSL.

```powershell
$probe = 'C:/path/to/scratch/probe'
$repo = (Get-Location).Path
dotnet build Electron2D.csproj -c Release
if ($LASTEXITCODE) { exit $LASTEXITCODE }
dotnet build "$probe/Probe.csproj" -c Release "-p:RepoRoot=$repo"
if ($LASTEXITCODE) { exit $LASTEXITCODE }
```

Build the private pair from the checkout root in Bash on Linux x64 or WSL. Use a scratch directory on the Linux filesystem. The producer checks the upstream archive against its pinned SHA-256 before extraction.

```bash
tls_dir=$(mktemp -d)
python3 -B tools/native/build_tls.py linux-x64 "$tls_dir"
```

In the same Bash shell, set `probe` to the Linux path of the probe directory and stage the pair and bridge. Record the hashes of the resulting libraries since a fresh build can differ from the historical binaries.

```bash
set -e
probe=/path/to/scratch/probe
out="$probe/bin/Release/net10.0"
native_dir="$out/runtimes/linux-x64/native"
install -d "$native_dir"
install -m 755 "$tls_dir/libcrypto.so.4" "$tls_dir/libssl.so.4" "$native_dir/"
sha256sum "$native_dir/libcrypto.so.4" "$native_dir/libssl.so.4"
bridge_sha=d05c4fb6747531c097e147456fe2ca9de400477c661c98d986848199c8bf4fbc
printf '%s  %s\n' "$bridge_sha" "$probe/libElectron2D.so" | sha256sum --check -
install -m 755 "$probe/libElectron2D.so" "$native_dir/"

archive="$probe/dotnet-runtime-10.0.8-linux-x64.tar.gz"
url=https://builds.dotnet.microsoft.com/dotnet/Runtime/10.0.8/dotnet-runtime-10.0.8-linux-x64.tar.gz
curl --fail --location --silent --show-error "$url" --output "$archive"
runtime_sha=e5ed2ff2326ce833269eca8f35c2b0511c49f9585bd35f2b2c335484beec8348ff4ef5fb4b01335b78f00d1d17ad9cfd756909d847ec6eaef0df789db361a0ae
printf '%s  %s\n' "$runtime_sha" "$archive" | sha512sum --check -
install -d "$probe/microsoft"
tar -xzf "$archive" -C "$probe/microsoft"
dotnet_host="$probe/microsoft/dotnet"
"$dotnet_host" --list-runtimes

for suite in tls dtls webrtc; do
    env DOTNET_OPENSSL_VERSION_OVERRIDE=4 LD_LIBRARY_PATH="$native_dir" \
        "$dotnet_host" "$out/Electron2D.Tests.dll" "$suite"
done
env -u DOTNET_OPENSSL_VERSION_OVERRIDE -u LD_LIBRARY_PATH \
    "$dotnet_host" "$out/Electron2D.Tests.dll" runtime
```

The first three cases must each report one private pair at the paths under `$native_dir`. The final case expects the engine to refuse to start TLS with a system OpenSSL 3 PAL and show only the two system libraries in its process map. That control requires system `libssl.so.3` and `libcrypto.so.3`; the standalone OpenSSL oracles also require the system `openssl` executable.

## Qualify the candidate runtime

From a matching Linux host, use the [native build commands](native-packaging.md#full-native-rebuild) and the [TLS qualification setup](maintaining.md#unified-native-tls-qualification). For the Release runner use the candidate's `native_dir` and an absolute `dotnet_host` path to a runtime with dynamic OpenSSL selection, as in the prototype. Build the candidate bridge from the candidate sources.

```bash
runner=tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll
for suite in TLS DTLS WEBRTC; do
    env DOTNET_OPENSSL_VERSION_OVERRIDE=4 LD_LIBRARY_PATH="$native_dir" \
        "ELECTRON2D_TEST_${suite}=1" "$dotnet_host" "$runner" || exit
done
env -u DOTNET_OPENSSL_VERSION_OVERRIDE -u LD_LIBRARY_PATH \
    ELECTRON2D_TEST_TLS_VERSION=runtime "$dotnet_host" "$runner"
```

Also run the documented stale-bridge rejection, browser interoperability, and ENet, HTTP, WebSocket and crypto regressions. Capture the mapped library paths and file hashes for positive and rejection scenarios. The diagnostics workflow's stock-3 BIO probe alone cannot establish successful private-PAL startup.

## Promotion and rollback

1. Freeze the source and environment, including compiler versions, the matching producer/consumer source SHA, OpenSSL archive digests, generated-header profile, and existing local adaptations. Keep the previous qualified package set, package hashes and startup configuration available.
2. Run the complete [release qualification](releasing.md#build-once-and-qualify) on a candidate. Validate the producer receipts, package manifest and clean/contaminated consumer outputs for each RID. On Linux, qualify ordinary installed-package startup with the PAL policy before advancing from the source override. Retain separate receipts for build, runtime and package-only execution.
3. Promote the exact qualified artifact under the [release runbook](releasing.md#promote-the-qualified-files). Check package signatures, public package entries and clean-cache restore from the public source. Smoke-test the public installation with current startup configuration. A missing RID, unresolved export, foreign or duplicate provider, failed trust/name check or transport regression stops promotion.
4. Rehearse rollback against a staging feed. Restore the exact last qualified managed/platform package versions, clear stale build/publish outputs and restore their startup policy before a fresh process. For a pre-migration 3.6.5 release this includes reverting the private producer pin, bridge/loader identities, manifests, notices and major-4 PAL override together. Repeat package-only restore, ELF/PE/Mach-O closure, trust and TLS/DTLS/WebRTC checks with that release's supported RIDs. If a defective version has been published, follow the runbook's recovery process and qualify a new version if replacement is needed.
