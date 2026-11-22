using Android.App;
using Android.Content.PM;
using Electron2D;

namespace PermissionsAndroid;

internal sealed class PublicPermissionWindow(string mode, Func<Activity> activity) : Window
{
    private const string Audio = Android.Manifest.Permission.RecordAudio;
    private const string Camera = Android.Manifest.Permission.Camera;
    private readonly List<(string Name, bool Granted)> _results = [];
    private bool _started;
    private bool _retried;
    private int _settledFrames;
    private int _owner;

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

        if (_results.Count >= 2 && ++_settledFrames == 60)
        {
            Require(OS.RequestPermission(Audio), "already granted returns true");
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
            "public-order" => [(Audio, true), (Camera, false)],
            "public-retry" => [(Audio, false), (Audio, true)],
            "public-finish" => [],
            _ => throw new ArgumentException($"Unknown permission scenario {mode}.")
        };
        Require(_started && _results.SequenceEqual(expected), "exact callback names, decisions, order and count");
        CheckList();
        Require(!OS.RequestPermission(Camera), "denied request after Activity destruction");
        return string.Join(",", _results.Select(result => $"{result.Name}:{result.Granted}"));
    }

    private static void CheckList()
    {
        var context = Application.Context;
        // Check the manifest's two dangerous entries and its excluded, install-time VIBRATE entry.
        var expected = new[] { Audio, Camera }.Where(name =>
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
