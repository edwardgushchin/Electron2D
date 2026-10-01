#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
using System;

namespace Electron2D.NVorbisBindings.Contracts
{
    /// <summary>
    /// Contains data for parameter change events.
    /// </summary>
    internal class ParameterChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="ParameterChangeEventArgs"/>.
        /// </summary>
        /// <param name="channels">The new number of channels.</param>
        /// <param name="sampleRate">The new sample rate.</param>
        public ParameterChangeEventArgs(int channels, int sampleRate)
        {
            Channels = channels;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Get the new number of channels in the stream.
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// Gets the new sample rate of the stream.
        /// </summary>
        public int SampleRate { get; }
    }
}
