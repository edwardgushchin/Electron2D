using Electron2D;

internal static partial class GraphChecks
{
    internal static void CommandMetadataCPU()
    {
        using var input = new VisualShaderNodeInput(); var changes = 0;
        input.InputTypeChanged += () => changes++;
        input.InputName = "region_rect"; Check(input.GetInputRealName() == "REGION_RECT" && changes == 1, "Command region has its four-component semantic role.");
        input.InputName = "specular_shininess"; Check(input.GetInputRealName() == "SPECULAR_SHININESS" && changes == 1, "Specular input shares the four-component role.");
        input.InputName = "uv"; Check(changes == 2, "Input notifications follow logical value roles.");
        input.InputName = "region_rect";
        using var copy = (VisualShaderNodeInput)input.Duplicate(); Check(copy.InputName == input.InputName, "Source copies retain the command input.");
        input.InputName = "unknown"; Check(input.GetInputRealName() == "", "Unknown names retain their established empty symbol.");
        Reject<NotSupportedException>(() => input.InputName = "attractor_force");
        Console.WriteLine("Command metadata CPU: input symbols, semantic notifications, source copying and unsupported force isolation passed.");
    }
}
