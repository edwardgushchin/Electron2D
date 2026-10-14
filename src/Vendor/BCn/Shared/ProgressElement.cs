#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
namespace Electron2DBlockCodecs.Shared
{
	internal struct ProgressElement
	{
		/// <summary>
		/// Current block being processed
		/// </summary>
		public int CurrentBlock { get; }

		/// <summary>
		/// The total amount of blocks to be processed
		/// </summary>
		public int TotalBlocks { get; }

		/// <summary>
		/// Returns the progress percentage as a float from 0 to 1
		/// </summary>
		public float Percentage => CurrentBlock / (float) TotalBlocks;

		public ProgressElement(int currentBlock, int totalBlocks)
		{
			CurrentBlock = currentBlock;
			TotalBlocks = totalBlocks;
		}

		public override string ToString()
		{
			return $"{nameof(CurrentBlock)}: {CurrentBlock}, {nameof(TotalBlocks)}: {TotalBlocks}, {nameof(Percentage)}: {Percentage}";
		}
	}
}
