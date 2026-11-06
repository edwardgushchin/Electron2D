# Status indicator consumer

The public-only consumer shares StatusIndicatorChecks with the root runner. From a Windows Developer PowerShell, build the independent Shell/UI Automation inspector outside the source tree:

```powershell
$receipts = 'C:/absolute/status-indicator-receipts'
New-Item -ItemType Directory -Force -Path $receipts
cl /nologo /EHsc /O2 /MT /std:c++17 /utf-8 tools/status-indicators/oracle.cpp /Fe:"$receipts/oracle.exe" /Fo:"$receipts/oracle.obj" user32.lib shell32.lib gdi32.lib ole32.lib oleaut32.lib oleacc.lib uiautomationcore.lib
dotnet run --project tests/StatusIndicator.Consumer/StatusIndicator.Consumer.csproj -c Release -- "$receipts/oracle.exe" $receipts
```

Use the matching full native bundle following [native packaging](../../docs/native-packaging.md), then repeat with a fresh candidate package and strict full-trim NativeAOT. All receipts/build objects belong outside the source tree. SDL_VIDEODRIVER=dummy plus the `unavailable` argument checks explicit failure before Windows native entry. The root profile uses ELECTRON2D_TEST_STATUS_INDICATOR=ORACLE and its first app argument is the receipts folder; `unavailable` selects the unsupported-host control.

The oracle locates only the target PID/title and exact registered icon ID. UI Automation invocation requires that icon's exact name and NotifyItemIcon identity. Tooltip observations use the exact notification-button identity; long text is re-registered before inspection because the Shell can cache its accessibility name after live modification. For hidden icons it may open the Windows overflow expander (English/Russian host labels), and closes it only if it opened it and it remains visible. It never moves the physical cursor, uses global input, restarts Explorer or closes another application. Injected press-channel messages and scoped TaskbarCreated notifications are explicitly separate from actual Shell activation and recovery after an Explorer restart.

The default profile verifies real bound popup opening and cancellation. ELECTRON2D_STATUS_MENU_ACTIVATE=1 enables a visible test window and requires the exact own foreground HWND before attempting MSAA selection; absence fails the profile. This does not grant focus permission or waive that prerequisite. Actual on-screen icon pixels, physical right-click selection and foreign platforms require separate receipts. See [behavior and current limitations](../../docs/components/status-indicators.md).
