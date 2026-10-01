#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NVorbisBindings.Contracts
{
    interface IFloorData
    {
        bool ExecuteChannel { get; }
        bool ForceEnergy { get; set; }
        bool ForceNoEnergy { get; set; }
    }
}
