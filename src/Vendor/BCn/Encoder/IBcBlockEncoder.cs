#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using System;
using Electron2DBlockCodecs.Shared;
using Electron2DBlockCodecs.Shared.ImageFiles;

namespace Electron2DBlockCodecs.Encoder
{
	internal interface IBcBlockEncoder<T> where T : unmanaged
	{
		byte[] Encode(T[] blocks, int blockWidth, int blockHeight, CompressionQuality quality, OperationContext context);
		void EncodeBlock(T block, CompressionQuality quality, Span<byte> output);
		GlInternalFormat GetInternalFormat();
		GlFormat GetBaseInternalFormat();
		DxgiFormat GetDxgiFormat();
		int GetBlockSize();
	}


}
