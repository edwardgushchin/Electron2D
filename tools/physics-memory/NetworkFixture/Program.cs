using Electron2D.Examples.PhysicsNetwork;

if (args is ["--process-memory-calibration"]) { Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ProcessMemory.Calibrate())); return 0; }
var check = args.Contains("--check");
if (!check)
{
    var index = Array.IndexOf(args, "--report");
    if (index < 0 || index + 1 == args.Length) throw new ArgumentException("The memory child requires its report path.");
    MemoryGate.Role = Path.GetFileNameWithoutExtension(args[index + 1]);
    MemoryGate.Pause("baseline");
}
var result = await NetworkApp.Run(args);
if (!check && result == 0) MemoryGate.Pause("released");
return result;
