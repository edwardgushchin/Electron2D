#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
namespace Electron2DBlockCodecs.Shared
{
	internal enum OutputFileFormat
	{
		/// <summary>
		/// Khronos texture Format https://www.khronos.org/opengles/sdk/tools/KTX/file_format_spec/
		/// </summary>
		Ktx,
		/// <summary>
		/// Direct draw surface Format https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds
		/// </summary>
		Dds
	}
}
