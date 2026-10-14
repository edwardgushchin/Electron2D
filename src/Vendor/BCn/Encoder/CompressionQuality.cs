#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
namespace Electron2DBlockCodecs.Encoder
{
	internal enum CompressionQuality
	{
		/// <summary>
		/// Fast, but low Quality. Especially bad with gradients.
		/// </summary>
		Fast,
		/// <summary>
		/// Strikes a balance between speed and Quality. Good enough for most purposes.
		/// </summary>
		Balanced,
		/// <summary>
		/// Aims for best Quality encoding. Can be very slow.
		/// </summary>
		BestQuality
	}
}
