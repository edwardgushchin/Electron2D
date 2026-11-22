using Android.App;
using Android.Content.PM;
using Electron2D;

namespace PermissionsAndroid;

internal sealed class PublicPermissionWindow(string mode, Func<Activity> activity) : Window
{
    private const string Audio = Android.Manifest.Permission.RecordAudio;
    private const string Camera = Android.Manifest.Permission.Camera;
    private const string Coarse = Android.Manifest.Permission.AccessCoarseLocation;
    private const string Fine = Android.Manifest.Permission.AccessFineLocation;
    private const string Background = "android.permission.ACCESS_BACKGROUND_LOCATION";
    private const string Sensors = "android.permission.BODY_SENSORS";
    private const string BackgroundSensors = "android.permission.BODY_SENSORS_BACKGROUND";
    private readonly List<(string Name, bool Granted)> _results = [];
    private bool _started;
    private bool _retried;
    private int _settledFrames;
    private int _owner;
    private bool IsLocationCase => mode.StartsWith("public-location-", StringComparison.Ordinal);
    private bool IsSensorCase => mode.StartsWith("public-sensors-", StringComparison.Ordinal);

    protected override void OnProcess(double delta)
    {
        base.OnProcess(delta);
        if (!_started)
        {
            if (!DisplayServer.WindowIsFocused())
            {
                return;
            }

            _started = true;
            _owner = Environment.CurrentManagedThreadId;
            Tree!.OnRequestPermissionsResult += Record;
            CheckList();
            Require(!OS.RequestPermission(""), "empty name");
            Require(!OS.RequestPermission("RECORD_AUDIO"), "short name");
            Require(!OS.RequestPermission(Audio + " "), "trailing whitespace");
            Require(!OS.RequestPermission(Audio + "\0"), "embedded null");
            Require(!OS.RequestPermission(Android.Manifest.Permission.ReadContacts), "undeclared permission");

            if (IsLocationCase)
            {
                string[] expectedGrants = mode switch
                {
                    "public-location-background" => [Coarse, Fine],
                    "public-location-upgrade" => [Coarse],
                    _ => []
                };
                Require(OS.GetGrantedPermissions().Order(StringComparer.Ordinal).SequenceEqual(expectedGrants),
                    "expected location grants before requesting");
                if (mode == "public-location-overlap-deny")
                {
                    Require(!OS.RequestPermission(Coarse), "single approximate request is pending");
                }
                Require(!OS.RequestPermissions(), "location request is pending");
                Require(!OS.RequestPermission(Background), "single background request overlaps the batch");
                return;
            }

            if (IsSensorCase)
            {
                string[] expectedGrants = mode == "public-sensors-background" ? [Sensors] : [];
                Require(OS.GetGrantedPermissions().SequenceEqual(expectedGrants), "expected sensor grants before requesting");
                Require(!OS.RequestPermissions(), "sensor request is pending");
                Require(!OS.RequestPermission(BackgroundSensors), "single background sensor request overlaps the batch");
                return;
            }

            if (mode is "public-bulk-granted" or "public-bulk-empty")
            {
                var context = Application.Context;
                const string restricted = Android.Manifest.Permission.WriteSecureSettings;
                using var package = context.PackageManager!.GetPackageInfo(context.PackageName!, PackageInfoFlags.Permissions);
                Require(package?.RequestedPermissions?.Contains(restricted, StringComparer.Ordinal) == true &&
                    context.CheckSelfPermission(restricted) == Permission.Denied, "declared non-dangerous permission is denied");
                Require(OS.RequestPermissions(), "no dangerous permission is missing");
                return;
            }

            if (mode == "public-bulk-finish")
            {
                var owner = activity();
                owner.RunOnUiThread(() =>
                {
                    Require(!OS.RequestPermissions(), "batch queued before Activity finishes");
                    Require(!OS.RequestPermission(Audio), "single overlaps the queued batch");
                    owner.Finish();
                });
                return;
            }

            if (mode == "public-single-bulk")
            {
                Require(!OS.RequestPermission(Audio), "single request is pending");
            }
            if (mode is "public-single-bulk" or "public-bulk-single" or "public-bulk-partial")
            {
                Require(!OS.RequestPermissions(), "batch request is pending");
                Require(!OS.RequestPermission(Camera), "camera overlaps the batch");
                Require(!OS.RequestPermissions(), "repeated batch shares the pending results");
                return;
            }

            if (mode == "public-finish")
            {
                var owner = activity();
                owner.RunOnUiThread(() =>
                {
                    OS.RequestPermission(Audio);
                    OS.RequestPermission(Camera);
                    owner.Finish();
                });
                return;
            }

            Require(!OS.RequestPermission(Audio), "first request is pending");
            Require(!OS.RequestPermission(Audio), "duplicate request is pending");
            if (mode == "public-order")
            {
                Require(!OS.RequestPermission(Camera), "second permission is pending");
            }
        }

        if (mode == "public-retry" && _results.Count == 1 && !_retried)
        {
            _retried = true;
            Require(_results[0] == (Audio, false), "first attempt denied");
            CheckList();
            Require(!OS.RequestPermission(Audio), "explicit retry is pending");
        }

        var expectedCount = mode switch
        {
            "public-bulk-empty" or "public-bulk-granted" => 0,
            "public-bulk-partial" or "public-location-background" or "public-sensors-background" => 1,
            "public-location-allow" or "public-location-deny" or "public-location-overlap-deny" => 3,
            _ => 2
        };
        if (_results.Count >= expectedCount && ++_settledFrames == 60)
        {
            if (mode != "public-bulk-empty" && !IsLocationCase && !IsSensorCase)
            {
                Require(OS.RequestPermission(Audio), "already granted returns true");
            }
            CheckList();
        }

        if (_settledFrames == 120)
        {
            Tree!.Quit();
        }
    }

