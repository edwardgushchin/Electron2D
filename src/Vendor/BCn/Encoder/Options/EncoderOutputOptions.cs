#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using Electron2DBlockCodecs.Shared;

namespace Electron2DBlockCodecs.Encoder.Options
{
    /// <summary>
    /// The output options for the encoder.
    /// </summary>
	internal class EncoderOutputOptions
    {
        /// <summary>
        /// Whether to generate mipMaps. Default is true.
        /// </summary>
        public bool GenerateMipMaps { get; set; } = true;

        /// <summary>
        /// The maximum number of mipmap levels to generate. -1 or 0 is unbounded.
        /// Default is -1.
        /// </summary>
        public int MaxMipMapLevel { get; set; } = -1;

        /// <summary>
        /// The compression Format to use. Default is Bc1.
        /// </summary>
        public CompressionFormat Format { get; set; } = CompressionFormat.Bc1;

        /// <summary>
        /// The Quality of the compression. Use either fast or balanced for testing.
        /// Fast can be used for near real-time encoding for most algorithms.
        /// Use bestQuality when needed. Default is balanced.
        /// </summary>
        public CompressionQuality Quality { get; set; } = CompressionQuality.Balanced;

        /// <summary>
        /// The output file Format of the data. Either Ktx or Dds.
        /// Default is Ktx.
        /// </summary>
        public OutputFileFormat FileFormat { get; set; } = OutputFileFormat.Ktx;

        /// <summary>
        /// The DDS file Format doesn't seem to have a standard for indicating whether a BC1 texture
        /// includes 1bit of alpha. This option will write DDPF_ALPHAPIXELS flag to the header
        /// to indicate the presence of an alpha channel. Some programs read and write this flag,
        /// but some programs don't like it and get confused. Your mileage may vary.
        /// Default is false.
        /// </summary>
        public bool DdsBc1WriteAlphaFlag { get; set; } = false;

		/// <summary>
		/// When writing a dds file, always prefer using the Dxt10 header
		/// to write the compression format instead of DwFourCC.
		/// </summary>
		public bool DdsPreferDxt10Header { get; set; } = false;
    }
}
