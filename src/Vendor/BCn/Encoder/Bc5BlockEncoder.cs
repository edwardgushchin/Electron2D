#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using Electron2DBlockCodecs.Shared;
using Electron2DBlockCodecs.Shared.ImageFiles;

namespace Electron2DBlockCodecs.Encoder
{
	internal class Bc5BlockEncoder : BaseBcBlockEncoder<Bc5Block, RawBlock4X4Rgba32>
	{
		private readonly Bc4ComponentBlockEncoder redBlockEncoder;
		private readonly Bc4ComponentBlockEncoder greenBlockEncoder;

		public Bc5BlockEncoder(ColorComponent component1, ColorComponent component2)
		{
			redBlockEncoder = new Bc4ComponentBlockEncoder(component1);
			greenBlockEncoder = new Bc4ComponentBlockEncoder(component2);
		}

		public override Bc5Block EncodeBlock(RawBlock4X4Rgba32 block, CompressionQuality quality)
		{
			return new Bc5Block
			{
				redBlock = redBlockEncoder.EncodeBlock(block, quality),
				greenBlock = greenBlockEncoder.EncodeBlock(block, quality)
			};
		}

		public override GlInternalFormat GetInternalFormat()
		{
			return GlInternalFormat.GlCompressedRedGreenRgtc2Ext;
		}

		public override GlFormat GetBaseInternalFormat()
		{
			return GlFormat.GlRg;
		}

		public override DxgiFormat GetDxgiFormat()
		{
			return DxgiFormat.DxgiFormatBc5Unorm;
		}
	}
}
