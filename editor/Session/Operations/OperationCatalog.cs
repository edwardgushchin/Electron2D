using System.Text.Json;
using System.Text.Json.Schema;

namespace Electron2D.Editor;

internal sealed record OperationDefinition(string Name, string Description, JsonElement InputSchema, bool ReadOnly);
internal static class OperationCatalog
{
    internal static readonly OperationDefinition[] Definitions =
    [
        Define<EmptyArguments>("context", "Read current scene/code document IDs, revisions, dirty/external state, shared selection and executable capabilities. Always inspect unsaved state before edits.", true),
        Define<SceneQuery>("scene_read", "Read a bounded page of stable scene object IDs, names, types, ownership, local transforms and revisions. Positions and transforms use engine units; max 128 nodes per page.", true),
        Define<CodeQuery>("code_read", "Read the shared unsaved C# buffer, never stale disk text. Start/length are UTF-16 offsets; chunks preserve surrogate boundaries. Max length 8192; use next for pagination.", true),
        Define<SceneChanges>("scene_apply", "Commit one attributed scene group with 1..64 typed position, transform, rename or reparent edits. Position preserves the current basis. Transform uses XX/XY/YX/YY/X/Y. ExpectedRevision is the object token, or whole scene token for reparent. Failure/conflict does not publish partial authoring.", false),
        Define<CodeReplacement>("code_edit", "Replace one UTF-16 source range in the shared unsaved buffer at the exact observed code document ID/revision. Text max 65536 characters. No implicit file save; stale edits conflict. Inspect the current buffer before computing offsets.", false),
        Define<EmptyArguments>("history", "Read up to 32 retained attributed operation groups and applied states. Selection does not create an authoring history group.", true),
        Define<HistoryChange>("undo", "Undo one retained applied group by stable operation ID, preserving later unrelated human work. Overlapping later changes conflict; never restore a whole old document blindly.", false),
        Define<HistoryChange>("redo", "Replay one safely undone retained group, with fresh monotonic revisions and overlap checks.", false),
        Define<SelectionChange>("selection", "Select a stable scene node or clear with the empty GUID, checking both scene and owner-local selection revisions. This changes context, not scene content or persistence.", false),
        Define<DocumentPublication>("save", "Explicitly publish one scene/code document at a fresh revision. This writes a project file; inspect FileWritten and diagnostics for post-write failure. Do not save unless the user authorized publication.", false),
        Define<DocumentRefresh>("refresh", "Reconcile an external scene/code file at a fresh revision. Reject refuses unsaved overlap; KeepLive preserves the buffer; UseDisk explicitly discards live changes and needs user authorization. No project file is written.", false),
        Define<JobSubmission>("job_start", "Build or run a fresh immutable snapshot of the exact unsaved scene/code plus qualified SDK. Returns queued job identity; process start is not success. Owner remains editable. Rendered mode needs explicit gpu/compatibility and capture steps; headless produces no images.", false),
        Define<EmptyArguments>("jobs", "Inspect shared UI/transport jobs, exact snapshot provenance, progress, terminal results and staleness. A result is current only while its captured document tokens still match.", true),
        Define<JobQuery>("job_status", "Poll one owned job without blocking. Completed means validated output; Failed/Cancelled are explicit outcomes. Artifacts and typed assertions retain the exercised snapshot identity.", true),
        Define<JobQuery>("job_cancel", "Request cancellation of an owned job and retirement of its inherited process group. Poll terminal status; committed authoring is preserved.", false),
        Define<JobDataQuery>("job_data", "Page exact snapshot inputs or terminal observations/assertions/diagnostics. Choose inputs, observations, assertions or diagnostics; max 128 entries. job_status returns only a compact preview.", true),
        Define<JobArtifactQuery>("job_artifact", "Read a validated PNG artifact by job identity and artifact index, never an arbitrary path. Returns image/png data and exact digest. Terminal image must fit four MiB; stale provenance remains explicit.", true)
    ];
    internal static string[] Names => Definitions.Select(d => d.Name).ToArray();
    private static OperationDefinition Define<T>(string name, string description, bool readOnly)
    {
        System.Text.Json.Nodes.JsonNode schema;
        try { schema = OperationWire.JSON.GetJsonSchemaAsNode(typeof(T), new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true }); }
        catch (Exception error) { throw new InvalidOperationException("Cannot export the operation schema " + typeof(T).Name + ": " + error.Message, error); }
        return new(name, description, JsonSerializer.SerializeToElement(schema), readOnly);
    }
}
