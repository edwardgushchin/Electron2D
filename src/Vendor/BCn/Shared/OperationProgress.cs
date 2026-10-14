#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using System;

namespace Electron2DBlockCodecs.Shared
{
	internal class OperationProgress
	{
		private readonly IProgress<ProgressElement> progress;
		private readonly int totalBlocks;
		private int processedBlocks;

		public OperationProgress(IProgress<ProgressElement> progress, int totalBlocks)
		{
			this.progress = progress;
			this.totalBlocks = totalBlocks;
		}

		public void SetProcessedBlocks(int processedBlocks)
		{
			this.processedBlocks = processedBlocks;
		}

		public void Report(int currentBlock)
		{
			progress?.Report(new ProgressElement(processedBlocks + currentBlock, totalBlocks));
		}
	}
}
