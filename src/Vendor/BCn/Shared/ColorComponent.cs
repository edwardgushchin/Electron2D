#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
namespace Electron2DBlockCodecs.Shared
{
	/// <summary>
	/// The component to take from colors for BC4 and BC5.
	/// </summary>
	internal enum ColorComponent
	{
		/// <summary>
		/// The red component of an Rgba32 color.
		/// </summary>
		R,

		/// <summary>
		/// The green component of an Rgba32 color.
		/// </summary>
		G,

		/// <summary>
		/// The blue component of an Rgba32 color.
		/// </summary>
		B,

		/// <summary>
		/// The alpha component of an Rgba32 color.
		/// </summary>
		A,

		/// <summary>
		/// Use the color's luminance value as the component.
		/// </summary>
		Luminance
	}
}
