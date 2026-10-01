#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Electron2D.NLayerBindings
{
    internal enum MpegVersion
    {
        Unknown = 0,
        Version1 = 10,
        Version2 = 20,
        Version25 = 25,
    }

    internal enum MpegLayer
    {
        Unknown = 0,
        LayerI = 1,
        LayerII = 2,
        LayerIII = 3,
    }

    internal enum MpegChannelMode
    {
        Stereo,
        JointStereo,
        DualChannel,
        Mono,
    }

    internal enum StereoMode
    {
        Both,
        LeftOnly,
        RightOnly,
        DownmixToMono,
    }
}
