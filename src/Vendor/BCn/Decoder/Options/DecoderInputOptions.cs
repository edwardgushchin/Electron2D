#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
namespace Electron2DBlockCodecs.Decoder.Options
{
    /// <summary>
    /// A class for the decoder input options.
    /// </summary>
	internal class DecoderInputOptions
    {
        /// <summary>
        /// The DDS file Format doesn't seem to have a standard for indicating whether a BC1 texture
        /// includes 1bit of alpha. This option will assume that all Bc1 textures contain alpha.
        /// If this option is false, but the dds header includes a DDPF_ALPHAPIXELS flag, alpha will be included.
        /// Default is true.
        /// </summary>
        public bool DdsBc1ExpectAlpha { get; set; } = true;
    }
}