    private void Record(MainLoop sender, string name, bool granted)
    {
        Require(ReferenceEquals(sender, Tree), "result delivered to the correct loop");
        Require(Environment.CurrentManagedThreadId == _owner, "result delivered on the loop owner");
        _results.Add((name, granted));
        CheckList();
    }

    internal string Verify()
    {
        (string Name, bool Granted)[] expected = mode switch
        {
            "public-order" or "public-single-bulk" or "public-bulk-single" => [(Audio, true), (Camera, false)],
            "public-bulk-partial" => [(Camera, false)],
            "public-location-allow" => [(Coarse, true), (Fine, true), (Background, false)],
            "public-location-deny" => [(Coarse, false), (Fine, false), (Background, false)],
            "public-location-background" => [(Background, false)],
            "public-location-upgrade" => [(Fine, true), (Background, false)],
            "public-location-overlap-deny" => [(Coarse, false), (Fine, true), (Background, false)],
            "public-sensors-allow" => [(Sensors, true), (BackgroundSensors, false)],
            "public-sensors-deny" => [(Sensors, false), (BackgroundSensors, false)],
            "public-sensors-background" => [(BackgroundSensors, false)],
            "public-retry" => [(Audio, false), (Audio, true)],
            "public-finish" or "public-bulk-finish" or "public-bulk-granted" or "public-bulk-empty" => [],
            _ => throw new ArgumentException($"Unknown permission scenario {mode}.")
        };
        var matches = mode == "public-bulk-single"
            ? _results.Count == expected.Length && expected.All(_results.Contains)
            : _results.SequenceEqual(expected);
        if (mode is "public-location-allow" or "public-location-deny")
        {
            // Android can order the two foreground names either way. The background decision must follow both.
            matches = _results.Count == expected.Length && _results[^1] == expected[^1] &&
                expected.All(_results.Contains);
        }
        Require(_started && matches, "exact callback names, decisions and count in the expected order");
        CheckList();
        var complete = mode is "public-bulk-empty" or "public-bulk-granted";
        Require(OS.RequestPermission(Camera) == (mode == "public-bulk-granted"), "single result after Activity destruction");
        Require(OS.RequestPermissions() == complete, "batch result after Activity destruction");
        return string.Join(",", _results.Select(result => $"{result.Name}:{result.Granted}"));
    }

    private void CheckList()
    {
        var context = Application.Context;
        string[] names = mode switch
        {
            _ when IsLocationCase => [Coarse, Fine, Background],
            _ when IsSensorCase => [Sensors, BackgroundSensors],
            _ => [Audio, Camera]
        };
        var expected = names.Where(name =>
            context.PackageManager!.CheckPermission(name, context.PackageName!) == Permission.Granted);
        Require(OS.GetGrantedPermissions().Order(StringComparer.Ordinal).SequenceEqual(expected.Order(StringComparer.Ordinal)),
            "exact dangerous grant snapshot matches Android PackageManager");
    }

    private static void Require(bool condition, string check)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Permission check failed: {check}.");
        }
    }
}
