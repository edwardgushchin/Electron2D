#define NETSTANDARD2_1
#define NETSTANDARD2_1_OR_GREATER
#nullable enable annotations
#nullable disable warnings
#pragma warning disable CS1591, CS1570, CS1572, CS1573, CS1574, CS1584, CS1658, CS1587, CS8500, CS0162, CA2022
using System;
using Electron2DBlockCodecs.Shared;

namespace Electron2DBlockCodecs.Encoder.Options
{
	/// <summary>
	/// General options for the encoder.
	/// </summary>
	internal class EncoderOptions
	{
		/// <summary>
		/// Whether the blocks should be encoded in parallel. This can be much faster than single-threaded encoding,
		/// but is slow if multiple textures are being processed at the same time.
		/// When a debugger is attached, the encoder defaults to single-threaded operation to ease debugging.
		/// Default is true.
		/// </summary>
		public bool IsParallel { get; set; } = true;

		/// <summary>
		/// Determines how many tasks should be used for parallel processing.
		/// </summary>
		public int TaskCount { get; set; } = Environment.ProcessorCount;

		/// <summary>
		/// The progress context for the operation.
		/// </summary>
		public IProgress<ProgressElement> Progress { get; set; }
	}
}
