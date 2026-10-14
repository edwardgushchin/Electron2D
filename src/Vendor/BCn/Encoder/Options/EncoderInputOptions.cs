#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using Electron2DBlockCodecs.Shared;

namespace Electron2DBlockCodecs.Encoder.Options
{
    /// <summary>
    /// The input options for the decoder.
    /// </summary>
	internal class EncoderInputOptions
    {
	    /// <summary>
	    /// If true, when encoding to R8 raw format,
	    /// use the pixel luminance instead of just the red channel. Default is false. (Does not apply to BC4 format)
	    /// </summary>
	    public bool LuminanceAsRed { get; set; } = false;

	    /// <summary>
	    /// The color channel to take for the values of a BC4 block. Default is red.
	    /// </summary>
		public ColorComponent Bc4Component { get; set; } = ColorComponent.R;

		/// <summary>
		/// The color channel to take for the values of the first BC5 block. Default is red.
		/// </summary>
		public ColorComponent Bc5Component1 { get; set; } = ColorComponent.R;

		/// <summary>
		/// The color channel to take for the values of the second BC5 block. Default is green.
		/// </summary>
		public ColorComponent Bc5Component2 { get; set; } = ColorComponent.G;
	}
}
