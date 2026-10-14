#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using System;
using System.Threading;

namespace Electron2DBlockCodecs.Shared
{
	/// <summary>
	/// The operation context.
	/// </summary>
	internal class OperationContext
	{
		/// <summary>
		/// Whether the blocks should be decoded in parallel.
		/// </summary>
		public bool IsParallel { get; set; }

		/// <summary>
		/// Determines how many tasks should be used for parallel processing.
		/// </summary>
		public int TaskCount { get; set; } = Environment.ProcessorCount;

		/// <summary>
		/// The cancellation token to check if the asynchronous operation was cancelled.
		/// </summary>
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// The progress context for the operation.
		/// </summary>
		public OperationProgress Progress { get; set; }
	}
}
