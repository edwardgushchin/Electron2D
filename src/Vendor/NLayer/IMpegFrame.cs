#nullable disable
#pragma warning disable CS1591
// Integration changes: private namespace, top-level visibility and preserved-source diagnostic policy.
namespace Electron2D.NLayerBindings
{
    /// <summary>
    /// Defines a standard way of representing a MPEG frame to the decoder
    /// </summary>
    internal interface IMpegFrame
    {
        /// <summary>
        /// Sample rate of this frame
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// The samplerate index (directly from the header)
        /// </summary>
        int SampleRateIndex { get; }

        /// <summary>
        /// Frame length in bytes
        /// </summary>
        int FrameLength { get; }

        /// <summary>
        /// Bit Rate
        /// </summary>
        int BitRate { get; }

        /// <summary>
        /// MPEG Version
        /// </summary>
        MpegVersion Version { get; }

        /// <summary>
        /// MPEG Layer
        /// </summary>
        MpegLayer Layer { get; }

        /// <summary>
        /// Channel Mode
        /// </summary>
        MpegChannelMode ChannelMode { get; }

        /// <summary>
        /// The number of samples in this frame
        /// </summary>
        int ChannelModeExtension { get; }

        /// <summary>
        /// The channel extension bits
        /// </summary>
        int SampleCount { get; }

        /// <summary>
        /// The bitrate index (directly from the header)
        /// </summary>
        int BitRateIndex { get; }

        /// <summary>
        /// Whether the Copyright bit is set
        /// </summary>
        bool IsCopyrighted { get; }

        /// <summary>
        /// Whether a CRC is present
        /// </summary>
        bool HasCrc { get; }

        /// <summary>
        /// Whether the CRC check failed (use error concealment strategy)
        /// </summary>
        bool IsCorrupted { get; }

        /// <summary>
        /// Resets the bit reader so frames can be reused
        /// </summary>
        void Reset();

        /// <summary>
        /// Provides sequential access to the bitstream in the frame (after the header and optional CRC)
        /// </summary>
        /// <param name="bitCount">The number of bits to read</param>
        /// <returns>-1 if the end of the frame has been encountered, otherwise the bits requested</returns>
        int ReadBits(int bitCount);
    }
}
