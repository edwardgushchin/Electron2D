#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using Electron2DBlockCodecs.Encoder;
using Electron2DBlockCodecs.Shared;

namespace Electron2DBlockCodecs.Decoder.Options
{
	/// <summary>
	/// A class for the decoder output options.
	/// </summary>
	internal class DecoderOutputOptions
	{
		/// <summary>
		/// If true, when decoding from R8 raw format,
		/// output pixels will have all colors set to the same value (greyscale).
		/// Default is true. (Does not apply to BC4 format.)
		/// </summary>
		public bool RedAsLuminance { get; set; } = true;

		/// <summary>
		/// The color channel to populate with the values of a BC4 block.
		/// </summary>
		public ColorComponent Bc4Component { get; set; } = ColorComponent.R;

		/// <summary>
		/// The color channel to populate with the values of the first BC5 block.
		/// </summary>
		public ColorComponent Bc5Component1 { get; set; } = ColorComponent.R;

		/// <summary>
		/// The color channel to populate with the values of the second BC5 block.
		/// </summary>
		public ColorComponent Bc5Component2 { get; set; } = ColorComponent.G;
	}
}
