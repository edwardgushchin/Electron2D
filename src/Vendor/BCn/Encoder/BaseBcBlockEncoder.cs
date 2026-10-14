#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Electron2DBlockCodecs.Shared;
using Electron2DBlockCodecs.Shared.ImageFiles;

namespace Electron2DBlockCodecs.Encoder
{
	internal abstract class BaseBcBlockEncoder<T, TBlock> : IBcBlockEncoder<TBlock> where T : unmanaged where TBlock : unmanaged
	{
		private static readonly object lockObj = new object();

		public byte[] Encode(TBlock[] blocks, int blockWidth, int blockHeight, CompressionQuality quality, OperationContext context)
		{
			var outputData = new byte[blockWidth * blockHeight * Unsafe.SizeOf<T>()];

			var currentBlocks = 0;
			if (context.IsParallel)
			{
				var options = new ParallelOptions
				{
					CancellationToken = context.CancellationToken,
					MaxDegreeOfParallelism = context.TaskCount
				};
				Parallel.For(0, blocks.Length, options, i =>
				 {
					 var outputBlocks = MemoryMarshal.Cast<byte, T>(outputData.AsSpan());
					 outputBlocks[i] = EncodeBlock(blocks[i], quality);

					 if (context.Progress != null)
					 {
						 lock (lockObj)
						 {
							 context.Progress.Report(++currentBlocks);
						 }
					 }
				 });
			}
			else
			{
				var outputBlocks = MemoryMarshal.Cast<byte, T>(outputData.AsSpan());
				for (var i = 0; i < blocks.Length; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					outputBlocks[i] = EncodeBlock(blocks[i], quality);

					context.Progress?.Report(++currentBlocks);
				}
			}

			return outputData;
		}

		public void EncodeBlock(TBlock block, CompressionQuality quality, Span<byte> output)
		{
			if (output.Length != Unsafe.SizeOf<T>())
			{
				throw new Exception("Cannot encode block! Output buffer is not the correct size.");
			}
			var encoded = EncodeBlock(block, quality);
			MemoryMarshal.Cast<byte, T>(output)[0] = encoded;
		}

		public abstract GlInternalFormat GetInternalFormat();
		public abstract GlFormat GetBaseInternalFormat();
		public abstract DxgiFormat GetDxgiFormat();
		public int GetBlockSize()
		{
			return Unsafe.SizeOf<T>();
		}

		public abstract T EncodeBlock(TBlock block, CompressionQuality quality);
	}
}
