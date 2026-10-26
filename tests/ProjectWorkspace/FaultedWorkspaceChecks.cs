using Electron2D;
using Electron2D.Editor;
using IOPath = System.IO.Path;

internal static class FaultedWorkspaceChecks
{
    private static PublicationFault CreateFault() => new();
    internal static int Run(string output)
    {
        ResourceFileTypes.RegisterNode("session.workspace.PublicationFault", CreateFault);
        var project = SessionProject.Create(IOPath.Combine(output, "Game"), AppContext.BaseDirectory);
        using (var root = new PublicationFault { Name = "Scene" }) ResourceSaver.SaveScene(root, IOPath.Combine(project.Root, project.SceneFile), SaverFlags.BundleResources);
        var owner = project.OpenSession(); using var human = owner.Connect("fault fixture");
        using var regular = new FontFile(); regular.LoadDynamicFont(IOPath.Combine(AppContext.BaseDirectory, "Assets/IBMPlexSans-Regular.ttf"));
        using var mono = new SystemFont { FontNames = ["monospace"] };
        var workspace = new ProjectWorkspace(project, owner, AppContext.BaseDirectory, regular, mono);
        var state = IOPath.Combine(project.Root, ".electron2d/session/state.json"); var backup = state + ".fixture";
        PublicationFault.AfterPublication = () => { File.Move(state, backup); Directory.CreateDirectory(state); };
        try
        {
            var result = human.Save(SessionDocumentKind.Scene, human.InspectScene().Revision);
            if (!result.FileWritten || !owner.IsFaulted || !result.Diagnostics.Any(d => d.Code == "reopen-required-after-write")) throw new InvalidOperationException("Fixture did not reach uncertain post-publication recovery.");
        }
        finally { PublicationFault.AfterPublication = null; if (Directory.Exists(state)) Directory.Delete(state); if (File.Exists(backup)) File.Move(backup, state); }
        workspace.PrepareClose(); workspace.Dispose(); workspace.RetireJobsAsync().GetAwaiter().GetResult(); owner.Dispose();
        using var recovered = project.OpenSession();
        if (recovered.IsFaulted) throw new InvalidOperationException("Fresh owner retained the retired fault state.");
        Console.WriteLine("PASS 4 faulted publication/status/workspace-close/owner-reopen contracts."); return 0;
    }
    private sealed class PublicationFault : Entity
    {
        internal static Action? AfterPublication;
        protected override Func<Node> CreateSceneInstanceFactory() => CreateFault;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationEditorPostSave) AfterPublication?.Invoke(); }
    }
}
