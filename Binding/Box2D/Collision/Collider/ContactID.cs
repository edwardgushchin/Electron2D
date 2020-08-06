using System.Runtime.InteropServices;

namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// Contact ids to facilitate warm starting.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct ContactId
    {
        [FieldOffset(0)]
        public ContactFeature ContactFeature;

        /// <summary>
        /// Used to quickly compare contact ids.
        /// </summary>
        [FieldOffset(0)]
        public uint Key;
    }
}